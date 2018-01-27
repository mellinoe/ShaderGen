using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ShaderGen.Glsl
{
    public class Glsl450Backend : GlslBackendBase
    {
        public override string GeneratedFileExtension => "450.glsl";
        
        private static bool? _glslangValidatorAvailable;

        public override bool CompilationToolsAreAvailable()
        {
            if (!_glslangValidatorAvailable.HasValue)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("glslangValidator");
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    Process.Start(psi);
                    _glslangValidatorAvailable = true;
                }
                catch { _glslangValidatorAvailable = false; }
            }

            return _glslangValidatorAvailable.Value;
        }

        public override bool CompileCode(string shaderPath, string entryPoint, ShaderFunctionType type, out string path) {
            string stage = type == ShaderFunctionType.VertexEntryPoint ? "vert"
                : type == ShaderFunctionType.FragmentEntryPoint ? "frag"
                : "comp";
            string outputPath = shaderPath + ".spv";
            string args = $"-V -S {stage} {shaderPath} -o {outputPath}";
            try
            {

                ProcessStartInfo psi = new ProcessStartInfo("glslangValidator", args);
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                Process p = Process.Start(psi);
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    path = outputPath;
                    return true;
                }
                else
                {
                    throw new ShaderGenerationException(p.StandardOutput.ReadToEnd());
                }
            }
            catch (Win32Exception)
            {
                Console.WriteLine("Unable to launch glslangValidator tool.");
            }

            path = null;
            return false;
        }

        public Glsl450Backend(Compilation compilation) : base(compilation)
        {
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return GlslKnownTypes.GetMappedName(fullType, true)
                .Replace(".", "_")
                .Replace("+", "_");
        }

        protected override void WriteVersionHeader(ShaderFunction function, StringBuilder sb)
        {
            sb.AppendLine("#version 450");
            sb.AppendLine("#extension GL_ARB_separate_shader_objects : enable");
            sb.AppendLine("#extension GL_ARB_shading_language_420pack : enable");
        }

        protected override void WriteUniform(StringBuilder sb, ResourceDefinition rd)
        {
            string layout = FormatLayoutStr(rd);
            sb.AppendLine($"{layout} uniform {rd.Name}");
            sb.AppendLine("{");
            sb.AppendLine($"    {CSharpToShaderType(rd.ValueType.Name)} field_{CorrectIdentifier(rd.Name.Trim())};");
            sb.AppendLine("};");
            sb.AppendLine();
        }

        protected override void WriteStructuredBuffer(StringBuilder sb, ResourceDefinition rd, bool isReadOnly)
        {
            string layout = FormatLayoutStr(rd, "std140");
            string readOnlyStr = isReadOnly ? " readonly" : " ";
            sb.AppendLine($"{layout}{readOnlyStr} buffer {rd.Name}");
            sb.AppendLine("{");
            sb.AppendLine($"    {CSharpToShaderType(rd.ValueType.Name)} field_{CorrectIdentifier(rd.Name.Trim())}[];");
            sb.AppendLine("};");
        }

        protected override void WriteSampler(StringBuilder sb, ResourceDefinition rd)
        {
            sb.Append(FormatLayoutStr(rd));
            sb.Append(' ');
            sb.Append("uniform sampler ");
            sb.Append(CorrectIdentifier(rd.Name));
            sb.AppendLine(";");
        }

        protected override void WriteTexture2D(StringBuilder sb, ResourceDefinition rd)
        {
            sb.Append(FormatLayoutStr(rd));
            sb.Append(' ');
            sb.Append("uniform texture2D ");
            sb.Append(CorrectIdentifier(rd.Name));
            sb.AppendLine(";");
        }

        protected override void WriteTextureCube(StringBuilder sb, ResourceDefinition rd)
        {
            sb.Append(FormatLayoutStr(rd));
            sb.Append(' ');
            sb.Append("uniform textureCube ");
            sb.Append(CorrectIdentifier(rd.Name));
            sb.AppendLine(";");
        }

        protected override void WriteTexture2DMS(StringBuilder sb, ResourceDefinition rd)
        {
            sb.Append(FormatLayoutStr(rd));
            sb.Append(' ');
            sb.Append("uniform texture2DMS ");
            sb.Append(CorrectIdentifier(rd.Name));
            sb.AppendLine(";");
        }

        protected override void WriteInOutVariable(
            StringBuilder sb,
            bool isInVar,
            bool isVertexStage,
            string normalizedType,
            string normalizedIdentifier,
            int index)
        {
            string qualifier = isInVar ? "in" : "out";
            string identifier;
            if ((isVertexStage && isInVar) || (!isVertexStage && !isInVar))
            {
                identifier = normalizedIdentifier;
            }
            else
            {
                identifier = $"fsin_{index}";
            }
            sb.AppendLine($"layout(location = {index}) {qualifier} {normalizedType} {identifier};");

        }

        protected override string FormatInvocationCore(string setName, string type, string method, InvocationParameterInfo[] parameterInfos)
        {
            return Glsl450KnownFunctions.TranslateInvocation(type, method, parameterInfos);
        }

        private string FormatLayoutStr(ResourceDefinition rd, string storageSpec = null)
        {
            string storageSpecPart = storageSpec != null ? $"{storageSpec}, " : string.Empty;
            return $"layout({storageSpecPart}set = {rd.Set}, binding = {rd.Binding})";
        }

        protected override void EmitGlPositionCorrection(StringBuilder sb)
        {
            sb.AppendLine($"        gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates");
        }
    }
}
