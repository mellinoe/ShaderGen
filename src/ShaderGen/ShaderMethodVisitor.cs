using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace ShaderGen
{
    public class ShaderMethodVisitor : CSharpSyntaxVisitor<string>
    {
        protected readonly Compilation Compilation;
        protected readonly string SetName;
        protected readonly LanguageBackend Backend;
        protected readonly ShaderFunction ShaderFunction;
        private string _containingTypeName;
        private HashSet<ResourceDefinition> _resourcesUsed = new HashSet<ResourceDefinition>();

        public ShaderMethodVisitor(
            Compilation compilation,
            string setName,
            ShaderFunction shaderFunction,
            LanguageBackend backend)
        {
            Compilation = compilation;
            SetName = setName;
            ShaderFunction = shaderFunction;
            Backend = backend;
        }

        private SemanticModel GetModel(SyntaxNode node) => Compilation.GetSemanticModel(node.SyntaxTree);

        public MethodProcessResult VisitFunction(BlockSyntax node)
        {
            _containingTypeName = Utilities.GetFullNestedTypePrefix(node, out bool _);
            StringBuilder sb = new StringBuilder();
            string blockResult = VisitBlock(node); // Visit block first in order to discover builtin variables.
            string functionDeclStr = GetFunctionDeclStr();

            if (ShaderFunction.Type == ShaderFunctionType.ComputeEntryPoint)
            {
                sb.AppendLine(Backend.GetComputeGroupCountsDeclaration(ShaderFunction.ComputeGroupCounts));
            }

            sb.AppendLine(functionDeclStr);
            sb.AppendLine(blockResult);
            return new MethodProcessResult(sb.ToString(), _resourcesUsed);
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
            string returnType = Backend.CSharpToShaderType(ShaderFunction.ReturnType.Name);
            string fullDeclType = Backend.CSharpToShaderType(ShaderFunction.DeclaringType);
            string funcName = ShaderFunction.IsEntryPoint
                ? ShaderFunction.Name
                : fullDeclType + "_" + ShaderFunction.Name;
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

            return Visit(node.Left)
                + " "
                + token
                + Visit(node.Right)
                + ";";
        }

        public override string VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            SymbolInfo exprSymbol = GetModel(node).GetSymbolInfo(node.Expression);
            if (exprSymbol.Symbol.Kind == SymbolKind.NamedType)
            {
                // Static member access
                SymbolInfo symbolInfo = GetModel(node).GetSymbolInfo(node);
                ISymbol symbol = symbolInfo.Symbol;
                if (symbol.Kind == SymbolKind.Property)
                {
                    return Visit(node.Name);
                }

                string typeName = Utilities.GetFullMetadataName(exprSymbol.Symbol);
                string targetName = Visit(node.Name);
                return Backend.FormatInvocation(SetName, typeName, targetName, Array.Empty<InvocationParameterInfo>());
            }
            else
            {
                // Other accesses
                bool isIndexerAccess = Backend.IsIndexerAccess(GetModel(node).GetSymbolInfo(node.Name));
                string expr = Visit(node.Expression);
                string name = Visit(node.Name);

                if (!isIndexerAccess)
                {
                    return Visit(node.Expression)
                        + node.OperatorToken.ToFullString()
                        + Visit(node.Name);
                }
                else
                {
                    return Visit(node.Expression)
                        + Visit(node.Name);
                }
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
                return Backend.FormatInvocation(SetName, type, method, parameterInfos);
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
                        string identifier = Visit(maes.Expression);
                        string identifierType = Utilities.GetFullTypeName(GetModel(maes.Expression), maes.Expression);
                        // Might need FullTypeName here too.
                        pis.Add(new InvocationParameterInfo()
                        {
                            Identifier = identifier,
                            FullTypeName = identifierType,
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

                        pis.Add(new InvocationParameterInfo
                        {
                            FullTypeName = containingType,
                            Identifier = identifier
                        });
                    }

                    pis.AddRange(GetParameterInfos(node.ArgumentList));
                    return Backend.FormatInvocation(SetName, containingType, methodName, pis.ToArray());
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
            return Backend.FormatInvocation(SetName, fullName, "ctor", parameters);
        }

        public override string VisitIdentifierName(IdentifierNameSyntax node)
        {
            SymbolInfo symbolInfo = GetModel(node).GetSymbolInfo(node);
            ISymbol symbol = symbolInfo.Symbol;
            string containingTypeName = Utilities.GetFullName(symbolInfo.Symbol.ContainingType);
            if (containingTypeName == "ShaderGen.ShaderBuiltins")
            {
                TryRecognizeBuiltInVariable(symbolInfo);
            }
            if (symbol.Kind == SymbolKind.Field && containingTypeName == _containingTypeName)
            {
                string symbolName = symbol.Name;
                ResourceDefinition referencedResource = _backend.GetContext(_setName).Resources.Single(rd => rd.Name == symbolName);
                _resourcesUsed.Add(referencedResource);
                return Backend.CorrectFieldAccess(symbolInfo);
            }
            else if (symbol.Kind == SymbolKind.Property)
            {
                return Backend.FormatInvocation(SetName, containingTypeName, symbol.Name, Array.Empty<InvocationParameterInfo>());
            }

            string mapped = Backend.CSharpToShaderIdentifierName(symbolInfo);
            return Backend.CorrectIdentifier(mapped);
        }

        private void TryRecognizeBuiltInVariable(SymbolInfo symbolInfo)
        {
            string name = symbolInfo.Symbol.Name;
            if (name == nameof(ShaderBuiltins.VertexID))
            {
                if (ShaderFunction.Type != ShaderFunctionType.VertexEntryPoint)
                {
                    throw new ShaderGenerationException("VertexID can only be used within Vertex shaders.");
                }
                ShaderFunction.UsesVertexID = true;
            }
            else if (name == nameof(ShaderBuiltins.InstanceID))
            {
                ShaderFunction.UsesInstanceID = true;
            }
            else if (name == nameof(ShaderBuiltins.DispatchThreadID))
            {
                if (ShaderFunction.Type != ShaderFunctionType.ComputeEntryPoint)
                {
                    throw new ShaderGenerationException("DispatchThreadID can only be used within Vertex shaders.");
                }
                ShaderFunction.UsesDispatchThreadID = true;
            }
            else if (name == nameof(ShaderBuiltins.GroupThreadID))
            {
                if (ShaderFunction.Type != ShaderFunctionType.ComputeEntryPoint)
                {
                    throw new ShaderGenerationException("GroupThreadID can only be used within Vertex shaders.");
                }
                ShaderFunction.UsesGroupThreadID = true;
            }
            else if (name == nameof(ShaderBuiltins.IsFrontFace))
            {
                if (ShaderFunction.Type != ShaderFunctionType.FragmentEntryPoint)
                {
                    throw new ShaderGenerationException("IsFrontFace can only be used within Fragment shaders.");
                }
                ShaderFunction.UsesFrontFace = true;
            }
        }

        public override string VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            string literal = node.ToFullString().Trim();
            return Backend.CorrectLiteral(literal);
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

            string csName = Compilation.GetSemanticModel(node.Type.SyntaxTree).GetFullTypeName(node.Type);
            string mappedType = Backend.CSharpToShaderType(csName);
            string initializerStr = Visit(node.Variables[0].Initializer);
            string result = mappedType + " "
                + Backend.CorrectIdentifier(node.Variables[0].Identifier.ToString());
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
            return string.Join(", ", ShaderFunction.Parameters.Select(FormatParameter));
        }

        protected virtual string FormatParameter(ParameterDefinition pd)
        {
            return $"{Backend.CSharpToShaderType(pd.Type.Name)} {Backend.CorrectIdentifier(pd.Name)}";
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
