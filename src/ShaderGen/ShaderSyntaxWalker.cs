using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Linq;
using System;
using System.IO;
using ShaderGen;
using System.Collections.Generic;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace ShaderGen
{
    public class ShaderSyntaxWalker : CSharpSyntaxWalker
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly TransformationContext _context;
        private readonly ShaderFunctionWalker _functionWalker;

        private readonly List<StructDefinition> _structs = new List<StructDefinition>();
        private readonly List<UniformDefinition> _uniforms = new List<UniformDefinition>();

        public ShaderSyntaxWalker(TransformationContext context) : base(Microsoft.CodeAnalysis.SyntaxWalkerDepth.Token)
        {
            _context = context;
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
            string structName = node.Identifier.Text;

            List<FieldDefinition> fields = new List<FieldDefinition>();
            foreach (MemberDeclarationSyntax member in node.Members)
            {
                if (member is FieldDeclarationSyntax fds)
                {
                    VariableDeclarationSyntax varDecl = fds.Declaration;
                    foreach (VariableDeclaratorSyntax vds in varDecl.Variables)
                    {
                        string fieldName = vds.Identifier.Text;
                        TypeReference tr = new TypeReference(GetFullTypeName(varDecl.Type));
                        fields.Add(new FieldDefinition(fieldName, tr));
                    }
                }
            }

            _structs.Add(new StructDefinition(structName, fields.ToArray()));
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
            if (GetUniformDecl(node, out AttributeSyntax uniformAttr))
            {
                ExpressionSyntax uniformBindingExpr = uniformAttr.ArgumentList.Arguments[0].Expression;
                if (!(uniformBindingExpr is LiteralExpressionSyntax les))
                {
                    throw new InvalidOperationException("Must use a literal parameter in UniformAttribute ctor.");
                }

                int uniformBinding = int.Parse(les.ToFullString());

                foreach (VariableDeclaratorSyntax vds in node.Variables)
                {
                    string uniformName = vds.Identifier.Text;
                    TypeInfo typeInfo = _context.SemanticModel.GetTypeInfo(node.Type);
                    string fullTypeName = GetFullTypeName(node.Type);
                    TypeReference tr = new TypeReference(fullTypeName);
                    UniformDefinition ud = new UniformDefinition(uniformName, uniformBinding, tr);
                    _uniforms.Add(ud);
                }
            }
        }

        private string GetFullTypeName(TypeSyntax type)
        {
            TypeInfo typeInfo = _context.SemanticModel.GetTypeInfo(type);
            string ns = GetFullNamespace(typeInfo.Type.ContainingNamespace);
            return ns + "." + typeInfo.Type.Name;
        }

        private string GetFullNamespace(INamespaceSymbol ns)
        {
            Debug.Assert(ns != null);
            string currentNamespace = ns.Name;
            if (ns.ContainingNamespace != null && !ns.ContainingNamespace.IsGlobalNamespace)
            {
                return GetFullNamespace(ns.ContainingNamespace) + "." + currentNamespace;
            }
            else
            {
                return currentNamespace;
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
            StringBuilder fullText = new StringBuilder();
            HlslWriter hlslWriter = new HlslWriter(_structs, _uniforms);
            fullText.Append(hlslWriter.GetHlslText());
            fullText.Append(_sb);
            File.WriteAllText(file, fullText.ToString());
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
}
