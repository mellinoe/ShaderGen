using Microsoft.CodeAnalysis;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;

namespace ShaderGen
{
    public class HlslBackend : LanguageBackend
    {
        private readonly List<StructureDefinition> _synthesizedStructures = new List<StructureDefinition>();

        private const string FragmentSemanticsSuffix = "__FRAGSEMANTICS";
        public HlslBackend(Compilation compilation) : base(compilation)
        {
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return HlslKnownTypes.GetMappedName(fullType)
                .Replace(".", "_");
        }

        protected void WriteStructure(StringBuilder sb, StructureDefinition sd)
        {
            bool fragmentSemantics = sd.Name.EndsWith(FragmentSemanticsSuffix);
            sb.AppendLine($"struct {CSharpToShaderType(sd.Name)}");
            sb.AppendLine("{");
            HlslSemanticTracker tracker = new HlslSemanticTracker();
            foreach (FieldDefinition field in sd.Fields)
            {
                sb.AppendLine($"    {CSharpToShaderType(field.Type.Name.Trim())} {field.Name.Trim()}{HlslSemantic(field.SemanticType, fragmentSemantics, ref tracker)};");
            }
            sb.AppendLine("};");
            sb.AppendLine();
        }

        private string HlslSemantic(SemanticType semanticType, bool fragmentSemantics, ref HlslSemanticTracker tracker)
        {
            switch (semanticType)
            {
                case SemanticType.None:
                    return string.Empty;
                case SemanticType.Position:
                    if (fragmentSemantics)
                    {
                        return " : SV_POSITION";
                    }
                    else
                    {
                        int val = tracker.Position++;
                        return " : POSITION" + val.ToString();
                    }
                case SemanticType.Normal:
                    {
                        int val = tracker.Normal++;
                        return " : NORMAL" + val.ToString();
                    }
                case SemanticType.TextureCoordinate:
                    {
                        int val = tracker.TexCoord++;
                        return " : TEXCOORD" + val.ToString();
                    }
                case SemanticType.Color:
                    {
                        int val = tracker.Color++;
                        return " : COLOR" + val.ToString();
                    }
                case SemanticType.Tangent:
                    {
                        int val = tracker.Tangent++;
                        return " : TANGENT" + val.ToString();
                    }
                default: throw new InvalidOperationException("Invalid semantic type: " + semanticType);
            }
        }

        private void WriteSampler(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"SamplerState {rd.Name} : register(s{binding});");
            sb.AppendLine();
        }

