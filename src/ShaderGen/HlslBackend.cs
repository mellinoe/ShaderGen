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
        private const string FragmentSemanticsSuffix = "__FRAGSEMANTICS";

        private readonly List<StructureDefinition> _synthesizedStructures = new List<StructureDefinition>();

        public HlslBackend(Compilation compilation) : base(compilation)
        {
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return HlslKnownTypes.GetMappedName(fullType)
                .Replace(".", "_")
                .Replace("+", "_");
        }

        protected void WriteStructure(StringBuilder sb, StructureDefinition sd)
        {
            bool fragmentSemantics = sd.Name.EndsWith(FragmentSemanticsSuffix);
            sb.AppendLine($"struct {CSharpToShaderType(sd.Name)}");
            sb.AppendLine("{");
            HlslSemanticTracker tracker = new HlslSemanticTracker();
            StringBuilder fb = new StringBuilder();
            foreach (FieldDefinition field in sd.Fields)
            {
                fb.Append(CSharpToShaderType(field.Type.Name.Trim()));
                fb.Append(' ');
                fb.Append(CorrectIdentifier(field.Name.Trim()));
                int arrayCount = field.ArrayElementCount;
                if (arrayCount > 0)
                {
                    fb.Append('['); fb.Append(arrayCount); fb.Append(']');
                }
                fb.Append(HlslSemantic(field.SemanticType, fragmentSemantics, ref tracker));
                fb.Append(';');
                sb.Append("    ");
                sb.AppendLine(fb.ToString());
                fb.Clear();
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
                        if (!tracker.emittedSvPosition)
                        {
                            tracker.emittedSvPosition = true;
                            return " : SV_POSITION";
                        }
                        else
                        {
                            int val = tracker.Position++;
                            return " : POSITION" + val.ToString();
                        }
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
                default: throw new ShaderGenerationException("Invalid semantic type: " + semanticType);
            }
        }

        private void WriteSampler(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"SamplerState {CorrectIdentifier(rd.Name)} : register(s{binding});");
            sb.AppendLine();
        }

        private void WriteTexture2D(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"Texture2D {CorrectIdentifier(rd.Name)} : register(t{binding});");
            sb.AppendLine();
        }

        private void WriteTextureCube(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"TextureCube {CorrectIdentifier(rd.Name)} : register(t{binding});");
            sb.AppendLine();
        }

        private void WriteUniform(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"cbuffer {rd.Name}Buffer : register(b{binding})");
            sb.AppendLine("{");
            sb.AppendLine($"    {CSharpToShaderType(rd.ValueType.Name)} {CorrectIdentifier(rd.Name.Trim())};");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        protected override string GenerateFullTextCore(string setName, ShaderFunction function)
        {
            StringBuilder sb = new StringBuilder();

            ShaderFunctionAndBlockSyntax entryPoint = GetContext(setName).Functions.SingleOrDefault(
                sfabs => sfabs.Function.Name == function.Name);
            if (entryPoint == null)
            {
                throw new ShaderGenerationException("Couldn't find given function: " + function.Name);
            }

            ValidateRequiredSemantics(setName, entryPoint.Function, function.Type);

            StructureDefinition input = GetRequiredStructureType(setName, entryPoint.Function.Parameters[0].Type);

            if (function.Type == ShaderFunctionType.VertexEntryPoint)
            {
                // HLSL vertex outputs needs to have semantics applied to the structure fields.
                StructureDefinition output = CreateOutputStructure(GetRequiredStructureType(setName, entryPoint.Function.ReturnType));
                GetContext(setName).Functions.Remove(entryPoint);
                entryPoint = entryPoint.WithReturnType(new TypeReference(output.Name));
                GetContext(setName).Functions.Add(entryPoint);
            }

            if (function.Type == ShaderFunctionType.FragmentEntryPoint)
            {
                // HLSL pixel shader inputs also need these semantics.
                StructureDefinition modifiedInput = CreateOutputStructure(input);
                GetContext(setName).Functions.Remove(entryPoint);
                entryPoint = entryPoint.WithParameter(0, new TypeReference(modifiedInput.Name));
                GetContext(setName).Functions.Add(entryPoint);
            }

            foreach (StructureDefinition sd in GetContext(setName).Structures)
            {
                WriteStructure(sb, sd);
            }
            foreach (StructureDefinition sd in _synthesizedStructures)
            {
                WriteStructure(sb, sd);
            }

            int uniformBinding = 0, textureBinding = 0, samplerBinding = 0;
            foreach (ResourceDefinition rd in GetContext(setName).Resources)
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

            string result = new HlslMethodVisitor(Compilation, entryPoint.Function, this)
                .VisitFunction(entryPoint.Block);
            sb.AppendLine(result);

            return sb.ToString();
        }

        protected override StructureDefinition GetRequiredStructureType(string setName, TypeReference type)
        {
            StructureDefinition result = GetContext(setName).Structures.SingleOrDefault(sd => sd.Name == type.Name);
            if (result == null)
            {
                result = _synthesizedStructures.SingleOrDefault(sd => sd.Name == type.Name);
                if (result == null)
                {
                    if (!TryDiscoverStructure(setName, type.Name, out result))
                    {
                        throw new ShaderGenerationException("Type referred by was not discovered: " + type.Name);
                    }
                }
            }

            return result;
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

            public bool emittedSvPosition;
        }

        internal override string CorrectIdentifier(string identifier)
        {
            return identifier;
        }
    }
}
