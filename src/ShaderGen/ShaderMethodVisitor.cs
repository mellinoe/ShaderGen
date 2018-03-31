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
        private HashSet<ResourceDefinition> _resourcesUsed = new HashSet<ResourceDefinition>();

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

        public MethodProcessResult VisitFunction(MethodDeclarationSyntax node)
        {
            _containingTypeName = Utilities.GetFullNestedTypePrefix((SyntaxNode)node.Body ?? node.ExpressionBody, out bool _);
            StringBuilder sb = new StringBuilder();
            string blockResult;
            // Visit block first in order to discover builtin variables.
            if (node.Body != null)
            {
                blockResult = VisitBlock(node.Body);
            }
            else if (node.ExpressionBody != null)
            {
                blockResult = VisitArrowExpressionClause(node.ExpressionBody);
            }
            else
            {
                throw new NotSupportedException("Methods without bodies cannot be shader functions.");
            }

            string functionDeclStr = GetFunctionDeclStr();

            if (_shaderFunction.Type == ShaderFunctionType.ComputeEntryPoint)
            {
                sb.AppendLine(_backend.GetComputeGroupCountsDeclaration(_shaderFunction.ComputeGroupCounts));
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

        public override string VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");

            string expressionResult = Visit(node.Expression);
            if (string.IsNullOrEmpty(expressionResult))
            {
                throw new NotImplementedException($"{node.Expression.GetType()} expressions are not implemented.");
            }

            if (_shaderFunction.ReturnType.Name == "System.Void")
            {
                sb.AppendLine("    " + expressionResult + ";");
            }
            else
            {
                sb.AppendLine("    return " + expressionResult + ";");
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
            return Visit(node.Declaration);
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

            string leftExpr = base.Visit(node.Left);
            string leftExprType = Utilities.GetFullTypeName(GetModel(node), node.Left);
            string rightExpr = base.Visit(node.Right);
            string rightExprType = Utilities.GetFullTypeName(GetModel(node), node.Right);

            string assignedValue = _backend.CorrectAssignedValue(leftExprType, rightExpr, rightExprType);
            return $"{leftExpr} {token} {assignedValue}";
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
                return _backend.FormatInvocation(_setName, typeName, targetName, Array.Empty<InvocationParameterInfo>());
            }
            else
            {
                // Other accesses
                bool isIndexerAccess = _backend.IsIndexerAccess(GetModel(node).GetSymbolInfo(node.Name));
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
            return Visit(node.Expression) + ";";
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

                if (type == "ShaderGen.ShaderBuiltins")
                {
                    ValidateBuiltInMethod(method);
                }

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
                        string identifier = Visit(maes.Expression);
                        string identifierType = Utilities.GetFullTypeName(GetModel(maes.Expression), maes.Expression);
                        Debug.Assert(identifier != null);
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

        private void ValidateBuiltInMethod(string name)
        {
            if (name == nameof(ShaderBuiltins.Ddx) || name == nameof(ShaderBuiltins.Ddy))
            {
                if (_shaderFunction.Type != ShaderFunctionType.FragmentEntryPoint)
                {
                    throw new ShaderGenerationException("Ddx and Ddy can only be used within Fragment shaders.");
                }
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

            string leftExpr = Visit(node.Left);
            string leftExprType = Utilities.GetFullTypeName(GetModel(node), node.Left);
            string operatorToken = node.OperatorToken.ToString();
            string rightExpr = Visit(node.Right);
            string rightExprType = Utilities.GetFullTypeName(GetModel(node), node.Right);

            return _backend.CorrectBinaryExpression(leftExpr, leftExprType, operatorToken, rightExpr, rightExprType);
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
            if (containingTypeName == "ShaderGen.ShaderBuiltins")
            {
                TryRecognizeBuiltInVariable(symbolInfo);
            }
            if (symbol.Kind == SymbolKind.Field && containingTypeName == _containingTypeName)
            {
                string symbolName = symbol.Name;
                ResourceDefinition referencedResource = _backend.GetContext(_setName).Resources.Single(rd => rd.Name == symbolName);
                _resourcesUsed.Add(referencedResource);
                _shaderFunction.UsesTexture2DMS |= referencedResource.ValueType.Name == "ShaderGen.Texture2DMSResource";

                return _backend.CorrectFieldAccess(symbolInfo);
            }
            else if (symbol.Kind == SymbolKind.Property)
            {
                return _backend.FormatInvocation(_setName, containingTypeName, symbol.Name, Array.Empty<InvocationParameterInfo>());
            }

            string mapped = _backend.CSharpToShaderIdentifierName(symbolInfo);
            return _backend.CorrectIdentifier(mapped);
        }

        private void TryRecognizeBuiltInVariable(SymbolInfo symbolInfo)
        {
            string name = symbolInfo.Symbol.Name;
            if (name == nameof(ShaderBuiltins.VertexID))
            {
                if (_shaderFunction.Type != ShaderFunctionType.VertexEntryPoint)
                {
                    throw new ShaderGenerationException("VertexID can only be used within Vertex shaders.");
                }
                _shaderFunction.UsesVertexID = true;
            }
            else if (name == nameof(ShaderBuiltins.InstanceID))
            {
                _shaderFunction.UsesInstanceID = true;
            }
            else if (name == nameof(ShaderBuiltins.DispatchThreadID))
            {
                if (_shaderFunction.Type != ShaderFunctionType.ComputeEntryPoint)
                {
                    throw new ShaderGenerationException("DispatchThreadID can only be used within Vertex shaders.");
                }
                _shaderFunction.UsesDispatchThreadID = true;
            }
            else if (name == nameof(ShaderBuiltins.GroupThreadID))
            {
                if (_shaderFunction.Type != ShaderFunctionType.ComputeEntryPoint)
                {
                    throw new ShaderGenerationException("GroupThreadID can only be used within Vertex shaders.");
                }
                _shaderFunction.UsesGroupThreadID = true;
            }
            else if (name == nameof(ShaderBuiltins.IsFrontFace))
            {
                if (_shaderFunction.Type != ShaderFunctionType.FragmentEntryPoint)
                {
                    throw new ShaderGenerationException("IsFrontFace can only be used within Fragment shaders.");
                }
                _shaderFunction.UsesFrontFace = true;
            }
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
            sb.AppendLine($"for ({declaration} {condition}; {incrementers})");
            sb.AppendLine(Visit(node.Statement));
            return sb.ToString();
        }

        public override string VisitSwitchStatement(SwitchStatementSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("switch (" + Visit(node.Expression) + ")");
            sb.AppendLine("{");
            foreach (SwitchSectionSyntax section in node.Sections)
            {
                foreach (SwitchLabelSyntax label in section.Labels)
                {
                    sb.AppendLine(Visit(label));
                }

                foreach (StatementSyntax statement in section.Statements)
                {
                    sb.AppendLine(Visit(statement));
                }
                sb.AppendLine("break;");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        public override string VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("case " + Visit(node.Value) + ":");
            return sb.ToString();
        }

        public override string VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("default:");
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

            StringBuilder sb = new StringBuilder();

            string varType = _compilation.GetSemanticModel(node.Type.SyntaxTree).GetFullTypeName(node.Type);
            string mappedType = _backend.CSharpToShaderType(varType);

            sb.Append(mappedType);
            sb.Append(' ');
            VariableDeclaratorSyntax varDeclarator = node.Variables[0];
            string identifier = _backend.CorrectIdentifier(varDeclarator.Identifier.ToString());
            sb.Append(identifier);

            if (varDeclarator.Initializer != null)
            {
                sb.Append(' ');
                sb.Append(varDeclarator.Initializer.EqualsToken.ToString());
                sb.Append(' ');

                string rightExpr = base.Visit(varDeclarator.Initializer.Value);
                string rightExprType = Utilities.GetFullTypeName(GetModel(node), varDeclarator.Initializer.Value);

                sb.Append(_backend.CorrectAssignedValue(varType, rightExpr, rightExprType));
            }

            sb.Append(';');

            return sb.ToString();
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

        public override string VisitCastExpression(CastExpressionSyntax node)
        {
            string varType = _compilation.GetSemanticModel(node.Type.SyntaxTree).GetFullTypeName(node.Type);
            string mappedType = _backend.CSharpToShaderType(varType);

            return "(" + mappedType + ") " + Visit(node.Expression);
        }

        protected string GetParameterDeclList()
        {
            return string.Join(", ", _shaderFunction.Parameters.Select(FormatParameter));
        }

        protected virtual string FormatParameter(ParameterDefinition pd)
        {
            return $"{_backend.ParameterDirection(pd.Direction)} {_backend.CSharpToShaderType(pd.Type.Name)} {_backend.CorrectIdentifier(pd.Name)}";
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
