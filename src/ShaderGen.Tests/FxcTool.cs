using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ShaderGen.Tests
{
    public static class FxcTool
    {
        private static readonly string SFxcLocation = FindFxcExe();

        public static void AssertCompilesCode(string code, string profile, string entryPoint)
        {
            using (TempFile tmpFile = new TempFile())
            {
                File.WriteAllText(tmpFile, code);
                AssertCompilesFile(tmpFile, profile, entryPoint);
            }
        }

        public static void AssertCompilesFile(string file, string profile, string entryPoint, string output = null)
        {
            if (SFxcLocation == null)
            {
                return;
            }

            ToolResult result = Compile(file, profile, entryPoint, output);
            if (result.ExitCode != 0)
            {
                string message = result.StdError;
                throw new InvalidOperationException("HLSL compilation failed: " + message);
            }
        }

        public static ToolResult Compile(string file, string profile, string entryPoint, string output = null)
        {
            var psi = new ProcessStartInfo {
                FileName = SFxcLocation,
                Arguments = FormatArgs(file, profile, entryPoint, output),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var p = Process.Start(psi);
            Assert.NotNull(p);
            p.WaitForExit(4000);

            string stdOut = p.StandardOutput.ReadToEnd();
            string stdError = p.StandardError.ReadToEnd();
            return new ToolResult(p.ExitCode, stdOut, stdError);
        }

        private static string FormatArgs(string file, string profile, string entryPoint, string output = null)
        {
            StringBuilder args = new StringBuilder();
            args.Append("/T "); args.Append(profile);
            args.Append(" /E "); args.Append(entryPoint);
            if (output != null)
            {
                args.Append(" /Fo "); args.Append(output);
            }
            args.Append(" "); args.Append(file);
            return args.ToString();
        }

        private static string FindFxcExe()
        {
            const string windowsKitsFolder = @"C:\Program Files (x86)\Windows Kits";
            string path = null;
            if (Directory.Exists(windowsKitsFolder))
            {
                IEnumerable<string> paths = Directory.EnumerateFiles(
                    windowsKitsFolder,
                    "fxc.exe",
                    SearchOption.AllDirectories);
                path = paths.FirstOrDefault(s => !s.Contains("arm"));
            }

            return path;
        }
    }

    public class ToolResult
    {
        public int ExitCode { get; }
        public string StdOut { get; }
        public string StdError { get; }

        public ToolResult(int exitCode, string stdOut, string stdError)
        {
            ExitCode = exitCode;
            StdOut = stdOut;
            StdError = stdError;
        }
    }
}
