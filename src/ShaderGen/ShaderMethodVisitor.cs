using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    public partial class ShaderMethodVisitor : CSharpSyntaxVisitor<string>
    {
        protected readonly Compilation _compilation;
        protected readonly LanguageBackend _backend;
        protected readonly ShaderFunction _shaderFunction;

        public ShaderMethodVisitor(Compilation compilation, ShaderFunction shaderFunction, LanguageBackend backend)
        {
            _compilation = compilation;
            _shaderFunction = shaderFunction;
            _backend = backend;
        }

        private SemanticModel GetModel(SyntaxNode node) => _compilation.GetSemanticModel(node.SyntaxTree);

        public override string VisitBlock(BlockSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            string functionDeclStr = GetFunctionDeclStr();
            sb.AppendLine(functionDeclStr);
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
            return $"{returnType} {_shaderFunction.Name}({GetParameterDeclList()})";
        }

        public override string VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            VariableDeclarationSyntax decl = node.Declaration;
            if (decl.Variables.Count != 1)
            {
                throw new NotImplementedException();
            }

            string csName = _compilation.GetSemanticModel(decl.Type.SyntaxTree).GetFullTypeName(decl.Type);
            string mappedType = _backend.CSharpToShaderType(csName);
            string initializerStr = Visit(decl.Variables[0].Initializer);
            string result = mappedType + " "
                + _backend.CorrectIdentifier(decl.Variables[0].Identifier.ToString());
            if (!string.IsNullOrEmpty(initializerStr))
            {
                result += " " + initializerStr;
            }

            result += ";";
            return result;
        }

        public override string VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            return node.EqualsToken.ToFullString() + Visit(node.Value);
        }

        public override string VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            return Visit(node.Left)
                + " "
                + node.OperatorToken.ToFullString()
                + Visit(node.Right)
                + ";";
        }

        public override string VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            return Visit(node.Expression)
                + node.OperatorToken.ToFullString()
                + Visit(node.Name);
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
            IdentifierNameSyntax ins = node.Expression as IdentifierNameSyntax;
            if (ins == null)
            {
                throw new NotImplementedException("Function calls must be made through an IdentifierNameSyntax.");
            }

            InvocationParameterInfo[] parameterInfos = GetParameterInfos(node.ArgumentList);
            SymbolInfo symbolInfo = GetModel(node).GetSymbolInfo(ins);
            string type = symbolInfo.Symbol.ContainingType.ToDisplayString();
            string method = symbolInfo.Symbol.Name;

            return _backend.FormatInvocation(type, method, parameterInfos);
        }

        public override string VisitBinaryExpression(BinaryExpressionSyntax node)
        {
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
            string fullName = symbolInfo.Symbol.Name;
            string ns = symbolInfo.Symbol.ContainingNamespace.ToDisplayString();
            if (!string.IsNullOrEmpty(ns))
            {
                fullName = ns + "." + fullName;
            }

            if (!Utilities.IsBasicNumericType(fullName))
            {
                throw new ShaderGenerationException(
                    "Constructors can only be called on basic numeric types.");
            }

            return _backend.CSharpToShaderType(fullName) + "(" + Visit(node.ArgumentList) + ")";
        }

        public override string VisitIdentifierName(IdentifierNameSyntax node)
        {
            SymbolInfo symbolInfo = GetModel(node).GetSymbolInfo(node);
            string mapped = _backend.CSharpToShaderIdentifierName(symbolInfo);
            return _backend.CorrectIdentifier(mapped);
        }

        public override string VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            return node.ToFullString().Trim();
        }

        protected string GetParameterDeclList()
        {
            return string.Join(", ", _shaderFunction.Parameters.Select(pd => $"{_backend.CSharpToShaderType(pd.Type.Name)} {_backend.CorrectIdentifier(pd.Name)}"));
        }

        private InvocationParameterInfo[] GetParameterInfos(ArgumentListSyntax argumentList)
        {
            return argumentList.Arguments.Select(argSyntax => new InvocationParameterInfo
            {
                FullTypeName = null, // TODO
                Identifier = Visit(argSyntax.Expression)
            }).ToArray();
        }
    }
}
