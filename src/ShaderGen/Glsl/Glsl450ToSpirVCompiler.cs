using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ShaderGen.Glsl {
    public class Glsl450ToSpirVCompiler : IBinaryCompiler {
        public LanguageBackend Language { get; }
        
        public string GeneratedFileExtension => "450.glsl";

        public Glsl450ToSpirVCompiler(LanguageBackend backend) {
            Language = backend;
        }

        private bool? _glslangValidatorAvailable;

        public virtual bool CompilationToolsAreAvailable()
        {
            if (_glslangValidatorAvailable.HasValue) return _glslangValidatorAvailable.Value;
            
            try
            {
                var psi = new ProcessStartInfo("glslangValidator") {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                Process.Start(psi);
                _glslangValidatorAvailable = true;
            }
            catch { _glslangValidatorAvailable = false; }
            return _glslangValidatorAvailable.Value;
        }

        public virtual bool CompileCode(string shaderPath, string entryPoint, ShaderFunctionType type, out string path) {
            var stage = type == ShaderFunctionType.VertexEntryPoint ? "vert"
                : type == ShaderFunctionType.FragmentEntryPoint ? "frag"
                : "comp";
            var outputPath = shaderPath + ".spv";
            var args = $"-V -S {stage} {shaderPath} -o {outputPath}";
            try
            {

                var psi = new ProcessStartInfo("glslangValidator", args) {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                
                var p = Process.Start(psi);
                p.WaitForExit();
                if (p.ExitCode != 0) {
                    throw new ShaderGenerationException(p.StandardOutput.ReadToEnd());
                }
                
                path = outputPath;
                return true;
            }
            catch (Win32Exception)
            {
                Console.WriteLine("Unable to launch glslangValidator tool.");
            }

            path = null;
            return false;
        }
    }
}