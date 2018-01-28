using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ShaderGen.Hlsl {
    public class HlslBinaryCompiler : IBinaryCompiler {
        public LanguageBackend Language { get; }
        
        public string GeneratedFileExtension => "hlsl";

        public HlslBinaryCompiler(LanguageBackend language) {
            Language = language;
        }
        
        public virtual bool CompileCode(string shaderPath, string entryPoint, ShaderFunctionType type, out string path)
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

        public virtual bool CompilationToolsAreAvailable()
        {
            if (_fxcAvailable.HasValue) return _fxcAvailable.Value;
            
            _fxcPath = FindFxcExe();
            _fxcAvailable = _fxcPath != null;
            return _fxcAvailable.Value;
        }
    }
}