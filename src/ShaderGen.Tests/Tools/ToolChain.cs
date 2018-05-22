using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Metal;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace ShaderGen.Tests.Tools
{
    /// <summary>
    /// A Tool Chain to complete compilation of a shader for a particular <see cref="LanguageBackend" />.
    /// </summary>
    public class ToolChain
    {
        public const int DefaultTimeout = 15000;
        private const string DefaultMetalPath = @"/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/usr/bin/metal";
        private delegate string ArgumentFormatterDelegate(string file, Stage stage, string entryPoint, string output = null);

        /// <summary>
        /// All the currently available tools by <see cref="LanguageBackend">Backend</see> <see cref="Type"/>.
        /// </summary>
        private static readonly IReadOnlyDictionary<Type, ToolChain> _toolChainsByBackendType;

        /// <summary>
        /// All the currently available tools by <see cref="GraphicsBackend"/>.
        /// </summary>
        private static readonly IReadOnlyDictionary<GraphicsBackend, ToolChain> _toolChainsByGraphicsBackend;

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
        /// Gets all known <see cref="ToolChain">ToolChains</see>.
        /// </summary>
        /// <value>
        /// All.
        /// </value>
        public static IEnumerable<ToolChain> All => _toolChainsByBackendType.Values;

        /// <summary>
        /// Gets all known backend types.
        /// </summary>
        /// <value>
        /// All backend types.
        /// </value>
        public static IEnumerable<Type> AllBackendTypes => _toolChainsByBackendType.Keys;

        /// <summary>
        /// Initializes the <see cref="ToolChain"/> class.
        /// </summary>
        static ToolChain()
        {
            List<ToolChain> tools = new List<ToolChain>();

            string fxcExe = FindFxcPath();
            Hlsl = new ToolChain(typeof(HlslBackend), GraphicsBackend.Direct3D11, c => new HlslBackend(c), CreateHeadlessD3D, fxcExe, FxcArguments);
            tools.Add(Hlsl);

            string glslvExe = FindGlslvPath();

            string NonVulkan(string f, Stage s, string e, string o) => GlsvArguments(f, s, e, false, o);
            GlslEs300 = new ToolChain(typeof(GlslEs300Backend), GraphicsBackend.OpenGLES, c => new GlslEs300Backend(c), () => CreateHeadlessGL(GraphicsBackend.OpenGLES), glslvExe, NonVulkan);
            Glsl330 = new ToolChain(typeof(Glsl330Backend), GraphicsBackend.OpenGL, c => new Glsl330Backend(c),
                () => CreateHeadlessGL(GraphicsBackend.OpenGL), glslvExe, NonVulkan);
            Glsl450 = new ToolChain(typeof(Glsl450Backend), GraphicsBackend.Vulkan, c => new Glsl450Backend(c), CreateHeadlessVulkan, glslvExe, (f, s, e, o) => GlsvArguments(f, s, e, true, o));
            tools.Add(GlslEs300);
            tools.Add(Glsl330);
            tools.Add(Glsl450);

            string metalPath = FindMetalPath();
            Metal = new ToolChain(typeof(MetalBackend), GraphicsBackend.Metal, c => new MetalBackend(c),
                CreateHeadlessMetal, metalPath,
                MetalArguments, Encoding.UTF8);
            tools.Add(Metal);

            // Set lookup dictionarys
            _toolChainsByBackendType = tools.ToDictionary(t => t.BackendType);
            _toolChainsByGraphicsBackend = tools.ToDictionary(t => t.GraphicsBackend);
        }

        /// <summary>
        /// Gets the <see cref="ToolChain"/> for the specified backend type.
        /// </summary>
        /// <param name="backendType">Type of the backend.</param>
        /// <returns>A <see cref="ToolChain"/> if available; otherwise <see langword="null"/>.</returns>
        public static ToolChain Get(Type backendType) =>
            _toolChainsByBackendType.TryGetValue(backendType, out ToolChain toolChain) ? toolChain : null;

        /// <summary>
        /// Gets the <see cref="ToolChain" /> for the specified backend.
        /// </summary>
        /// <param name="backend">The backend.</param>
        /// <returns>
        /// A <see cref="ToolChain" /> if available; otherwise <see langword="null" />.
        /// </returns>
        public static ToolChain Get(LanguageBackend backend) =>
            _toolChainsByBackendType.TryGetValue(backend.GetType(), out ToolChain toolChain) ? toolChain : null;

        /// <summary>
        /// Gets the <see cref="ToolChain" /> for the specified <see cref="GraphicsBackend"/>.
        /// </summary>
        /// <param name="graphicsBackend">The graphics backend.</param>
        /// <returns>
        /// A <see cref="ToolChain" /> if available; otherwise <see langword="null" />.
        /// </returns>
        public static ToolChain Get(GraphicsBackend graphicsBackend) =>
            _toolChainsByGraphicsBackend.TryGetValue(graphicsBackend, out ToolChain toolChain) ? toolChain : null;

        /// <summary>
        /// The graphics backend.
        /// </summary>
        public readonly GraphicsBackend GraphicsBackend;

        /// <summary>
        /// The name of the backend this tool supports.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The backend type.
        /// </summary>
        public readonly Type BackendType;

        /// <summary>
        /// Gets a value indicating whether this <see cref="ToolChain"/> is available for compilation.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if this <see cref="ToolChain"/> is available; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsAvailable => _toolPath != null;

        /// <summary>
        /// Indicates whether a headless graphics device is available.
        /// </summary>
        public readonly bool HeadlessAvailable;

        /// <summary>
        /// The tool path (currently only single executables supported).
        /// </summary>
        private readonly string _toolPath;

        /// <summary>
        /// The argument formatter.
        /// </summary>
        private readonly ArgumentFormatterDelegate _argumentFormatter;

        /// <summary>
        /// The preferred file encoding for the tool.
        /// </summary>
        private readonly Encoding _preferredFileEncoding;

        /// <summary>
        /// The function to create a <see cref="LanguageBackend"/>.
        /// </summary>
        private readonly Func<Compilation, LanguageBackend> _createBackend;

        /// <summary>
        /// Function to create a headless graphics device.
        /// </summary>
        private readonly Func<GraphicsDevice> _createHeadless;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolChain" /> class.  For now tool chains are single tools, but this
        /// could be easily extended to support multiple steps.
        /// </summary>
        /// <param name="backendType">Type of the backend.</param>
        /// <param name="graphicsBackend">The graphics backend.</param>
        /// <param name="createBackend">The function to create the backend.</param>
        /// <param name="createHeadless">The function to create a headless graphics device.</param>
        /// <param name="toolPath">The tool path.</param>
        /// <param name="argumentFormatter">The argument formatter.</param>
        /// <param name="preferredFileEncoding">The preferred file encoding.</param>
        /// <exception cref="ArgumentOutOfRangeException">backendType</exception>
        private ToolChain(
            Type backendType,
            GraphicsBackend graphicsBackend,
            Func<Compilation, LanguageBackend> createBackend,
            Func<GraphicsDevice> createHeadless,
            string toolPath,
            ArgumentFormatterDelegate argumentFormatter,
            Encoding preferredFileEncoding = default(Encoding))
        {
            if (!backendType.IsSubclassOf(typeof(LanguageBackend)))
                throw new ArgumentOutOfRangeException(nameof(backendType),
                    $"{backendType.Name} is not a descendent of {nameof(LanguageBackend)}.");

            // Calculate name (strip 'Backend' if present).
            Name = backendType.Name;
            if (Name.EndsWith("Backend", StringComparison.InvariantCultureIgnoreCase))
                Name = Name.Substring(0, Name.Length - 7);

            BackendType = backendType;
            _createBackend = createBackend;
            _createHeadless = createHeadless;
            _toolPath = string.IsNullOrWhiteSpace(toolPath) ? null : toolPath;
            _argumentFormatter = argumentFormatter;
            GraphicsBackend = graphicsBackend;
            _preferredFileEncoding = preferredFileEncoding ?? Encoding.Default;

            if (_toolPath != null &&
                _createHeadless != null &&
                GraphicsDevice.IsBackendSupported(GraphicsBackend))
            {
                try
                {
                    // Try to create a headless graphics device
                    using (_createHeadless()) { }

                    HeadlessAvailable = true;
                }
                catch
                {
                    HeadlessAvailable = false;

                }
            }
            else HeadlessAvailable = false;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{Name} tool chain";

        /// <summary>
        /// Creates the backend.
        /// </summary>
        /// <param name="compilation">The compilation.</param>
        /// <returns></returns>
        public LanguageBackend CreateBackend(Compilation compilation) => _createBackend(compilation);

        /// <summary>
        /// Creates a headless <see cref="GraphicsDevice" />
        /// </summary>
        /// <returns></returns>
        public GraphicsDevice CreateHeadless() => _createHeadless();

        /// <summary>
        /// Compiles the specified path.
        /// </summary>
        /// <param name="code">The shader code.</param>
        /// <param name="stage">The stage.</param>
        /// <param name="entryPoint">The entry point.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ToolResult Compile(string code, Stage stage, string entryPoint, int timeout = DefaultTimeout)
        {
            using (TempFile tmpFile = new TempFile())
            {
                File.WriteAllText(tmpFile, code, _preferredFileEncoding);
                return CompileFile(tmpFile, code, stage, entryPoint, timeout);
            }
        }

        /// <summary>
        /// Compiles the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="stage">The stage.</param>
        /// <param name="entryPoint">The entry point.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ToolResult CompileFile(string path, Stage stage, string entryPoint, int timeout = DefaultTimeout)
        {
            string code = File.ReadAllText(path);
            return CompileFile(path, code, stage, entryPoint, timeout);
        }

        /// <summary>
        /// Compiles the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="code">The code.</param>
        /// <param name="stage">The stage.</param>
        /// <param name="entryPoint">The entry point.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private ToolResult CompileFile(string path, string code, Stage stage, string entryPoint, int timeout = DefaultTimeout)
        {
            if (!IsAvailable)
                throw new InvalidOperationException($"The {Name} tool chain is not available!");

            using (TempFile tempFile = new TempFile())
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = _toolPath,
                    Arguments = _argumentFormatter(path, stage, entryPoint, tempFile),
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    // Add handlers to handle data
                    // ReSharper disable AccessToDisposedClosure
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            outputWaitHandle.Set();
                        else
                            output.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            errorWaitHandle.Set();
                        else
                            error.AppendLine(e.Data);
                    };
                    // ReSharper restore AccessToDisposedClosure

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    int exitCode;
                    if (!process.WaitForExit(timeout) || !outputWaitHandle.WaitOne(timeout) ||
                        !errorWaitHandle.WaitOne(timeout))
                    {
                        if (output.Length > 0) output.AppendLine("TIMED OUT!").AppendLine();
                        error.AppendLine($"Timed out calling: \"{_toolPath}\" {process.StartInfo.Arguments}");
                        exitCode = int.MinValue;
                    }
                    else
                        exitCode = process.ExitCode;

                    // Get compiled output (if any), otherwise use the source code.
                    byte[] outputBytes = File.ReadAllBytes(tempFile);
                    if (outputBytes.Length < 1)
                        outputBytes = _preferredFileEncoding.GetBytes(code);

                    return new ToolResult(this, code, exitCode, output.ToString(), error.ToString(), outputBytes);
                }
            }
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
            return File.Exists(exePath) ? exePath : null;
        }

        private static string GlsvArguments(string file, Stage stage, string entrypoint, bool vulkanSemantics, string output)
        {
            StringBuilder args = new StringBuilder();
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

            if (vulkanSemantics)
            {
                args.Append(" -V");

                // Only support file output for Vulkan
                if (output != null)
                    args.Append($" -o \"{output}\"");
            }

            args.Append($" \"{file}\"");
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
            args.Append(output != null ? $"\"outputFile\"" : " -o /dev/null");
            args.Append($" \"{file}\"");
            return args.ToString();
        }

        private static GraphicsDevice CreateHeadlessGL(GraphicsBackend backend)
        {
            WindowCreateInfo windowCreateInfo = new WindowCreateInfo
            {
                X = 0,
                Y = 0,
                WindowWidth = 1,
                WindowHeight = 1,
                WindowTitle = "Headless Graphics",
                WindowInitialState = WindowState.Hidden
            };
            Sdl2Window window = VeldridStartup.CreateWindow(ref windowCreateInfo);
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                true,
                PixelFormat.R16_UNorm,
                true,
                ResourceBindingModel.Improved);
            return VeldridStartup.CreateDefaultOpenGLGraphicsDevice(options, window, backend);
        }

        private static GraphicsDevice CreateHeadlessVulkan() =>
            GraphicsDevice.CreateVulkan(
                new GraphicsDeviceOptions(
                    true,
                    PixelFormat.R16_UNorm,
                    true,
                    ResourceBindingModel.Improved));

        private static GraphicsDevice CreateHeadlessD3D() =>
            GraphicsDevice.CreateD3D11(
                new GraphicsDeviceOptions(
                    true,
                    PixelFormat.R16_UNorm,
                    true,
                    ResourceBindingModel.Improved));

        private static GraphicsDevice CreateHeadlessMetal() =>
            GraphicsDevice.CreateMetal(
                new GraphicsDeviceOptions(
                    true,
                    PixelFormat.R16_UNorm,
                    true,
                    ResourceBindingModel.Improved));
    }
}
