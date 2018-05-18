using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Metal;

namespace ShaderGen.Tests.Tools
{
    /// <summary>
    /// A Tool Chain to complete compilation of a shader for a particular <see cref="LanguageBackend" />.
    /// </summary>
    public class ToolChain
    {
        private const string DefaultMetalPath = @"/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/usr/bin/metal";
        private delegate string ArgumentFormatterDelegate(string file, Stage stage, string entryPoint, string output = null);

        /// <summary>
        /// All the currently available tools by <see cref="LanguageBackend"/>.
        /// </summary>
        private static readonly IReadOnlyDictionary<Type, ToolChain> _toolChains;

        /// <summary>
        /// The HLSL tool chain.
        /// </summary>
        public static readonly ToolChain Hlsl;

        /// <summary>
        /// The GLSL es300 tool chain.
        /// </summary>
        public static readonly ToolChain GlslEs300;

        /// <summary>
        /// The GLSL330 tool chain.
        /// </summary>
        public static readonly ToolChain Glsl330;

        /// <summary>
        /// The GLSL450 tool chain.
        /// </summary>
        public static readonly ToolChain Glsl450;

        /// <summary>
        /// The metal tool chain.
        /// </summary>
        public static readonly ToolChain Metal;

        /// <summary>
        /// Initializes the <see cref="ToolChain"/> class.
        /// </summary>
        static ToolChain()
        {
            List<ToolChain> tools = new List<ToolChain>();

            string fxcExe = FindFxcPath();
            Hlsl = new ToolChain(typeof(HlslBackend), fxcExe, FxcArguments);
            tools.Add(Hlsl);

            string glslvExe = FindGlslvPath();

            string NonVulkan(string f, Stage s, string e, string o) => GlsvArguments(f, s, e, false, o);
            GlslEs300 = new ToolChain(typeof(GlslEs300Backend), glslvExe, NonVulkan);
            Glsl330 = new ToolChain(typeof(Glsl330Backend), glslvExe, NonVulkan);
            Glsl450 = new ToolChain(typeof(Glsl450Backend), glslvExe, (f, s, e, o) => GlsvArguments(f, s, e, true, o));
            tools.Add(GlslEs300);
            tools.Add(Glsl330);
            tools.Add(Glsl450);
            
            string metalPath = FindMetalPath();
            Metal = new ToolChain(typeof(MetalBackend), metalPath, MetalArguments);
            tools.Add(Metal);

            // Set lookup dictionary
            _toolChains = tools.ToDictionary(t => t.BackendType);
        }

        /// <summary>
        /// Gets the <see cref="ToolChain"/> for the specified backend type.
        /// </summary>
        /// <param name="backendType">Type of the backend.</param>
        /// <returns>A <see cref="ToolChain"/> if available; otherwise <see langword="null"/>.</returns>
        public static ToolChain Get(Type backendType) =>
            _toolChains.TryGetValue(backendType, out ToolChain toolChain) ? toolChain : null;

        /// <summary>
        /// The name of the backend this tool supports.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The backend type.
        /// </summary>
        public readonly Type BackendType;

        /// <summary>
        /// Gets a value indicating whether this <see cref="ToolChain"/> is available.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if this <see cref="ToolChain"/> is available; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsAvailable => _toolPath != null;

        /// <summary>
        /// The tool path (currently only single executables supported).
        /// </summary>
        private readonly string _toolPath;

        /// <summary>
        /// The argument formatter.
        /// </summary>
        private readonly ArgumentFormatterDelegate _argumentFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolChain"/> class.  For now tool chains are single tools, but this
        /// could be easily extended to support multiple steps.
        /// </summary>
        /// <param name="backendType">Type of the backend.</param>
        /// <param name="toolPath">The tool path.</param>
        /// <param name="argumentFormatter">The argument formatter.</param>
        /// <exception cref="ArgumentOutOfRangeException">backendType</exception>
        private ToolChain(Type backendType, string toolPath, ArgumentFormatterDelegate argumentFormatter)
        {
            if (!backendType.IsSubclassOf(typeof(LanguageBackend)))
                throw new ArgumentOutOfRangeException(nameof(backendType),
                    $"{backendType.Name} is not a descendent of {nameof(LanguageBackend)}.");

            // Calculate name (strip 'Backend' if present).
            Name = backendType.Name;
            if (Name.EndsWith("Backend", StringComparison.InvariantCultureIgnoreCase))
                Name = Name.Substring(0, Name.Length - 7);

            BackendType = backendType;
            _toolPath = string.IsNullOrWhiteSpace(toolPath) ? null : toolPath;
            _argumentFormatter = argumentFormatter;
        }

