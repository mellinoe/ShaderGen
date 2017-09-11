using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    internal class ShaderSyntaxWalker : CSharpSyntaxWalker
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly Compilation _compilation;
        private readonly LanguageBackend[] _backends;
        private int _lastResourceBinding;

        public ShaderSyntaxWalker(Compilation compilation, LanguageBackend[] backends) : base(SyntaxWalkerDepth.Token)
        {
            _compilation = compilation;
            _backends = backends;
        }

        public ShaderModel GetShaderModel()
        {
            // HACK: Discover all method input structures.
            foreach (ShaderFunctionAndBlockSyntax sf in _backends[0].Functions.ToArray())
            {
                foreach (LanguageBackend b in _backends) { b.GetCode(sf.Function); }
            }
            return _backends[0].GetShaderModel(); // HACK
        }

        private SemanticModel GetModel(SyntaxNode node) => _compilation.GetSemanticModel(node.SyntaxTree);

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            string functionName = node.Identifier.ToFullString();
            List<ParameterDefinition> parameters = new List<ParameterDefinition>();
            foreach (ParameterSyntax ps in node.ParameterList.Parameters)
            {
                parameters.Add(GetParameterDefinition(ps));
            }

            TypeReference returnType = new TypeReference(GetModel(node).GetFullTypeName(node.ReturnType));

            bool isVertexShader, isFragmentShader = false;
            isVertexShader = Utilities.GetMethodAttributes(node, "VertexShader").Any();
            if (!isVertexShader)
            {
                isFragmentShader = Utilities.GetMethodAttributes(node, "FragmentShader").Any();
            }

            ShaderFunctionType type = isVertexShader
                ? ShaderFunctionType.VertexEntryPoint : isFragmentShader
                ? ShaderFunctionType.FragmentEntryPoint : ShaderFunctionType.Normal;

            ShaderFunction sf = new ShaderFunction(functionName, returnType, parameters.ToArray(), type);
            ShaderFunctionAndBlockSyntax sfab = new ShaderFunctionAndBlockSyntax(sf, node.Body);
            foreach (LanguageBackend b in _backends) { b.AddFunction(sfab); }
        }

        private ParameterDefinition GetParameterDefinition(ParameterSyntax ps)
        {
            string fullType = GetModel(ps).GetFullTypeName(ps.Type);
            string name = ps.Identifier.ToFullString();
            return new ParameterDefinition(name, new TypeReference(fullType));
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            TryGetStructDefinition(GetModel(node), node, out var sd);
            foreach (var b in _backends) { b.AddStructure(sd); }
        }

        public static bool TryGetStructDefinition(SemanticModel model, StructDeclarationSyntax node, out StructureDefinition sd)
        {
            string fullNamespace = Utilities.GetFullNestedTypePrefix(node);
            string structName = node.Identifier.ToFullString().Trim();
            if (!string.IsNullOrEmpty(fullNamespace))
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
                        string fieldName = vds.Identifier.Text.Trim();
                        TypeReference tr = new TypeReference(model.GetFullTypeName(varDecl.Type));
                        SemanticType semanticType = GetSemanticType(vds);

                        fields.Add(new FieldDefinition(fieldName, tr, semanticType));
                    }
                }
            }

            sd = new StructureDefinition(structName.Trim(), fields.ToArray());
            return true;
        }

        private static SemanticType GetSemanticType(VariableDeclaratorSyntax vds)
        {
            AttributeSyntax[] attrs = Utilities.GetMemberAttributes(vds, "VertexSemantic");
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
                    throw new ShaderGenerationException("Incorrectly formatted attribute: " + semanticTypeAttr.ToFullString());
                }
            }
            else if (attrs.Length > 1)
            {
                throw new ShaderGenerationException("Too many vertex semantics applied to field: " + vds.ToFullString());
            }

            return SemanticType.None;
        }

        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            if (GetResourceDecl(node, out AttributeSyntax resourceAttr))
            {
                int resourceBinding = _lastResourceBinding++;

                if (node.Variables.Count != 1)
                {
                    throw new ShaderGenerationException("Cannot declare multiple ResourceAttribute variables together.");
                }

                VariableDeclaratorSyntax vds = node.Variables[0];

                string resourceName = vds.Identifier.Text;
                TypeInfo typeInfo = GetModel(node).GetTypeInfo(node.Type);
                string fullTypeName = GetModel(node).GetFullTypeName(node.Type);
                TypeReference tr = new TypeReference(fullTypeName);
                ShaderResourceKind kind = ClassifyResourceKind(fullTypeName);
                ResourceDefinition rd = new ResourceDefinition(resourceName, resourceBinding, tr, kind);
                foreach (LanguageBackend b in _backends) { b.AddResource(rd); }
            }
        }

        private ShaderResourceKind ClassifyResourceKind(string fullTypeName)
        {
            if (fullTypeName == "ShaderGen.Texture2DResource")
            {
                return ShaderResourceKind.Texture2D;
            }
            if (fullTypeName == "ShaderGen.TextureCubeResource")
            {
                return ShaderResourceKind.TextureCube;
            }
            else if (fullTypeName == "ShaderGen.SamplerResource")
            {
                return ShaderResourceKind.Sampler;
            }
            else
            {
                return ShaderResourceKind.Uniform;
            }
        }

        private bool GetResourceDecl(VariableDeclarationSyntax node, out AttributeSyntax attr)
        {
            attr = (node.Parent.DescendantNodes().OfType<AttributeSyntax>().FirstOrDefault(
                attrSyntax => attrSyntax.ToString().Contains("Resource")));
            return attr != null;
        }
    }
}
