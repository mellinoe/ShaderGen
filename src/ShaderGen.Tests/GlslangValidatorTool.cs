using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ShaderGen.Tests
{
    public static class GlsLangValidatorTool
    {
        private static readonly string s_toolPath = FindExe();

        public static void AssertCompilesCode(string code, string type, bool vulkanSemantics)
        {
            using (TempFile tmpFile = new TempFile())
            {
                File.WriteAllText(tmpFile, code);
                AssertCompilesFile(tmpFile, type, vulkanSemantics);
            }
        }

        public static void AssertCompilesFile(string file, string type, bool vulkanSemantics, string output = null)
        {
            if (s_toolPath == null)
            {
                return;
            }

            ToolResult result = Compile(file, type, vulkanSemantics, output);
            if (result.ExitCode != 0)
            {
                string message = result.StdOut;
                throw new InvalidOperationException("GLSL compilation failed: " + message);
            }
        }

        private static ToolResult Compile(string file, string type, bool vulkanSemantics, string output = null)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = s_toolPath,
                Arguments = FormatArgs(file, type, vulkanSemantics, output),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            Process p = Process.Start(psi);
            p.WaitForExit(4000);

            string stdOut = p.StandardOutput.ReadToEnd();
            string stdError = p.StandardError.ReadToEnd();
            return new ToolResult(p.ExitCode, stdOut, stdError);
        }

        private static string FormatArgs(string file, string type, bool vulkanSemantics, string output = null)
        {
            StringBuilder args = new StringBuilder();
            if (vulkanSemantics)
            {
                args.Append("-V ");
            }
            args.Append("-S "); args.Append(type);
            if (output != null)
            {
                args.Append(" -o "); args.Append(output);
            }
            args.Append(" "); args.Append(file);
            return args.ToString();
        }


        private static string FindExe()
        {
            // First, try to launch from the current environment.
            try
            {
                Process.Start("glslangvalidator").WaitForExit();
                return "glslangvalidator";
            }
            catch { }

            // Check if the Vulkan SDK is installed, and use the compiler bundled there.
            const string VulkanSdkEnvVar = "VULKAN_SDK";
            string vulkanSdkPath = Environment.GetEnvironmentVariable(VulkanSdkEnvVar);
            if (vulkanSdkPath != null)
            {
                string exeExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
                string exePath = Path.Combine(vulkanSdkPath, "bin", "glslangvalidator" + exeExtension);
                if (File.Exists(exePath))
                {
                    return exePath;
                }
            }

            return null;
        }
    }
}