        public LanguageBackend GetBackend(Compilation compilation)
        {

        }

        public void AssertCompilesCode(string code, Stage stage, string entryPoint)
        {
            using (TempFile tmpFile = new TempFile())
            {
                File.WriteAllText(tmpFile, code, Encoding.UTF8);
                AssertCompilesFile(tmpFile, stage, entryPoint);
            }
        }

        public void AssertCompilesFile(string file, Stage stage, string entryPoint, string output = null)
        {
            ToolResult result = Compile(file, stage, entryPoint, output);
            if (result.ExitCode != 0)
            {
                string message = result.StdError;
                throw new InvalidOperationException($"{Name} compilation failed: " + message);
            }
        }

        private ToolResult Compile(string file, Stage stage, string entryPoint, string output = null)
        {
            if (!IsAvailable)
                throw new InvalidOperationException($"The {Name} tool chain is not available!");

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = _toolPath,
                Arguments = _argumentFormatter(file, stage, entryPoint, output),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            Process p = Process.Start(psi);
            p.WaitForExit(4000);

            string stdOut = p.StandardOutput.ReadToEnd();
            string stdError = p.StandardError.ReadToEnd();
            return new ToolResult(p.ExitCode, stdOut, stdError);
        }


        /*
         * FXC Tool
         */
        private static string FindFxcPath()
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

        private static string FxcArguments(string file, Stage stage, string entryPoint, string output)
        {
            StringBuilder args = new StringBuilder();
            args.Append("/T ");
            switch (stage)
            {
                case Stage.Vertex:
                    args.Append("vs_5_0");
                    break;
                case Stage.Fragment:
                    args.Append("ps_5_0");
                    break;
                case Stage.Compute:
                    args.Append("cs_5_0");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }

            args.Append($" /E \"{entryPoint}\"");
            if (output != null)
                args.Append($" /Fo \"{output}\"");

            args.Append($" \"{file}\"");
            return args.ToString();
        }

        /*
         * GLSLangValidator tool
         */
        private static string FindGlslvPath()
        {
            // First, try to launch from the current environment.
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo()
                {
                    FileName = "glslangvalidator",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                Process p = Process.Start(psi);
                p.StandardOutput.ReadToEndAsync();
                p.StandardError.ReadToEndAsync();
                p.WaitForExit(2000);
                return "glslangvalidator";
            }
            catch { }

            // Check if the Vulkan SDK is installed, and use the compiler bundled there.
            const string VulkanSdkEnvVar = "VULKAN_SDK";
            string vulkanSdkPath = Environment.GetEnvironmentVariable(VulkanSdkEnvVar);
            if (vulkanSdkPath == null) return null;

            string exeExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
            string exePath = Path.Combine(vulkanSdkPath, "bin", "glslangvalidator" + exeExtension);
            if (File.Exists(exePath))
            {
                return exePath;
            }

            return null;
        }

        private static string GlsvArguments(string file, Stage stage, string entrypoint, bool vulkanSemantics, string output)
        {
            StringBuilder args = new StringBuilder();
            if (vulkanSemantics)
                args.Append("-V ");
            args.Append("-S ");
            switch (stage)
            {
                case Stage.Vertex:
                    args.Append("vert");
                    break;
                case Stage.Fragment:
                    args.Append("frag");
                    break;
                case Stage.Compute:
                    args.Append("comp");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
            if (output != null)
                args.Append($" -o \"{output}\"");

            args.Append($" {file}");
            return args.ToString();
        }

        /*
         * Metal tool
         */
        private static string FindMetalPath() => File.Exists(DefaultMetalPath) ? DefaultMetalPath : null;

        private static string MetalArguments(string file, Stage stage, string entrypoint, string output)
        {
            StringBuilder args = new StringBuilder();
            args.Append("-x metal ");
            args.Append("-mmacosx-version-min=10.12 ");
            args.Append($" -o ");
            args.Append(output != null ? $"\"output\"" : " -o /dev/null");
            args.Append($" \"{file}\"");
            return args.ToString();
        }
    }
}
