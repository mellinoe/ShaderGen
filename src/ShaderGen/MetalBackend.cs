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
    public class MetalBackend : LanguageBackend
    {
        public MetalBackend(Compilation compilation) : base(compilation)
        {
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return MetalKnownTypes.GetMappedName(fullType)
                .Replace(".", "_")
                .Replace("+", "_");
        }

        protected void WriteStructure(StringBuilder sb, StructureDefinition sd)
        {
            sb.AppendLine($"struct {CSharpToShaderType(sd.Name)}");
            sb.AppendLine("{");
            StringBuilder fb = new StringBuilder();
            uint attribute = 0;
            uint colorTarget = 0;
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

                if (field.SemanticType == SemanticType.SystemPosition)
                {
                    fb.Append($" [[ position ]]");
                }
                else if (field.SemanticType == SemanticType.ColorTarget)
                {
                    fb.Append($" [[ color({colorTarget++}) ]]");
                }
                else if (field.SemanticType != SemanticType.None)
                {
                    fb.Append($" [[ attribute({attribute++}) ]]");
                }
                fb.Append(';');
                sb.Append("    ");
                sb.AppendLine(fb.ToString());
                fb.Clear();
            }
            sb.AppendLine("};");
            sb.AppendLine();
        }

        internal string GetResourceParameterList(ShaderFunction function, string setName)
        {
            BackendContext setContext = GetContext(setName);

            List<ResourceDefinition[]> resourcesBySet = setContext.Resources.GroupBy(rd => rd.Set)
                .Select(g => g.ToArray()).ToList();

            List<string> resourceArgList = new List<string>();
            int bufferBinding = 0;
            if (function.Type == ShaderFunctionType.VertexEntryPoint
                && function.Parameters.Length > 0)
            {
                bufferBinding = 1;
            }

            int textureBinding = 0;
            int samplerBinding = 0;
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
                            resourceArgList.Add(WriteUniform(rd, bufferBinding++));
                            break;
                        case ShaderResourceKind.Texture2D:
                            resourceArgList.Add(WriteTexture2D(rd, textureBinding++));
                            break;
                        case ShaderResourceKind.TextureCube:
                            resourceArgList.Add(WriteTextureCube(rd, textureBinding++));
                            break;
                        case ShaderResourceKind.Texture2DMS:
                            resourceArgList.Add(WriteTexture2DMS(rd, textureBinding++));
                            break;
                        case ShaderResourceKind.Sampler:
                            resourceArgList.Add(WriteSampler(rd, samplerBinding++));
                            break;
                        case ShaderResourceKind.StructuredBuffer:
                            resourceArgList.Add(WriteStructuredBuffer(rd, bufferBinding++));
                            break;
                        case ShaderResourceKind.RWStructuredBuffer:
                            resourceArgList.Add(WriteRWStructuredBuffer(rd, bufferBinding++));
                            break;
                        default: throw new ShaderGenerationException("Illegal resource kind: " + rd.ResourceKind);
                    }
                }
            }

            return string.Join(", ", resourceArgList);
        }

        private string WriteSampler(ResourceDefinition rd, int binding)
        {
            return $"sampler {rd.Name} [[ sampler({binding}) ]]";
        }

        private string WriteTexture2D(ResourceDefinition rd, int binding)
        {
            return $"texture2d<float> {rd.Name} [[ texture({binding}) ]]";
        }

        private string WriteTextureCube(ResourceDefinition rd, int binding)
        {
            return $"texturecube<float> {rd.Name} [[ texture({binding}) ]]";
        }

        private string WriteTexture2DMS(ResourceDefinition rd, int binding)
        {
            return $"texture2d_ms<float> {rd.Name} [[ texture({binding}) ]]";
        }

        private string WriteUniform(ResourceDefinition rd, int binding)
        {
            return $"constant {CSharpToShaderType(rd.ValueType.Name)} &{rd.Name} [[ buffer({binding}) ]]";
        }

        private string WriteStructuredBuffer(ResourceDefinition rd, int binding)
        {
            return $"constant {CSharpToShaderType(rd.ValueType.Name)} &{rd.Name} [[ buffer({binding}) ]]";
        }

        private string WriteRWStructuredBuffer(ResourceDefinition rd, int binding)
        {
            return $"device {CSharpToShaderType(rd.ValueType.Name)} *{rd.Name} [[ buffer({binding}) ]]";
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

            // Write header
            sb.AppendLine("#include <metal_stdlib>");
            sb.AppendLine("using namespace metal;");

            // TODO: Necessary for Metal?
            StructureDefinition[] orderedStructures
                = StructureDependencyGraph.GetOrderedStructureList(Compilation, setContext.Structures);
            foreach (StructureDefinition sd in orderedStructures)
            {
                WriteStructure(sb, sd);
            }

            FunctionCallGraphDiscoverer fcgd = new FunctionCallGraphDiscoverer(
                Compilation,
                new TypeAndMethodName { TypeName = function.DeclaringType, MethodName = function.Name });
            fcgd.GenerateFullGraph();
            // TODO: Necessary for Metal?
            TypeAndMethodName[] orderedFunctionList = fcgd.GetOrderedCallList();

            foreach (TypeAndMethodName name in orderedFunctionList)
            {
                ShaderFunctionAndBlockSyntax f = setContext.Functions.Single(
                    sfabs => sfabs.Function.DeclaringType == name.TypeName && sfabs.Function.Name == name.MethodName);
                if (!f.Function.IsEntryPoint)
                {
                    sb.AppendLine(new MetalMethodVisitor(Compilation, setName, f.Function, this).VisitFunction(f.Block));
                }
            }

            string result = new MetalMethodVisitor(Compilation, setName, entryPoint.Function, this)
                .VisitFunction(entryPoint.Block);
            sb.AppendLine(result);

            return sb.ToString();
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
            return MetalKnownFunctions.TranslateInvocation(type, method, parameterInfos);
        }

        protected override string CSharpToIdentifierNameCore(string typeName, string identifier)
        {
            return MetalKnownIdentifiers.GetMappedIdentifier(typeName, identifier);
        }

        internal override string GetComputeGroupCountsDeclaration(UInt3 groupCounts)
        {
            return string.Empty;
            // This isn't actually specified in the compute shader code in Metal.
            // It's specified when an MTLComputeCommandEncoder is created.
        }

        internal override string CorrectIdentifier(string identifier)
        {
            return identifier;
        }
    }
}
