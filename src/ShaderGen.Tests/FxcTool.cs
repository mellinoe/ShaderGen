using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ShaderGen.Tests
{
    public static class FxcTool
    {
        private static readonly string s_fxcLocation = FindFxcExe();

        public static FxcToolResult Compile(string file, string profile, string entryPoint, string output)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = s_fxcLocation,
                Arguments = FormatArgs(file, profile, entryPoint, output),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            Process p = Process.Start(psi);
            p.WaitForExit();

            string stdOut = p.StandardOutput.ReadToEnd();
            string stdError = p.StandardError.ReadToEnd();
            return new FxcToolResult(p.ExitCode, stdOut, stdError);
        }

        private static string FormatArgs(string file, string profile, string entryPoint, string output)
        {
            return $"/T {profile} /E {entryPoint} /Fo {output} {file}";
        }

        private static string FindFxcExe()
        {
            const string WindowsKitsFolder = @"C:\Program Files (x86)\Windows Kits";
            IEnumerable<string> paths = Directory.EnumerateFiles(
                WindowsKitsFolder,
                "fxc.exe",
                SearchOption.AllDirectories);
            string path = paths.FirstOrDefault(s => !s.Contains("arm"));
            if (path == null)
            {
                throw new InvalidOperationException("Couldn't locate fxc.exe.");
            }

            return path;
        }
    }

    public class FxcToolResult
    {
        public int ExitCode { get; }
        public string StdOut { get; }
        public string StdError { get; }

        public FxcToolResult(int exitCode, string stdOut, string stdError)
        {
            ExitCode = exitCode;
            StdOut = stdOut;
            StdError = stdError;
        }
    }
}
