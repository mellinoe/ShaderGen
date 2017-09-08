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

        private readonly List<StructDefinition> _structs = new List<StructDefinition>();
        private readonly List<UniformDefinition> _uniforms = new List<UniformDefinition>();
        private readonly List<HlslMethodVisitor> _methods = new List<HlslMethodVisitor>();

        public ShaderSyntaxWalker(TransformationContext context) : base(Microsoft.CodeAnalysis.SyntaxWalkerDepth.Token)
        {
            _context = context;
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            string functionName = node.Identifier.ToFullString();
            List<ParameterDefinition> parameters = new List<ParameterDefinition>();
            foreach (ParameterSyntax ps in node.ParameterList.Parameters)
            {
                parameters.Add(GetParameterDefinition(ps));
            }

            TypeReference returnType = new TypeReference(_context.GetFullTypeName(node.ReturnType));

            ShaderFunction sf = new ShaderFunction(functionName, returnType, parameters.ToArray(), node.Body);

            HlslMethodVisitor smv = new HlslMethodVisitor(_context, sf);
            smv.VisitBlock(node.Body);
            _methods.Add(smv);
        }

        private ParameterDefinition GetParameterDefinition(ParameterSyntax ps)
        {
            string fullType = _context.GetFullTypeName(ps.Type);
            string name = ps.Identifier.ToFullString();
            return new ParameterDefinition(name, new TypeReference(fullType));
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
                        TypeReference tr = new TypeReference(_context.GetFullTypeName(varDecl.Type));
                        fields.Add(new FieldDefinition(fieldName, tr));
                    }
                }
            }

            _structs.Add(new StructDefinition(structName, fields.ToArray()));
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
                    string fullTypeName = _context.GetFullTypeName(node.Type);
                    TypeReference tr = new TypeReference(fullTypeName);
                    UniformDefinition ud = new UniformDefinition(uniformName, uniformBinding, tr);
                    _uniforms.Add(ud);
                }
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

            foreach (HlslMethodVisitor method in _methods)
            {
                fullText.Append(method._value);
            }

            File.WriteAllText(file, fullText.ToString());
        }
    }
}
