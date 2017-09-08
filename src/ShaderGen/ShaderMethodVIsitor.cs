using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using CodeGeneration.Roslyn;
using System;
using System.Linq;
using System.IO;

namespace ShaderGen
{
    public class HlslMethodVisitor : CSharpSyntaxVisitor<string>
    {
        private readonly TransformationContext _context;
        private readonly TypeTranslator _typeTranslator;
        private readonly ShaderFunction _shaderFunction;
        public string _value;

        public HlslMethodVisitor(TransformationContext context, ShaderFunction shaderFunction)
        {
            _context = context;
            _shaderFunction = shaderFunction;
            _typeTranslator = new HlslTypeTranslator(context);
        }

        public override string VisitBlock(BlockSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            string returnType = _typeTranslator.CSharpToShaderType(_shaderFunction.ReturnType.Name);
            sb.AppendLine($"{returnType} {_shaderFunction.Name}({GetParameterDeclList()})");
            sb.AppendLine("{");

            foreach (StatementSyntax ss in node.Statements)
            {
                string statementResult = Visit(ss);
                if (string.IsNullOrEmpty(statementResult))
                {
                    sb.AppendLine(ss.GetType().ToString() + " (Unhandled)");
                }
                else
                {
                    sb.AppendLine(statementResult);
                }
            }

            sb.AppendLine("}");

            _value = sb.ToString();
            return sb.ToString();
        }

        public override string VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            VariableDeclarationSyntax decl = node.Declaration;
            if (decl.Variables.Count != 1)
            {
                throw new NotImplementedException();
            }

            string mappedType = _typeTranslator.CSharpToShaderType(decl.Type);
            return mappedType + " " + decl.Variables[0].Identifier + " " + Visit(decl.Variables[0].Initializer) + ";";
        }

        public override string VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            return node.EqualsToken.ToFullString() + Visit(node.Value);
        }

        public override string VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            return Visit(node.Left)
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
            return $"{Visit(node.Expression)}({Visit(node.ArgumentList)})";
        }

        public override string VisitArgumentList(ArgumentListSyntax node)
        {
            return string.Join(", ", node.Arguments.Select(argSyntax => Visit(argSyntax)));
        }

        public override string VisitArgument(ArgumentSyntax node)
        {
            string result = Visit(node.Expression);
            return string.IsNullOrEmpty(result) ? ($"[Unhandled] {node.Expression.GetType()}") : result;
        }

        public override string VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            return _typeTranslator.CSharpToShaderType(node.Type) + "(" + Visit(node.ArgumentList) + ")";
        }

        public override string VisitIdentifierName(IdentifierNameSyntax node)
        {
            return node.Identifier.ToFullString();
        }

        public override string VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            return node.ToFullString();
        }

        private string GetParameterDeclList()
        {
            return string.Join(", ", _shaderFunction.Parameters.Select(pd => $"{_typeTranslator.CSharpToShaderType(pd.Type.Name)} {pd.Name}"));
        }
    }
}
