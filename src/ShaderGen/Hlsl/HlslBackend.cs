using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ShaderGen.Hlsl
{
    public class HlslBackend : LanguageBackend
    {
        public override string GeneratedFileExtension => "hlsl";
        
        public HlslBackend(Compilation compilation) : base(compilation)
        {
        }
        
        public override bool CompileCode(string shaderPath, string entryPoint, ShaderFunctionType type, out string path)
        {
            try
            {
                string profile = type == ShaderFunctionType.VertexEntryPoint ? "vs_5_0"
                    : type == ShaderFunctionType.FragmentEntryPoint ? "ps_5_0"
                    : "cs_5_0";
                string outputPath = shaderPath + ".bytes";
                string args = $"/T {profile} /E {entryPoint} {shaderPath} /Fo {outputPath}";
                string fxcPath = FindFxcExe();
                ProcessStartInfo psi = new ProcessStartInfo(fxcPath, args);
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                Process p = new Process() { StartInfo = psi };
                p.Start();
                var stdOut = p.StandardOutput.ReadToEndAsync();
                var stdErr = p.StandardError.ReadToEndAsync();
                bool exited = p.WaitForExit(2000);

                if (exited && p.ExitCode == 0)
                {
                    path = outputPath;
                    return true;
                }
                else
                {
                    string message = $"StdOut: {stdOut.Result}, StdErr: {stdErr.Result}";
                    Console.WriteLine($"Failed to compile HLSL: {message}.");
                }
            }
            catch (Win32Exception)
            {
                Console.WriteLine("Unable to launch fxc tool.");
            }

            path = null;
            return false;
        }
        
        protected virtual string FindFxcExe()
        {
            const string windowsKitsFolder = @"C:\Program Files (x86)\Windows Kits";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Directory.Exists(windowsKitsFolder))
            {
                var paths = Directory.EnumerateFiles(
                    windowsKitsFolder,
                    "fxc.exe",
                    SearchOption.AllDirectories);
                return paths.FirstOrDefault(s => !s.Contains("arm"));
            }

            return null;
        }
        
        private bool? _fxcAvailable;
        private string _fxcPath;

        public override bool CompilationToolsAreAvailable()
        {
            if (_fxcAvailable.HasValue) return _fxcAvailable.Value;
            
            _fxcPath = FindFxcExe();
            _fxcAvailable = _fxcPath != null;
            return _fxcAvailable.Value;
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return HlslKnownTypes.GetMappedName(fullType)
                .Replace(".", "_")
                .Replace("+", "_");
        }

        protected void WriteStructure(StringBuilder sb, StructureDefinition sd)
        {
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
            ShaderFunctionAndBlockSyntax entryPoint = setContext.Functions.SingleOrDefault(
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

            FunctionCallGraphDiscoverer fcgd = new FunctionCallGraphDiscoverer(
                Compilation,
                new TypeAndMethodName { TypeName = function.DeclaringType, MethodName = function.Name });
            fcgd.GenerateFullGraph();
            TypeAndMethodName[] orderedFunctionList = fcgd.GetOrderedCallList();

            List<ResourceDefinition[]> resourcesBySet = setContext.Resources.GroupBy(rd => rd.Set)
                .Select(g => g.ToArray()).ToList();

            StringBuilder functionsSB = new StringBuilder();
            foreach (TypeAndMethodName name in orderedFunctionList)
            {
                ShaderFunctionAndBlockSyntax f = setContext.Functions.Single(
                    sfabs => sfabs.Function.DeclaringType == name.TypeName && sfabs.Function.Name == name.MethodName);
                if (!f.Function.IsEntryPoint)
                {
                    MethodProcessResult processResult = new HlslMethodVisitor(Compilation, setName, f.Function, this).VisitFunction(f.Block);
                    foreach (ResourceDefinition rd in processResult.ResourcesUsed)
                    {
                        resourcesUsed.Add(rd);
                    }
                    functionsSB.AppendLine(processResult.FullText);
                }
            }

            MethodProcessResult result = new HlslMethodVisitor(Compilation, setName, entryPoint.Function, this)
                .VisitFunction(entryPoint.Block);
            foreach (ResourceDefinition rd in result.ResourcesUsed)
            {
                resourcesUsed.Add(rd);
            }

            // Emit all of the resources now, because we've learned which ones are actually used by this function.
            int uniformBinding = 0, textureBinding = 0, samplerBinding = 0, uavBinding = function.ColorOutputCount;
            foreach (ResourceDefinition[] set in resourcesBySet)
            {
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
