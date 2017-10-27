using Microsoft.CodeAnalysis;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Diagnostics;

namespace ShaderGen
{
    public class HlslBackend : LanguageBackend
    {
        private const string FragmentSemanticsSuffix = "__FRAGSEMANTICS";

        private readonly Dictionary<string, List<StructureDefinition>> _synthesizedStructures
            = new Dictionary<string, List<StructureDefinition>>();

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
            Debug.Assert(function.IsEntryPoint);

            StringBuilder sb = new StringBuilder();
            BackendContext setContext = GetContext(setName);
            ShaderFunctionAndBlockSyntax entryPoint = setContext.Functions.SingleOrDefault(
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
                StructureDefinition output = CreateOutputStructure(setName, GetRequiredStructureType(setName, entryPoint.Function.ReturnType));
                setContext.Functions.Remove(entryPoint);
                entryPoint = entryPoint.WithReturnType(new TypeReference(output.Name));
                setContext.Functions.Add(entryPoint);
            }

            if (function.Type == ShaderFunctionType.FragmentEntryPoint)
            {
                // HLSL pixel shader inputs also need these semantics.
                StructureDefinition modifiedInput = CreateOutputStructure(setName, input);
                setContext.Functions.Remove(entryPoint);
                entryPoint = entryPoint.WithParameter(0, new TypeReference(modifiedInput.Name));
                setContext.Functions.Add(entryPoint);
            }

            StructureDefinition[] orderedStructures
                = StructureDependencyGraph.GetOrderedStructureList(Compilation, setContext.Structures);
            foreach (StructureDefinition sd in orderedStructures)
            {
                WriteStructure(sb, sd);
            }
            foreach (StructureDefinition sd in GetSynthesizedStructures(setName))
            {
                WriteStructure(sb, sd);
            }

            FunctionCallGraphDiscoverer fcgd = new FunctionCallGraphDiscoverer(
                Compilation,
                new TypeAndMethodName { TypeName = function.DeclaringType, MethodName = function.Name });
            fcgd.GenerateFullGraph();
            TypeAndMethodName[] orderedFunctionList = fcgd.GetOrderedCallList();

            List<ResourceDefinition[]> resourcesBySet = setContext.Resources.GroupBy(rd => rd.Set)
                .Select(g => g.ToArray()).ToList();

            int setIndex = 0;
            foreach (ResourceDefinition[] set in resourcesBySet)
            {
                Debug.Assert(set[0].Set == setIndex);
                setIndex += 1;

                int uniformBinding = 0, textureBinding = 0, samplerBinding = 0;
                foreach (ResourceDefinition rd in set)
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
            }

            foreach (TypeAndMethodName name in orderedFunctionList)
            {
                ShaderFunctionAndBlockSyntax f = setContext.Functions.Single(
                    sfabs => sfabs.Function.DeclaringType == name.TypeName && sfabs.Function.Name == name.MethodName);
                if (!f.Function.IsEntryPoint)
                {
                    sb.AppendLine(new HlslMethodVisitor(Compilation, setName, f.Function, this).VisitFunction(f.Block));
                }
            }

            string result = new HlslMethodVisitor(Compilation, setName, entryPoint.Function, this)
                .VisitFunction(entryPoint.Block);
            sb.AppendLine(result);

            return sb.ToString();
        }

        protected override StructureDefinition GetRequiredStructureType(string setName, TypeReference type)
        {
            StructureDefinition result = GetContext(setName).Structures.SingleOrDefault(sd => sd.Name == type.Name);
            if (result == null)
            {
                List<StructureDefinition> synthSDs = GetSynthesizedStructures(setName);
                result = synthSDs.SingleOrDefault(sd => sd.Name == type.Name);
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

        private StructureDefinition CreateOutputStructure(string setName, StructureDefinition sd)
        {
            if (sd.Name.EndsWith(FragmentSemanticsSuffix))
            {
                return sd;
            }

            string newName = sd.Name + FragmentSemanticsSuffix;
            List<StructureDefinition> synthesizedStructures = GetSynthesizedStructures(setName);
            StructureDefinition existing = synthesizedStructures.SingleOrDefault(ssd => ssd.Name == newName);
            if (existing != null)
            {
                return existing;
            }

            StructureDefinition clone = new StructureDefinition(newName, sd.Fields);
            synthesizedStructures.Add(clone);
            return clone;
        }

        protected override string FormatInvocationCore(string setName, string type, string method, InvocationParameterInfo[] parameterInfos)
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

        private List<StructureDefinition> GetSynthesizedStructures(string setName)
        {
            if (!_synthesizedStructures.TryGetValue(setName, out List<StructureDefinition> ret))
            {
                ret = new List<StructureDefinition>();
                _synthesizedStructures.Add(setName, ret);
            }

            return ret;
        }
    }
}
