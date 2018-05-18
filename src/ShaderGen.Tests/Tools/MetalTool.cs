using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ShaderGen.Tests.Tools
{
    public static class MetalTool
    {
        private static readonly string s_toolPath = FindExe();

        public static bool IsAvailable => s_toolPath != null;

        public static void AssertCompilesCode(string code)
        {
            using (TempFile tmpFile = new TempFile())
            {
                File.WriteAllText(tmpFile, code, Encoding.UTF8);
                Debug.WriteLine($"Wrote shader code to {tmpFile.FilePath}");
                AssertCompilesFile(tmpFile);
            }
        }

        public static void AssertCompilesFile(string file, string output = null)
        {
            if (!IsAvailable)
                throw new InvalidOperationException("Metal compilation unavailable!");

            ToolResult result = Compile(file, output);
            if (result.ExitCode != 0)
            {
                string message = result.StdError;
                throw new InvalidOperationException("Metal compilation failed: " + message);
            }
        }

        private static ToolResult Compile(string file, string output = null)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = s_toolPath,
                Arguments = FormatArgs(file, output),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            Process p = Process.Start(psi);
            p.WaitForExit(4000);

            string stdOut = p.StandardOutput.ReadToEnd();
            string stdError = p.StandardError.ReadToEnd();
            return new ToolResult(p.ExitCode, stdOut, stdError);
        }

        private static string FormatArgs(string file, string output = null)
        {
            StringBuilder args = new StringBuilder();
            args.Append("-x metal ");
            args.Append("-mmacosx-version-min=10.12 ");
            if (output != null)
            {
                args.Append(" -o "); args.Append(output);
            }
            else
            {
                args.Append(" -o /dev/null");
            }
            args.Append(" "); args.Append(file);
            return args.ToString();
        }


        private static string FindExe()
        {
            const string DefaultLocation = @"/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/usr/bin/metal";

            if (File.Exists(DefaultLocation))
            {
                return DefaultLocation;
            }
            else
            {
                return null;
            }
        }
    }
}