        private void WriteTexture2D(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"Texture2D {rd.Name} : register(t{binding});");
            sb.AppendLine();
        }

        private void WriteTextureCube(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"TextureCube {rd.Name} : register(t{binding});");
            sb.AppendLine();
        }

        private void WriteUniform(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"cbuffer {rd.Name}Buffer : register(b{binding})");
            sb.AppendLine("{");
            sb.AppendLine($"    {CSharpToShaderType(rd.ValueType.Name)} {rd.Name.Trim()};");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        protected override string GenerateFullTextCore(ShaderFunction function)
        {
            StringBuilder sb = new StringBuilder();

            ShaderFunctionAndBlockSyntax entryPoint = Functions.SingleOrDefault(
                sfabs => sfabs.Function.Name == function.Name);
            if (entryPoint == null)
            {
                throw new ShaderGenerationException("Couldn't find given function: " + function.Name);
            }

            StructureDefinition input = GetRequiredStructureType(entryPoint.Function.Parameters[0].Type);

            if (function.Type == ShaderFunctionType.VertexEntryPoint)
            {
                // HLSL vertex outputs needs to have semantics applied to the structure fields.
                StructureDefinition output = CreateOutputStructure(GetRequiredStructureType(entryPoint.Function.ReturnType));
                Functions.Remove(entryPoint);
                entryPoint = entryPoint.WithReturnType(new TypeReference(output.Name));
                Functions.Add(entryPoint);
            }

            if (function.Type == ShaderFunctionType.FragmentEntryPoint)
            {
                // HLSL pixel shader inputs also need these semantics.
                StructureDefinition modifiedInput = CreateOutputStructure(input);
                Functions.Remove(entryPoint);
                entryPoint = entryPoint.WithParameter(0, new TypeReference(modifiedInput.Name));
                Functions.Add(entryPoint);
            }

            foreach (StructureDefinition sd in Structures)
            {
                WriteStructure(sb, sd);
            }
            foreach (StructureDefinition sd in _synthesizedStructures)
            {
                WriteStructure(sb, sd);
            }

            int uniformBinding = 0, textureBinding = 0, samplerBinding = 0;
            foreach (ResourceDefinition rd in Resources)
            {
                switch (rd.ResourceKind)
                {
                    case ShaderResourceKind.Uniform:
                        WriteUniform(sb, rd, uniformBinding++);
                        break;
                    case ShaderResourceKind.Texture2D:
                        WriteTexture2D(sb, rd, textureBinding++);
                        break;
                    case ShaderResourceKind.TextureCube:
                        WriteTextureCube(sb, rd, textureBinding++);
                        break;
                    case ShaderResourceKind.Sampler:
                        WriteSampler(sb, rd, samplerBinding++);
                        break;
                    default: throw new ShaderGenerationException("Illegal resource kind: " + rd.ResourceKind);
                }
            }

            string result = new HlslMethodVisitor(Compilation, entryPoint.Function, this).Visit(entryPoint.Block);
            sb.AppendLine(result);

            return sb.ToString();
        }

        private StructureDefinition GetRequiredStructureType(TypeReference type)
        {
            StructureDefinition result = Structures.SingleOrDefault(sd => sd.Name == type.Name);
            if (result == null)
            {
                result = _synthesizedStructures.SingleOrDefault(sd => sd.Name == type.Name);
                if (result == null)
                {
                    if (!TryDiscoverStructure(type.Name))
                    {
                        throw new InvalidOperationException("Type referred by was not discovered: " + type.Name);
                    }
                }
            }

            return result;
        }

        private bool TryDiscoverStructure(string name)
        {
            INamedTypeSymbol type = Compilation.GetTypeByMetadataName(name);
            SyntaxNode declaringSyntax = type.OriginalDefinition.DeclaringSyntaxReferences[0].GetSyntax();
            if (declaringSyntax is StructDeclarationSyntax sds)
            {
                if (ShaderSyntaxWalker.TryGetStructDefinition(Compilation.GetSemanticModel(sds.SyntaxTree), sds, out StructureDefinition sd))
                {
                    Structures.Add(sd);
                    return true;
                }
            }

            return false;
        }

        private StructureDefinition CreateOutputStructure(StructureDefinition sd)
        {
            if (sd.Name.EndsWith(FragmentSemanticsSuffix))
            {
                return sd;
            }

            string newName = sd.Name + FragmentSemanticsSuffix;
            StructureDefinition existing = _synthesizedStructures.SingleOrDefault(ssd => ssd.Name == newName);
            if (existing != null)
            {
                return existing;
            }

            StructureDefinition clone = new StructureDefinition(newName, sd.Fields);
            _synthesizedStructures.Add(clone);
            return clone;
        }

        protected override string FormatInvocationCore(string type, string method, InvocationParameterInfo[] parameterInfos)
        {
            return HlslKnownFunctions.TranslateInvocation(type, method, parameterInfos);
        }

        protected override string CSharpToIdentifierNameCore(string typeName, string identifier)
        {
            return HlslKnownIdentifiers.GetMappedIdentifier(typeName, identifier);
        }

        private struct HlslSemanticTracker
        {
            public int Position;
            public int TexCoord;
            public int Normal;
            public int Tangent;
            public int Color;
        }
    }
}
