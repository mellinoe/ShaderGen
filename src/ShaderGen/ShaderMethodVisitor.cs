using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShaderGen
{
    public partial class ShaderMethodVisitor : CSharpSyntaxVisitor<string>
    {
        protected readonly Compilation _compilation;
        protected readonly string _setName;
        protected readonly LanguageBackend _backend;
        protected readonly ShaderFunction _shaderFunction;
        private string _containingTypeName;

        public ShaderMethodVisitor(
            Compilation compilation,
            string setName,
            ShaderFunction shaderFunction,
            LanguageBackend backend)
        {
            _compilation = compilation;
            _setName = setName;
            _shaderFunction = shaderFunction;
            _backend = backend;
        }

        private SemanticModel GetModel(SyntaxNode node) => _compilation.GetSemanticModel(node.SyntaxTree);

        public string VisitFunction(BlockSyntax node)
        {
            _containingTypeName = Utilities.GetFullNestedTypePrefix(node, out bool _);
            StringBuilder sb = new StringBuilder();
            string functionDeclStr = GetFunctionDeclStr();
            sb.AppendLine(functionDeclStr);
            sb.AppendLine(VisitBlock(node));
            return sb.ToString();
        }

        public override string VisitBlock(BlockSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");

            foreach (StatementSyntax ss in node.Statements)
            {
                string statementResult = Visit(ss);
                if (string.IsNullOrEmpty(statementResult))
                {
                    throw new NotImplementedException($"{ss.GetType()} statements are not implemented.");
                }
                else
                {
                    sb.AppendLine("    " + statementResult);
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        protected virtual string GetFunctionDeclStr()
        {
            string returnType = _backend.CSharpToShaderType(_shaderFunction.ReturnType.Name);
            string fullDeclType = _backend.CSharpToShaderType(_shaderFunction.DeclaringType);
            string funcName = _shaderFunction.IsEntryPoint
                ? _shaderFunction.Name
                : fullDeclType + "_" + _shaderFunction.Name;
            return $"{returnType} {funcName}({GetParameterDeclList()})";
        }

        public override string VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            return Visit(node.Declaration) + ";";
        }

        public override string VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            return node.EqualsToken.ToFullString() + Visit(node.Value);
        }

        public override string VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            string token = node.OperatorToken.ToFullString().Trim();
            if (token == "%=")
            {
                throw new ShaderGenerationException(
                    "Modulus operator not supported in shader functions. Use ShaderBuiltins.Mod instead.");

            }

            return base.Visit(node.Left)
                + " "
                + token
                + base.Visit(node.Right)
                + ";";
        }

        public override string VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            SymbolInfo exprSymbol = GetModel(node).GetSymbolInfo(node.Expression);
            if (exprSymbol.Symbol.Kind == SymbolKind.NamedType)
            {
                // Static member access
                string typeName = Utilities.GetFullMetadataName(exprSymbol.Symbol);
                string targetName = Visit(node.Name);
                return _backend.FormatInvocation(_setName, typeName, targetName, Array.Empty<InvocationParameterInfo>());
            }
            else
            {
                // Other accesses

                return Visit(node.Expression)
                    + node.OperatorToken.ToFullString()
                    + Visit(node.Name);
            }
        }

        public override string VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return Visit(node.Expression);
        }

        public override string VisitReturnStatement(ReturnStatementSyntax node)
        {
            return "return "
                + Visit(node.Expression)
                + ";";
        }

        public override string VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax ins)
            {
                InvocationParameterInfo[] parameterInfos = GetParameterInfos(node.ArgumentList);
                SymbolInfo symbolInfo = GetModel(node).GetSymbolInfo(ins);
                string type = symbolInfo.Symbol.ContainingType.ToDisplayString();
                string method = symbolInfo.Symbol.Name;
                return _backend.FormatInvocation(_setName, type, method, parameterInfos);
            }
            else if (node.Expression is MemberAccessExpressionSyntax maes)
            {
                SymbolInfo methodSymbol = GetModel(maes).GetSymbolInfo(maes);
                if (methodSymbol.Symbol is IMethodSymbol ims)
                {
                    string containingType = Utilities.GetFullMetadataName(ims.ContainingType);
                    string methodName = ims.MetadataName;
                    List<InvocationParameterInfo> pis = new List<InvocationParameterInfo>();
                    if (ims.IsExtensionMethod)
                    {
                        string identifier = null;
                        // Extension method invocation, ie: swizzle:
                        if (maes.Expression is MemberAccessExpressionSyntax subExpression)
                        {
                            identifier = Visit(subExpression);
                        }
                        else if (maes.Expression is IdentifierNameSyntax identNameSyntax)
                        {
                            identifier = Visit(identNameSyntax);
                        }

                        Debug.Assert(identifier != null);
                        // Might need FullTypeName here too.
                        pis.Add(new InvocationParameterInfo()
                        {
                            Identifier = identifier
                        });
                    }

                    else if (!ims.IsStatic) // Add implicit "this" parameter.
                    {
                        string identifier = null;
                        if (maes.Expression is MemberAccessExpressionSyntax subExpression)
                        {
                            identifier = Visit(subExpression);
                        }
                        else if (maes.Expression is IdentifierNameSyntax identNameSyntax)
                        {
                            identifier = Visit(identNameSyntax);
                        }

                        Debug.Assert(identifier != null);
                        pis.Add(new InvocationParameterInfo
                        {
                            FullTypeName = containingType,
                            Identifier = identifier
                        });
                    }

                    pis.AddRange(GetParameterInfos(node.ArgumentList));
                    return _backend.FormatInvocation(_setName, containingType, methodName, pis.ToArray());
                }

                throw new NotImplementedException();
            }
            else
            {
                string message = "Function calls must be made through an IdentifierNameSyntax or a MemberAccessExpressionSyntax.";
                message += Environment.NewLine + "This node used a " + node.Expression.GetType().Name;
                message += Environment.NewLine + node.ToFullString();
                throw new NotImplementedException(message);
            }
        }

        public override string VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            string token = node.OperatorToken.ToFullString().Trim();
            if (token == "%")
            {
                throw new ShaderGenerationException(
                    "Modulus operator not supported in shader functions. Use ShaderBuiltins.Mod instead.");
            }

            return Visit(node.Left) + " "
                + node.OperatorToken + " "
                + Visit(node.Right);
        }

        public override string VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            return node.OpenParenToken
                + Visit(node.Expression)
                + node.CloseParenToken;
        }

        public override string VisitArgumentList(ArgumentListSyntax node)
        {
            return string.Join(", ", node.Arguments.Select(argSyntax => Visit(argSyntax)));
        }

        public override string VisitArgument(ArgumentSyntax node)
        {
            string result = Visit(node.Expression);
            if (string.IsNullOrEmpty(result))
            {
                throw new NotImplementedException($"{node.Expression.GetType()} arguments are not implemented.");
            }
            else
            {
                return result;
            }
        }

        public override string VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            SymbolInfo symbolInfo = GetModel(node).GetSymbolInfo(node.Type);
            string fullName = Utilities.GetFullName(symbolInfo);
             
            InvocationParameterInfo[] parameters = GetParameterInfos(node.ArgumentList);
            return _backend.FormatInvocation(_setName, fullName, "ctor", parameters);
        }

        public override string VisitIdentifierName(IdentifierNameSyntax node)
        {
            SymbolInfo symbolInfo = GetModel(node).GetSymbolInfo(node);
            ISymbol symbol = symbolInfo.Symbol;
            string containingTypeName = Utilities.GetFullName(symbolInfo.Symbol.ContainingType);
            if (symbol.Kind == SymbolKind.Field && containingTypeName == _containingTypeName)
            {
                return _backend.CorrectFieldAccess(symbolInfo);
            }
            string mapped = _backend.CSharpToShaderIdentifierName(symbolInfo);
            return _backend.CorrectIdentifier(mapped);
        }

        public override string VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            string literal = node.ToFullString().Trim();
            return _backend.CorrectLiteral(literal);
        }

        public override string VisitIfStatement(IfStatementSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("if (" + Visit(node.Condition) + ")");
            sb.AppendLine(Visit(node.Statement));
            sb.AppendLine(Visit(node.Else));
            return sb.ToString();
        }

        public override string VisitElseClause(ElseClauseSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("else");
            sb.AppendLine(Visit(node.Statement));
            return sb.ToString();
        }

        public override string VisitForStatement(ForStatementSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            string declaration = Visit(node.Declaration);
            string incrementers = string.Join(", ", node.Incrementors.Select(es => Visit(es)));
            string condition = Visit(node.Condition);
            sb.AppendLine($"for ({declaration}; {condition}; {incrementers})");
            sb.AppendLine(Visit(node.Statement));
            return sb.ToString();
        }

        public override string VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            return node.OperatorToken.ToFullString() + Visit(node.Operand);
        }

        public override string VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            return Visit(node.Operand) + node.OperatorToken.ToFullString();
        }

        public override string VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            if (node.Variables.Count != 1)
            {
                throw new NotImplementedException();
            }

            string csName = _compilation.GetSemanticModel(node.Type.SyntaxTree).GetFullTypeName(node.Type);
            string mappedType = _backend.CSharpToShaderType(csName);
            string initializerStr = Visit(node.Variables[0].Initializer);
            string result = mappedType + " "
                + _backend.CorrectIdentifier(node.Variables[0].Identifier.ToString());
            if (!string.IsNullOrEmpty(initializerStr))
            {
                result += " " + initializerStr;
            }

            return result;
        }

        public override string VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            return Visit(node.Expression) + Visit(node.ArgumentList);
        }

        public override string VisitBracketedArgumentList(BracketedArgumentListSyntax node)
        {
            return node.OpenBracketToken.ToFullString()
                + string.Join(", ", node.Arguments.Select(argSyntax => Visit(argSyntax)))
                + node.CloseBracketToken.ToFullString();
        }

        protected string GetParameterDeclList()
        {
            return string.Join(", ", _shaderFunction.Parameters.Select(pd => $"{_backend.CSharpToShaderType(pd.Type.Name)} {_backend.CorrectIdentifier(pd.Name)}"));
        }

        private InvocationParameterInfo[] GetParameterInfos(ArgumentListSyntax argumentList)
        {
            return argumentList.Arguments.Select(argSyntax =>
            {
                return GetInvocationParameterInfo(argSyntax);
            }).ToArray();
        }

        private InvocationParameterInfo GetInvocationParameterInfo(ArgumentSyntax argSyntax)
        {
            TypeInfo typeInfo = GetModel(argSyntax).GetTypeInfo(argSyntax.Expression);
            return new InvocationParameterInfo
            {
                FullTypeName = typeInfo.Type.ToDisplayString(),
                Identifier = Visit(argSyntax.Expression)
            };
        }
    }
}
