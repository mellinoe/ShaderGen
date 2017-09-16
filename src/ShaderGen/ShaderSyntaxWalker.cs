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
        private readonly ShaderSetInfo _shaderSet;
        private int _lastResourceBinding;

        public ShaderSyntaxWalker(Compilation compilation, LanguageBackend[] backends, ShaderSetInfo ss)
            : base(SyntaxWalkerDepth.Token)
        {
            _compilation = compilation;
            _backends = backends;
            _shaderSet = ss;
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
            foreach (LanguageBackend b in _backends) { b.AddFunction(_shaderSet.Name, sfab); }
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
            foreach (var b in _backends) { b.AddStructure(_shaderSet.Name, sd); }
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
                        string typeName = model.GetFullTypeName(varDecl.Type);
                        bool isArrayType = typeName.Contains("[]");
                        int arrayElementCount = 0;
                        if (isArrayType)
                        {
                            typeName = typeName.Substring(0, typeName.Length - 2);
                            arrayElementCount = GetArrayCountValue(vds);
                        }

                        TypeReference tr = new TypeReference(typeName);
                        SemanticType semanticType = GetSemanticType(vds);


                        fields.Add(new FieldDefinition(fieldName, tr, semanticType, arrayElementCount));
                    }
                }
            }

            sd = new StructureDefinition(structName.Trim(), fields.ToArray());
            return true;
        }

        private static int GetArrayCountValue(VariableDeclaratorSyntax vds)
        {
            AttributeSyntax[] arraySizeAttrs = Utilities.GetMemberAttributes(vds, "ArraySize");
            if (arraySizeAttrs.Length != 1)
            {
                throw new ShaderGenerationException(
                    "Array fields in structs must have a constant size specified by an ArraySizeAttribute.");
            }
            AttributeSyntax arraySizeAttr = arraySizeAttrs[0];
            string fullArg0 = arraySizeAttr.ArgumentList.Arguments[0].ToFullString();
            if (int.TryParse(fullArg0, out int ret))
            {
                return ret;
            }
            else
            {
                throw new ShaderGenerationException("Incorrectly formatted attribute: " + arraySizeAttr.ToFullString());
            }

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

            if (CheckSingleAttribute(vds, "PositionSemantic"))
            {
                return SemanticType.Position;
            }
            else if (CheckSingleAttribute(vds, "NormalSemantic"))
            {
                return SemanticType.Normal;
            }
            else if (CheckSingleAttribute(vds, "TextureCoordinateSemantic"))
            {
                return SemanticType.TextureCoordinate;
            }
            else if (CheckSingleAttribute(vds, "ColorSemantic"))
            {
                return SemanticType.Color;
            }
            else if (CheckSingleAttribute(vds, "TangentSemantic"))
            {
                return SemanticType.Tangent;
            }

            return SemanticType.None;
        }

        private static bool CheckSingleAttribute(VariableDeclaratorSyntax vds, string name)
        {
            AttributeSyntax[] attrs = Utilities.GetMemberAttributes(vds, name);
            return attrs.Length == 1;
        }

        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            int resourceBinding = _lastResourceBinding++;

            if (node.Variables.Count != 1)
            {
                throw new ShaderGenerationException("Cannot declare multiple variables together.");
            }

            VariableDeclaratorSyntax vds = node.Variables[0];

            string resourceName = vds.Identifier.Text;
            TypeInfo typeInfo = GetModel(node).GetTypeInfo(node.Type);
            string fullTypeName = GetModel(node).GetFullTypeName(node.Type);
            TypeReference tr = new TypeReference(fullTypeName);
            ShaderResourceKind kind = ClassifyResourceKind(fullTypeName);
            ResourceDefinition rd = new ResourceDefinition(resourceName, resourceBinding, tr, kind);
            if (kind == ShaderResourceKind.Uniform)
            {
                ValidateResourceType(typeInfo);
            }

            foreach (LanguageBackend b in _backends) { b.AddResource(_shaderSet.Name, rd); }
        }

        private void ValidateResourceType(TypeInfo typeInfo)
        {
            string name = typeInfo.Type.ToDisplayString();
            if (name != nameof(ShaderGen) + "." + nameof(Texture2DResource)
                && name != nameof(ShaderGen) + "." + nameof(TextureCubeResource)
                && name != nameof(ShaderGen) + "." + nameof(SamplerResource))
            {
                if (typeInfo.Type.IsReferenceType)
                {
                    throw new ShaderGenerationException("Shader resource fields must be simple blittable structures.");
                }
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
    }
}
