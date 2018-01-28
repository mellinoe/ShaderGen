using System.Diagnostics;
using System.IO;

namespace ShaderGen.Metal {
    public class MetalBinaryCompiler : IBinaryCompiler {
        public LanguageBackend Language { get; }
        
        public string GeneratedFileExtension => "metal";

        public MetalBinaryCompiler(LanguageBackend language) {
            Language = language;
        }
        
        private const string MetalPath = @"/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/usr/bin/metal";
        private const string MetalLibPath = @"/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/usr/bin/metallib";

        private bool? _metalToolsAvailable;

        public virtual bool CompilationToolsAreAvailable() {
            if (!_metalToolsAvailable.HasValue)
            {
                _metalToolsAvailable = File.Exists(MetalPath) && File.Exists(MetalLibPath);
            }

            return _metalToolsAvailable.Value;
        }

        public virtual bool CompileCode(string shaderPath, string entryPoint, ShaderFunctionType type, out string path) {
            string shaderPathWithoutExtension = Path.ChangeExtension(shaderPath, null);
            string outputPath = shaderPathWithoutExtension + ".metallib";
            string bitcodePath = Path.GetTempFileName();
            string metalArgs = $"-x metal -o {bitcodePath} {shaderPath}";
            try
            {
                var metalPsi = new ProcessStartInfo(MetalPath, metalArgs) {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                var metalProcess = Process.Start(metalPsi);
                metalProcess.WaitForExit();
                if (metalProcess.ExitCode != 0)
                {
                    throw new ShaderGenerationException(metalProcess.StandardError.ReadToEnd());
                }

                string metallibArgs = $"-o {outputPath} {bitcodePath}";
                var metallibPsi = new ProcessStartInfo(MetalLibPath, metallibArgs) {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                var metallibProcess = Process.Start(metallibPsi);
                metallibProcess.WaitForExit();
                if (metallibProcess.ExitCode != 0)
                {
                    throw new ShaderGenerationException(metallibProcess.StandardError.ReadToEnd());
                }

                path = outputPath;
                return true;
            }
            finally
            {
                File.Delete(bitcodePath);
            }
        }
    }
}