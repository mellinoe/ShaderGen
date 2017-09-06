using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Linq;
using System;
using System.IO;

public class ShaderSyntaxWalker : CSharpSyntaxWalker
{
    private readonly StringBuilder _sb = new StringBuilder();
    private readonly ShaderFunctionWalker _functionWalker;

    public ShaderSyntaxWalker() : base(Microsoft.CodeAnalysis.SyntaxWalkerDepth.Token)
    {
        _functionWalker = new ShaderFunctionWalker(_sb);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        _sb.AppendLine($"Encountered method declaration: {node.Identifier.Text}");
        if (node.AttributeLists.Any(als => als.Attributes.Any(attrSyntax => attrSyntax.GetText().ToString().Contains("EntryFunction"))))
        {
            _sb.AppendLine($"  - Is a shader method.");
        }

        VisitBlock(node.Body);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        _sb.AppendLine($"Encountered struct declaration: {node.Identifier.Text}");
        foreach (MemberDeclarationSyntax member in node.Members)
        {
            _sb.AppendLine($"  * Member: {member.ToFullString()}");
        }
    }

    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        _sb.AppendLine($"  * Assignment: [[ {node.ToFullString()} ]]");
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        _sb.AppendLine($"  * Member access: [[ {node.ToFullString()} ]]");
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        _sb.AppendLine($"  * Invocation: {node.ToFullString()}");
    }

    public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
    {
        _sb.AppendLine($"  * Var decl: [[ {node.ToFullString()} ]] ");
        if (GetUniformDecl(node, out AttributeSyntax uniformAttr))
        {
            _sb.AppendLine($"    - This is a uniform: {uniformAttr.ToFullString()}");
        }

    }

    private bool GetUniformDecl(VariableDeclarationSyntax node, out AttributeSyntax attr)
    {
        attr = (node.Parent.DescendantNodes().OfType<AttributeSyntax>().FirstOrDefault(
            attrSyntax => attrSyntax.ToString().Contains("Uniform")));
        return attr != null;
    }

    internal void WriteToFile(string file)
    {
        File.WriteAllText(file, _sb.ToString());
    }
}

public class ShaderFunctionWalker : CSharpSyntaxWalker
{
    private readonly StringBuilder _sb;

    public ShaderFunctionWalker(StringBuilder sb)
    {
        _sb = sb;
    }
}
