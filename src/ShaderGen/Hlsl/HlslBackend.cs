using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ShaderGen.Hlsl
{
    public class HlslBackend : LanguageBackend
    {
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
            sb.AppendLine($"BAD CODE struct {CSharpToShaderType(sd.Name)}");
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
                fb.Append(HlslSemantic(field.SemanticType, ref tracker));
                fb.Append(';');
                sb.Append("    ");
                sb.AppendLine(fb.ToString());
                fb.Clear();
            }
            sb.AppendLine("};");
            sb.AppendLine();
        }

        private string HlslSemantic(SemanticType semanticType, ref HlslSemanticTracker tracker)
        {
            switch (semanticType)
            {
                case SemanticType.None:
                    return string.Empty;
                case SemanticType.Position:
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
                case SemanticType.SystemPosition:
                    {
                        return " : SV_Position";
                    }
                case SemanticType.ColorTarget:
                    {
                        int val = tracker.ColorTarget++;
                        return " : SV_Target" + val.ToString();
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

        private void WriteTexture2DMS(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"Texture2DMS<float4> {CorrectIdentifier(rd.Name)} : register(t{binding});");
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

        private void WriteStructuredBuffer(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"StructuredBuffer<{CSharpToShaderType(rd.ValueType.Name)}> {CorrectIdentifier(rd.Name)}: register(t{binding});");
        }

        private void WriteRWStructuredBuffer(StringBuilder sb, ResourceDefinition rd, int binding)
        {
            sb.AppendLine($"RWStructuredBuffer<{CSharpToShaderType(rd.ValueType.Name)}> {CorrectIdentifier(rd.Name)}: register(u{binding});");
        }

        protected override MethodProcessResult GenerateFullTextCore(string setName, ShaderFunction function)
        {
            Debug.Assert(function.IsEntryPoint);

            StringBuilder sb = new StringBuilder();
            HashSet<ResourceDefinition> resourcesUsed = new HashSet<ResourceDefinition>();

            BackendContext setContext = GetContext(setName);
            ShaderFunctionAndMethodDeclarationSyntax entryPoint = setContext.Functions.SingleOrDefault(
                sfabs => sfabs.Function.Name == function.Name);
            if (entryPoint == null)
            {
                throw new ShaderGenerationException("Couldn't find given function: " + function.Name);
            }

            ValidateRequiredSemantics(setName, entryPoint.Function, function.Type);

            StructureDefinition[] orderedStructures
                = StructureDependencyGraph.GetOrderedStructureList(Compilation, setContext.Structures);
            foreach (StructureDefinition sd in orderedStructures)
            {
                WriteStructure(sb, sd);
            }

            List<ResourceDefinition[]> resourcesBySet = setContext.Resources.GroupBy(rd => rd.Set)
                .Select(g => g.ToArray()).ToList();

            StringBuilder functionsSB = new StringBuilder();
            foreach (ShaderFunctionAndMethodDeclarationSyntax f in entryPoint.OrderedFunctionList)
            {
                if (!f.Function.IsEntryPoint)
                {
                    MethodProcessResult processResult = new HlslMethodVisitor(Compilation, setName, f.Function, this).VisitFunction(f.MethodDeclaration);
                    foreach (ResourceDefinition rd in processResult.ResourcesUsed)
                    {
                        resourcesUsed.Add(rd);
                    }
                    functionsSB.AppendLine(processResult.FullText);
                }
            }

            MethodProcessResult result = new HlslMethodVisitor(Compilation, setName, entryPoint.Function, this)
                .VisitFunction(entryPoint.MethodDeclaration);
            foreach (ResourceDefinition rd in result.ResourcesUsed)
            {
                resourcesUsed.Add(rd);
            }

            // Emit all of the resources now, because we've learned which ones are actually used by this function.
            int uniformBinding = 0, textureBinding = 0, samplerBinding = 0, uavBinding = function.ColorOutputCount;
            int setIndex = 0;
            foreach (ResourceDefinition[] set in resourcesBySet)
            {
                Debug.Assert(set[0].Set == setIndex);
                setIndex += 1;

                foreach (ResourceDefinition rd in set)
                {
                    switch (rd.ResourceKind)
                    {
                        case ShaderResourceKind.Uniform:
                            if (resourcesUsed.Contains(rd))
                            {
                                WriteUniform(sb, rd, uniformBinding);
                            }
                            uniformBinding++;
                            break;
                        case ShaderResourceKind.Texture2D:
                            if (resourcesUsed.Contains(rd))
                            {
                                WriteTexture2D(sb, rd, textureBinding);
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.TextureCube:
                            if (resourcesUsed.Contains(rd))
                            {
                                WriteTextureCube(sb, rd, textureBinding);
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.Texture2DMS:
                            if (resourcesUsed.Contains(rd))
                            {

                                WriteTexture2DMS(sb, rd, textureBinding);
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.Sampler:
                            if (resourcesUsed.Contains(rd))
                            {
                                WriteSampler(sb, rd, samplerBinding);
                            }
                            samplerBinding++;
                            break;
                        case ShaderResourceKind.StructuredBuffer:
                            if (resourcesUsed.Contains(rd))
                            {
                                WriteStructuredBuffer(sb, rd, textureBinding);
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.RWStructuredBuffer:
                            if (resourcesUsed.Contains(rd))
                            {
                                WriteRWStructuredBuffer(sb, rd, uavBinding);
                            }
                            uavBinding++;
                            break;
                        default: throw new ShaderGenerationException("Illegal resource kind: " + rd.ResourceKind);
                    }
                }
            }

            // Resources need to be defined before the function that uses them -- so append this after the resources.
            sb.Append(functionsSB.ToString());
            sb.AppendLine(result.FullText);

            return new MethodProcessResult(sb.ToString(), resourcesUsed);
        }

        protected override StructureDefinition GetRequiredStructureType(string setName, TypeReference type)
        {
            StructureDefinition result = GetContext(setName).Structures.SingleOrDefault(sd => sd.Name == type.Name);
            if (result == null)
            {
                if (!TryDiscoverStructure(setName, type.Name, out result))
                {
                    throw new ShaderGenerationException("Type referred by was not discovered: " + type.Name);
                }
            }

            return result;
        }

        protected override string FormatInvocationCore(string setName, string type, string method, InvocationParameterInfo[] parameterInfos)
        {
            return HlslKnownFunctions.TranslateInvocation(type, method, parameterInfos);
        }

        protected override string CSharpToIdentifierNameCore(string typeName, string identifier)
        {
            return HlslKnownIdentifiers.GetMappedIdentifier(typeName, identifier);
        }

        internal override string GetComputeGroupCountsDeclaration(UInt3 groupCounts)
        {
            return $"[numthreads({groupCounts.X}, {groupCounts.Y}, {groupCounts.Z})]";
        }

        internal override string ParameterDirection(ParameterDirection direction)
        {
            switch (direction)
            {
                case ShaderGen.ParameterDirection.Out:
                    return "out";
                case ShaderGen.ParameterDirection.InOut:
                    return "inout";
                default:
                    return string.Empty;
            }
        }

        private struct HlslSemanticTracker
        {
            public int Position;
            public int TexCoord;
            public int Normal;
            public int Tangent;
            public int Color;
            public int ColorTarget;
        }

        internal override string CorrectIdentifier(string identifier)
        {
            return identifier;
        }
    }
}
