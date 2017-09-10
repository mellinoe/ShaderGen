using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    public class HlslMethodVisitor : CSharpSyntaxVisitor<string>
    {
        private readonly SemanticModel _model;
        private readonly LanguageBackend _backend;
        private readonly ShaderFunction _shaderFunction;
        public string _value;

        public HlslMethodVisitor(SemanticModel model, ShaderFunction shaderFunction, HlslBackend backend)
        {
            _model = model;
            _shaderFunction = shaderFunction;
            _backend = backend;
        }

        public override string VisitBlock(BlockSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            string returnType = _backend.CSharpToShaderType(_shaderFunction.ReturnType.Name);
            string suffix = _shaderFunction.Type == ShaderFunctionType.FragmentEntryPoint ? " : SV_Target" : string.Empty;
            sb.AppendLine($"{returnType} {_shaderFunction.Name}({GetParameterDeclList()}){suffix}");
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

        public override string VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            VariableDeclarationSyntax decl = node.Declaration;
            if (decl.Variables.Count != 1)
            {
                throw new NotImplementedException();
            }

            string mappedType = _backend.CSharpToShaderType(decl.Type);
            string initializerStr = Visit(decl.Variables[0].Initializer);
            string result = mappedType + " " + decl.Variables[0].Identifier;
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

            SymbolInfo symbolInfo = _model.GetSymbolInfo(ins);
            string type = symbolInfo.Symbol.ContainingType.ToDisplayString();
            string method = symbolInfo.Symbol.Name;
            string functionName = _backend.CSharpToShaderFunctionName(type, method);
            return $"{functionName}({Visit(node.ArgumentList)})";
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
            SymbolInfo symbolInfo = _model.GetSymbolInfo(node.Type);
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
            return node.Identifier.ToFullString().Trim();
        }

        public override string VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            return node.ToFullString().Trim();
        }

        private string GetParameterDeclList()
        {
            return string.Join(", ", _shaderFunction.Parameters.Select(pd => $"{_backend.CSharpToShaderType(pd.Type.Name)} {pd.Name}"));
        }
    }
}
