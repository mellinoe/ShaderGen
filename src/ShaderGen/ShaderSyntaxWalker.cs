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
        private readonly ILanguageBackend[] _backends;
        private readonly ShaderSetInfo _shaderSet;

        private Dictionary<int, int> _setCounts = new Dictionary<int, int>();

        public ShaderSyntaxWalker(Compilation compilation, ILanguageBackend[] backends, ShaderSetInfo ss)
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
                parameters.Add(ParameterDefinition.GetParameterDefinition(_compilation, ps));
            }

            TypeReference returnType = new TypeReference(GetModel(node).GetFullTypeName(node.ReturnType));

            UInt3 computeGroupCounts = new UInt3();
            bool isFragmentShader = false, isComputeShader = false;
            bool isVertexShader = Utilities.GetMethodAttributes(node, "VertexShader").Any();
            if (!isVertexShader)
            {
                isFragmentShader = Utilities.GetMethodAttributes(node, "FragmentShader").Any();
            }
            if (!isVertexShader && !isFragmentShader)
            {
                AttributeSyntax computeShaderAttr = Utilities.GetMethodAttributes(node, "ComputeShader").FirstOrDefault();
                if (computeShaderAttr != null)
                {
                    isComputeShader = true;
                    computeGroupCounts.X = GetAttributeArgumentUIntValue(computeShaderAttr, 0);
                    computeGroupCounts.Y = GetAttributeArgumentUIntValue(computeShaderAttr, 1);
                    computeGroupCounts.Z = GetAttributeArgumentUIntValue(computeShaderAttr, 2);
                }
            }

            ShaderFunctionType type = isVertexShader
                ? ShaderFunctionType.VertexEntryPoint
                : isFragmentShader
                    ? ShaderFunctionType.FragmentEntryPoint
                    : isComputeShader
                        ? ShaderFunctionType.ComputeEntryPoint
                        : ShaderFunctionType.Normal;

            string nestedTypePrefix = Utilities.GetFullNestedTypePrefix(node, out bool nested);
            ShaderFunction sf = new ShaderFunction(
                nestedTypePrefix,
                functionName,
                returnType,
                parameters.ToArray(),
                type,
                computeGroupCounts);
            ShaderFunctionAndBlockSyntax sfab = new ShaderFunctionAndBlockSyntax(sf, node.Body);
            foreach (LanguageBackend b in _backends) { b.AddFunction(_shaderSet.Name, sfab); }
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            TryGetStructDefinition(GetModel(node), node, out var sd);
            foreach (var b in _backends) { b.AddStructure(_shaderSet.Name, sd); }
        }

        public static bool TryGetStructDefinition(SemanticModel model, StructDeclarationSyntax node, out StructureDefinition sd)
        {
            string fullNestedTypePrefix = Utilities.GetFullNestedTypePrefix(node, out bool nested);
            string structName = node.Identifier.ToFullString().Trim();
            if (!string.IsNullOrEmpty(fullNestedTypePrefix))
            {
                string joiner = nested ? "+" : ".";
                structName = fullNestedTypePrefix + joiner + structName;
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
                        string typeName = model.GetFullTypeName(varDecl.Type, out bool isArray);
                        int arrayElementCount = 0;
                        if (isArray)
                        {
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
            return GetAttributeArgumentIntValue(arraySizeAttr, 0);
        }

        private static int GetAttributeArgumentIntValue(AttributeSyntax attr, int index)
        {
            if (attr.ArgumentList.Arguments.Count < index + 1)
            {
                throw new ShaderGenerationException(
                    "Too few arguments in attribute " + attr.ToFullString() + ". Required + " + (index + 1));
            }
            string fullArg0 = attr.ArgumentList.Arguments[index].ToFullString();
            if (int.TryParse(fullArg0, out int ret))
            {
                return ret;
            }
            else
            {
                throw new ShaderGenerationException("Incorrectly formatted attribute: " + attr.ToFullString());
            }
        }

        private static uint GetAttributeArgumentUIntValue(AttributeSyntax attr, int index)
        {
            if (attr.ArgumentList.Arguments.Count < index + 1)
            {
                throw new ShaderGenerationException(
                    "Too few arguments in attribute " + attr.ToFullString() + ". Required + " + (index + 1));
            }
            string fullArg0 = attr.ArgumentList.Arguments[index].ToFullString();
            if (uint.TryParse(fullArg0, out uint ret))
            {
                return ret;
            }
            else
            {
                throw new ShaderGenerationException("Incorrectly formatted attribute: " + attr.ToFullString());
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

            if (CheckSingleAttribute(vds, "SystemPositionSemantic"))
            {
                return SemanticType.SystemPosition;
            }
            else if (CheckSingleAttribute(vds, "PositionSemantic"))
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
            else if (CheckSingleAttribute(vds, "ColorTargetSemantic"))
            {
                return SemanticType.ColorTarget;
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
            if (node.Variables.Count != 1)
            {
                throw new ShaderGenerationException("Cannot declare multiple variables together.");
            }

            VariableDeclaratorSyntax vds = node.Variables[0];

            string resourceName = vds.Identifier.Text;
            TypeInfo typeInfo = GetModel(node).GetTypeInfo(node.Type);
            string fullTypeName = GetModel(node).GetFullTypeName(node.Type);
            TypeReference valueType = new TypeReference(fullTypeName);
            ShaderResourceKind kind = ClassifyResourceKind(fullTypeName);

            if (kind == ShaderResourceKind.StructuredBuffer || kind == ShaderResourceKind.RWStructuredBuffer)
            {
                valueType = ParseStructuredBufferElementType(vds);
            }

            int set = 0; // Default value if not otherwise specified.
            if (GetResourceDecl(node, out AttributeSyntax resourceSetDecl))
            {
                set = GetAttributeArgumentIntValue(resourceSetDecl, 0);
            }

            int resourceBinding = GetAndIncrementBinding(set);

            ResourceDefinition rd = new ResourceDefinition(resourceName, set, resourceBinding, valueType, kind);
            if (kind == ShaderResourceKind.Uniform)
            {
                ValidateResourceType(typeInfo);
            }

            foreach (LanguageBackend b in _backends) { b.AddResource(_shaderSet.Name, rd); }
        }

        private TypeReference ParseStructuredBufferElementType(VariableDeclaratorSyntax vds)
        {
            FieldDeclarationSyntax fieldDecl = (FieldDeclarationSyntax)vds.Parent.Parent;
            GenericNameSyntax gns = (GenericNameSyntax)fieldDecl.Declaration.Type;
            TypeSyntax type = gns.TypeArgumentList.Arguments[0];
            string fullName = Utilities.GetFullTypeName(GetModel(vds), type);
            return new TypeReference(fullName);
        }

        private int GetAndIncrementBinding(int set)
        {
            if (!_setCounts.TryGetValue(set, out int ret))
            {
                ret = 0;
                _setCounts.Add(set, ret);
            }
            else
            {
                ret += 1;
                _setCounts[set] = ret;
            }

            return ret;
        }

        private void ValidateResourceType(TypeInfo typeInfo)
        {
            string name = typeInfo.Type.ToDisplayString();
            if (name != nameof(ShaderGen) + "." + nameof(Texture2DResource)
                && name != nameof(ShaderGen) + "." + nameof(TextureCubeResource)
                && name != nameof(ShaderGen) + "." + nameof(Texture2DMSResource)
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
            else if (fullTypeName == "ShaderGen.TextureCubeResource")
            {
                return ShaderResourceKind.TextureCube;
            }
            else if (fullTypeName == "ShaderGen.Texture2DMSResource")
            {
                return ShaderResourceKind.Texture2DMS;
            }
            else if (fullTypeName == "ShaderGen.SamplerResource")
            {
                return ShaderResourceKind.Sampler;
            }
            else if (fullTypeName.Contains("ShaderGen.RWStructuredBuffer"))
            {
                return ShaderResourceKind.RWStructuredBuffer;
            }
            else if (fullTypeName.Contains("ShaderGen.StructuredBuffer"))
            {
                return ShaderResourceKind.StructuredBuffer;
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
