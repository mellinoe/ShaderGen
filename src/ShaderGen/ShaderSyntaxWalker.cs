using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    public class ShaderSyntaxWalker : CSharpSyntaxWalker
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly SemanticModel _model;

        private readonly List<StructDefinition> _structs = new List<StructDefinition>();
        private readonly List<UniformDefinition> _uniforms = new List<UniformDefinition>();
        private readonly List<HlslMethodVisitor> _methods = new List<HlslMethodVisitor>();

        public ShaderSyntaxWalker(SemanticModel model) : base(SyntaxWalkerDepth.Token)
        {
            _model = model;
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            string functionName = node.Identifier.ToFullString();
            List<ParameterDefinition> parameters = new List<ParameterDefinition>();
            foreach (ParameterSyntax ps in node.ParameterList.Parameters)
            {
                parameters.Add(GetParameterDefinition(ps));
            }

            TypeReference returnType = new TypeReference(_model.GetFullTypeName(node.ReturnType));

            ShaderFunction sf = new ShaderFunction(functionName, returnType, parameters.ToArray(), node.Body);

            HlslMethodVisitor hmv = new HlslMethodVisitor(_model, sf);
            hmv.VisitBlock(node.Body);
            _methods.Add(hmv);
        }

        private ParameterDefinition GetParameterDefinition(ParameterSyntax ps)
        {
            string fullType = _model.GetFullTypeName(ps.Type);
            string name = ps.Identifier.ToFullString();
            return new ParameterDefinition(name, new TypeReference(fullType));
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            string fullNamespace = Extensions.GetFullNamespace(node);
            string structName = node.Identifier.ToFullString();
            if (fullNamespace != null)
            {
                structName = fullNamespace + "." + structName;
            }

            List<FieldDefinition> fields = new List<FieldDefinition>();
            foreach (MemberDeclarationSyntax member in node.Members)
            {
                if (member is FieldDeclarationSyntax fds)
                {
                    VariableDeclarationSyntax varDecl = fds.Declaration;
                    foreach (VariableDeclaratorSyntax vds in varDecl.Variables)
                    {
                        string fieldName = vds.Identifier.Text;
                        TypeReference tr = new TypeReference(_model.GetFullTypeName(varDecl.Type));
                        SemanticType semanticType = GetSemanticType(vds);

                        fields.Add(new FieldDefinition(fieldName, tr, semanticType));
                    }
                }
            }

            _structs.Add(new StructDefinition(structName, fields.ToArray()));
        }

        private SemanticType GetSemanticType(VariableDeclaratorSyntax vds)
        {
            AttributeSyntax[] attrs = vds.Parent.Parent.DescendantNodes().OfType<AttributeSyntax>()
                .Where(attrSyntax => attrSyntax.Name.ToString().Contains("SemanticType")).ToArray();
            if (attrs.Length == 1)
            {
                AttributeSyntax semanticTypeAttr = attrs[0];
                string fullArg0 = semanticTypeAttr.ArgumentList.Arguments[0].ToFullString();
                if (fullArg0.Contains("."))
                {
                    fullArg0 = fullArg0.Substring(fullArg0.LastIndexOf('.') + 1);
                }
                if (Enum.TryParse(fullArg0, out SemanticType ret))
                {
                    return ret;
                }
                else
                {
                    throw new InvalidOperationException("Incorrectly formatted attribute: " + semanticTypeAttr.ToFullString());
                }
            }

            return SemanticType.None;
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
                    TypeInfo typeInfo = _model.GetTypeInfo(node.Type);
                    string fullTypeName = _model.GetFullTypeName(node.Type);
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

        public void WriteToFile(string file)
        {
            StringBuilder fullText = new StringBuilder();
            HlslBackend backend = new HlslBackend(_model);
            backend.WriteStructures(fullText, _structs.ToArray());
            backend.WriteUniforms(fullText, _uniforms.ToArray());

            foreach (HlslMethodVisitor method in _methods)
            {
                fullText.Append(method._value);
            }

            File.WriteAllText(file, fullText.ToString());
        }
    }
}
