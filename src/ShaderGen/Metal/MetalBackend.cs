using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ShaderGen.Metal
{
    public class MetalBackend : LanguageBackend
    {
        public MetalBackend(Compilation compilation) : base(compilation)
        {
        }

        private string CSharpToShaderTypeCore(string fullType, bool packed)
        {
            string mapped = packed
                ? MetalKnownTypes.GetPackedName(fullType)
                : MetalKnownTypes.GetMappedName(fullType);
            return mapped
                .Replace(".", "_")
                .Replace("+", "_");
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return CSharpToShaderTypeCore(fullType, false);
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
                string typeName = CSharpToShaderTypeCore(field.Type.Name, field.SemanticType == SemanticType.None);
                fb.Append(typeName);
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

        internal string GetResourceParameterList(ShaderFunction function, string setName, HashSet<ResourceDefinition> resourcesUsed)
        {
            BackendContext setContext = GetContext(setName);

            List<ResourceDefinition[]> resourcesBySet = setContext.Resources.GroupBy(rd => rd.Set)
                .Select(g => g.ToArray()).ToList();

            List<string> resourceArgList = new List<string>();
            int bufferBinding = 0;
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
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteUniform(rd, bufferBinding));
                            }
                            bufferBinding++;
                            break;
                        case ShaderResourceKind.Texture2D:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteTexture2D(rd, textureBinding));
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.Texture2DArray:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteTexture2DArray(rd, textureBinding));
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.TextureCube:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteTextureCube(rd, textureBinding));
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.Texture2DMS:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteTexture2DMS(rd, textureBinding));
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.Sampler:
                        case ShaderResourceKind.SamplerComparison:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteSampler(rd, samplerBinding));
                            }
                            samplerBinding++;
                            break;
                        case ShaderResourceKind.StructuredBuffer:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteStructuredBuffer(rd, bufferBinding));
                            }
                            bufferBinding++;
                            break;
                        case ShaderResourceKind.RWStructuredBuffer:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteRWStructuredBuffer(rd, bufferBinding));
                            }
                            bufferBinding++;
                            break;
                        case ShaderResourceKind.RWTexture2D:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteRWTexture2D(rd, textureBinding));
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.DepthTexture2D:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteDepthTexture2D(rd, textureBinding));
                            }
                            textureBinding++;
                            break;
                        case ShaderResourceKind.DepthTexture2DArray:
                            if (resourcesUsed.Contains(rd))
                            {
                                resourceArgList.Add(WriteDepthTexture2DArray(rd, textureBinding));
                            }
                            textureBinding++;
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

        private string WriteTexture2DArray(ResourceDefinition rd, int binding)
        {
            return $"texture2d_array<float> {rd.Name} [[ texture({binding}) ]]";
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
            return $"constant {CSharpToShaderType(rd.ValueType.Name)} *{rd.Name} [[ buffer({binding}) ]]";
        }

        private string WriteRWStructuredBuffer(ResourceDefinition rd, int binding)
        {
            return $"device {CSharpToShaderType(rd.ValueType.Name)} *{rd.Name} [[ buffer({binding}) ]]";
        }

        private string WriteRWTexture2D(ResourceDefinition rd, int binding)
        {
            return $"texture2d<float, access::read_write> {rd.Name} [[ texture({binding}) ]]";
        }

        private string WriteDepthTexture2D(ResourceDefinition rd, int binding)
        {
            return $"depth2d<float> {rd.Name} [[ texture({binding}) ]]";
        }

        private string WriteDepthTexture2DArray(ResourceDefinition rd, int binding)
        {
            return $"depth2d_array<float> {rd.Name} [[ texture({binding}) ]]";
        }

        protected override MethodProcessResult GenerateFullTextCore(string setName, ShaderFunction function)
        {
            Debug.Assert(function.IsEntryPoint);

            StringBuilder sb = new StringBuilder();
            BackendContext setContext = GetContext(setName);
            ShaderFunctionAndMethodDeclarationSyntax entryPoint = setContext.Functions.SingleOrDefault(
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

            HashSet<ResourceDefinition> resourcesUsed
                = ProcessFunctions(setName, entryPoint, out string funcsStr, out string entryStr);

            StringBuilder containerSB = new StringBuilder();
            containerSB.AppendLine("struct ShaderContainer {");

            List<string> structFields = new List<string>();
            List<string> ctorParams = new List<string>();
            List<string> ctorAssignments = new List<string>();

            foreach (ResourceDefinition resource in resourcesUsed)
            {
                structFields.Add(GetResourceField(resource));
                ctorParams.Add(GetResourceCtorParam(resource));
                ctorAssignments.Add($"{resource.Name}({resource.Name}_param)");
            }

            foreach (string sf in structFields)
            {
                containerSB.AppendLine(sf);
            }

            containerSB.AppendLine(funcsStr);

            // Emit the ctor definition
            containerSB.AppendLine($"ShaderContainer(");
            string ctorParamsStr = string.Join(", ", ctorParams);
            containerSB.AppendLine(ctorParamsStr);
            containerSB.AppendLine($")");
            string allCtorAssignments = string.Join(", ", ctorAssignments);
            if (!string.IsNullOrEmpty(allCtorAssignments))
            {
                containerSB.AppendLine(":");
                containerSB.AppendLine(allCtorAssignments);
            }
            containerSB.AppendLine("{}");

            containerSB.AppendLine(entryStr);

            containerSB.AppendLine("};"); // Close the global containing struct.
            sb.AppendLine(containerSB.ToString());

            // Emit the out function call which creates the container struct and calls the real shader function.
            string type = entryPoint.Function.Type == ShaderFunctionType.VertexEntryPoint
                ? "vertex"
                : entryPoint.Function.Type == ShaderFunctionType.FragmentEntryPoint
                    ? "fragment"
                    : "kernel";

            ShaderFunction entryFunction = entryPoint.Function;
            string returnType = CSharpToShaderType(entryFunction.ReturnType.Name);
            string fullDeclType = CSharpToShaderType(entryFunction.DeclaringType);
            string funcName = entryFunction.Name;
            string baseParameterList = string.Join(", ", entryFunction.Parameters.Select(FormatParameter));
            string resourceParameterList = GetResourceParameterList(entryFunction, setName, resourcesUsed);
            string builtinParameterList = string.Join(
                ", ",
                GetBuiltinParameterList(entryFunction).Select(b => $"{b.Type} {b.Name} {b.Attribute}"));
            string fullParameterList = string.Join(
                ", ",
                new string[]
                {
                    baseParameterList, resourceParameterList, builtinParameterList
                }.Where(s => !string.IsNullOrEmpty(s)));

            string functionDeclStr = $"{type} {returnType} {funcName}({fullParameterList})";

            string containerArgs = string.Join(", ", resourcesUsed.Select(
                rd => rd.Name));

            string entryFuncArgs = string.Join(
                ", ",
                MetalBackend.GetBuiltinParameterList(entryFunction).Select(b => $"{b.Name}"));

            if (entryFunction.Parameters.Length > 0)
            {
                Debug.Assert(entryFunction.Parameters.Length == 1);
                entryFuncArgs = Utilities.JoinIgnoreNull(
                    ", ",
                    new string[] { $"{entryFunction.Parameters[0].Name}", entryFuncArgs });
            }

            sb.AppendLine(functionDeclStr);
            sb.AppendLine("{");
            sb.AppendLine($"return ShaderContainer({containerArgs}).{entryFunction.Name}({entryFuncArgs});");
            sb.AppendLine("}");

            return new MethodProcessResult(sb.ToString(), resourcesUsed);
        }

        private string FormatParameter(ParameterDefinition pd)
        {
            return $"{CSharpToShaderType(pd.Type)} {CorrectIdentifier(pd.Name)} [[ stage_in ]]";
        }

        public static List<(string Type, string Name, string Attribute)> GetBuiltinParameterList(ShaderFunction function)
        {
            List<(string, string, string)> values = new List<(string, string, string)>();
            if (function.UsesVertexID)
            {
                values.Add(("uint", "_builtins_VertexID", "[[ vertex_id ]]"));
            }
            if (function.UsesInstanceID)
            {
                values.Add(("uint", "_builtins_InstanceID", "[[ instance_id ]]"));
            }
            if (function.UsesDispatchThreadID)
            {
                values.Add(("uint3", "_builtins_DispatchThreadID", "[[ thread_position_in_grid ]]"));
            }
            if (function.UsesGroupThreadID)
            {
                values.Add(("uint3", "_builtins_GroupThreadID", "[[ thread_position_in_threadgroup ]]"));
            }
            if (function.UsesFrontFace)
            {
                values.Add(("bool", "_builtins_IsFrontFace", "[[ front_facing ]]"));
            }

            return values;
        }

        private string GetResourceField(ResourceDefinition rd)
        {
            switch (rd.ResourceKind)
            {
                case ShaderResourceKind.Texture2D:
                    return $"thread texture2d<float> {rd.Name};";
                case ShaderResourceKind.Texture2DArray:
                    return $"thread texture2d_array<float> {rd.Name};";
                case ShaderResourceKind.Texture2DMS:
                    return $"thread texture2d_ms<float> {rd.Name};";
                case ShaderResourceKind.TextureCube:
                    return $"thread texturecube<float> {rd.Name};";
                case ShaderResourceKind.Sampler:
                case ShaderResourceKind.SamplerComparison:
                    return $"thread sampler {rd.Name};";
                case ShaderResourceKind.Uniform:
                    return $"constant {CSharpToShaderType(rd.ValueType.Name)}& {rd.Name};";
                case ShaderResourceKind.StructuredBuffer:
                    return $"constant {CSharpToShaderType(rd.ValueType.Name)}* {rd.Name};";
                case ShaderResourceKind.RWStructuredBuffer:
                    return $"device {CSharpToShaderType(rd.ValueType.Name)}* {rd.Name};";
                case ShaderResourceKind.RWTexture2D:
                    return $"texture2d<float, access::read_write> {rd.Name};";
                case ShaderResourceKind.DepthTexture2D:
                    return $"thread depth2d<float> {rd.Name};";
                case ShaderResourceKind.DepthTexture2DArray:
                    return $"thread depth2d_array<float> {rd.Name};";
                default:
                    Debug.Fail("Invalid ResourceKind: " + rd.ResourceKind);
                    throw new InvalidOperationException();
            }
        }

        private string GetResourceCtorParam(ResourceDefinition rd)
        {
            switch (rd.ResourceKind)
            {
                case ShaderResourceKind.Texture2D:
                    return $"thread texture2d<float> {rd.Name}_param";
                case ShaderResourceKind.Texture2DArray:
                    return $"thread texture2d_array<float> {rd.Name}_param";
                case ShaderResourceKind.Texture2DMS:
                    return $"thread texture2d_ms<float> {rd.Name}_param";
                case ShaderResourceKind.TextureCube:
                    return $"thread texturecube<float> {rd.Name}_param";
                case ShaderResourceKind.Sampler:
                case ShaderResourceKind.SamplerComparison:
                    return $"thread sampler {rd.Name}_param";
                case ShaderResourceKind.Uniform:
                    return $"constant {CSharpToShaderType(rd.ValueType.Name)}& {rd.Name}_param";
                case ShaderResourceKind.StructuredBuffer:
                    return $"constant {CSharpToShaderType(rd.ValueType.Name)}* {rd.Name}_param";
                case ShaderResourceKind.RWStructuredBuffer:
                    return $"device {CSharpToShaderType(rd.ValueType.Name)}* {rd.Name}_param";
                case ShaderResourceKind.RWTexture2D:
                    return $"texture2d<float, access::read_write> {rd.Name}_param";
                case ShaderResourceKind.DepthTexture2D:
                    return $"thread depth2d<float> {rd.Name}_param";
                case ShaderResourceKind.DepthTexture2DArray:
                    return $"thread depth2d_array<float> {rd.Name}_param";
                default:
                    throw new InvalidOperationException("Invalid ResourceKind: " + rd.ResourceKind);
            }
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

        internal override bool IsIndexerAccess(SymbolInfo member)
        {
            string containingType = Utilities.GetFullMetadataName(member.Symbol.ContainingType);
            char memberNameFirstChar = member.Symbol.Name[0];
            return
                (containingType == "System.Numerics.Matrix4x4"
                    && memberNameFirstChar == 'M'
                    && char.IsDigit(member.Symbol.Name[1]))
                    ||
                ((containingType.StartsWith("System.Numerics.Vector")
                    || containingType.StartsWith("ShaderGen.UInt")
                    || containingType.StartsWith("ShaderGen.Int"))
                    && (memberNameFirstChar == 'X'
                        || memberNameFirstChar == 'Y'
                        || memberNameFirstChar == 'Z'
                        || memberNameFirstChar == 'W'));
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

        internal override string ParameterDirection(ParameterDirection direction)
        {
            switch (direction)
            {
                case ShaderGen.ParameterDirection.Out:
                case ShaderGen.ParameterDirection.InOut:
                    return "&";
                default:
                    return string.Empty;
            }
        }

        protected override ShaderMethodVisitor VisitShaderMethod(string setName, ShaderFunction func)
        {
            return new MetalMethodVisitor(Compilation, setName, func, this);
        }
    }
}
