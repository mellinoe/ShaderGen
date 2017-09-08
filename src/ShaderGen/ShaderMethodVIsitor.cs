using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using CodeGeneration.Roslyn;
using System;
using System.Linq;

namespace ShaderGen
{
    public class HlslMethodVisitor : CSharpSyntaxVisitor<string>
    {
        private readonly TransformationContext _context;
        private readonly ShaderFunction _shaderFunction;
        public string _value;

        public HlslMethodVisitor(TransformationContext context, ShaderFunction shaderFunction)
        {
            _context = context;
            _shaderFunction = shaderFunction;
        }

        public override string VisitBlock(BlockSyntax node)
        {
            StringBuilder sb = new StringBuilder();
            string returnType = HlslKnownTypes.GetMappedName(_shaderFunction.ReturnType.Name);
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

            string type = _context.GetFullTypeName(decl.Type);
            string mappedType = HlslKnownTypes.GetMappedName(type);
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
            return $"CALL[{node.Expression.GetType()}]::" + Visit(node.Expression) + $"({Visit(node.ArgumentList)})";
        }

        public override string VisitArgumentList(ArgumentListSyntax node)
        {
            return string.Join(", ", node.Arguments.Select(argSyntax => Visit(argSyntax)));
        }

        public override string VisitIdentifierName(IdentifierNameSyntax node)
        {
            return node.Identifier.ToFullString();
        }

        private string GetParameterDeclList()
        {
            return string.Join(", ", _shaderFunction.Parameters.Select(pd => $"{HlslKnownTypes.GetMappedName(pd.Type.Name)} {pd.Name}"));
        }
    }
}
