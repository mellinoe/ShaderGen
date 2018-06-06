using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        /// <summary>
        /// The default timeout in ms to allow for a tool to run.
        /// </summary>
        public const int DefaultTimeout = 15000;

        /// <summary>
        /// Compiles and validates code in one step.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="stage">The compilation stage.</param>
        /// <param name="entryPoint">The entry point.</param>
        /// <returns></returns>
        private delegate CompileResult CompileDelegate(string code, Stage stage, string entryPoint);

        private const string WindowsKitsFolder = @"C:\Program Files (x86)\Windows Kits";
        private const string VulkanSdkEnvVar = "VULKAN_SDK";
        private const string DefaultMetalPath = @"/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/usr/bin/metal";
        private const string DefaultMetallibPath = @"/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/usr/bin/metallib";

        /// <summary>
        /// All the currently available tools by <see cref="LanguageBackend">Backend</see> <see cref="Type"/>.
        /// </summary>
        private static readonly Dictionary<Type, ToolChain> _toolChainsByBackendType
            = new Dictionary<Type, ToolChain>();

        /// <summary>
        /// All the currently available tools by <see cref="GraphicsBackend"/>.
        /// </summary>
        private static readonly Dictionary<GraphicsBackend, ToolChain> _toolChainsByGraphicsBackend
            = new Dictionary<GraphicsBackend, ToolChain>();

        /// <summary>
        /// The HLSL tool chain.
        /// </summary>
        public static readonly ToolChain Direct3D11;

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
        public static IEnumerable<ToolChain> All => _toolChainsByGraphicsBackend.Values;

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
            Direct3D11 = new ToolChain(
                GraphicsBackend.Direct3D11,
                typeof(HlslBackend),
                c => new HlslBackend(c),
                _fxcPath.Value != null ? FxcCompile : (CompileDelegate)null,
                CreateHeadlessD3D, null);
            GlslEs300 = new ToolChain(
                GraphicsBackend.OpenGLES,
                typeof(GlslEs300Backend),
                c => new GlslEs300Backend(c),
                _glslvPath.Value != null ? GLCompile : (CompileDelegate)null,
                () => CreateHeadlessGL(GraphicsBackend.OpenGLES),
                null);
            Glsl330 = new ToolChain(
                GraphicsBackend.OpenGL,
                typeof(Glsl330Backend),
                c => new Glsl330Backend(c),
                _glslvPath.Value != null ? GLCompile : (CompileDelegate)null,
                () => CreateHeadlessGL(GraphicsBackend.OpenGL),
                null);
            Glsl450 = new ToolChain(
                GraphicsBackend.Vulkan,
                typeof(Glsl450Backend),
                c => new Glsl450Backend(c),
                _glslvPath.Value != null ? VulkanCompile : (CompileDelegate)null,
                CreateHeadlessVulkan,
                null);
            Metal = new ToolChain(
                GraphicsBackend.Metal,
                typeof(MetalBackend),
                c => new MetalBackend(c),
                _metalPath.Value != null ? MetalCompile : (CompileDelegate)null,
                CreateHeadlessMetal,
                null);
        }

        /// <summary>
        /// Gets the <see cref="ToolChain"/> for the specified backend type.
        /// </summary>
        /// <param name="backendType">Type of the backend.</param>
        /// <returns>A <see cref="ToolChain"/> if available; otherwise <see langword="null"/>.</returns>
        public static ToolChain Get(Type backendType) =>
            _toolChainsByBackendType.TryGetValue(backendType, out ToolChain toolChain)
                ? toolChain
                : null;

        /// <summary>
        /// Gets the <see cref="ToolChain" /> for the specified backend.
        /// </summary>
        /// <param name="backend">The backend.</param>
        /// <returns>
        /// A <see cref="ToolChain" /> if available; otherwise <see langword="null" />.
        /// </returns>
        public static ToolChain Get(LanguageBackend backend) => backend != null ? Get(backend.GetType()) : null;


        /// <summary>
        /// Gets the <see cref="ToolChain" /> for the specified <see cref="GraphicsBackend"/>.
        /// </summary>
        /// <param name="graphicsBackend">The graphics backend.</param>
        /// <returns>
        /// A <see cref="ToolChain" /> if available; otherwise <see langword="null" />.
        /// </returns>
        public static ToolChain Get(GraphicsBackend graphicsBackend) =>
            _toolChainsByGraphicsBackend.TryGetValue(graphicsBackend, out ToolChain toolChain)
                ? toolChain
                : null;

        /// <summary>
        /// Gets the graphicsBackends, ensuring it has the required features.
        /// </summary>
        /// <param name="graphicsBackend">The graphics backend.</param>
        /// <returns></returns>
        /// <exception cref="ShaderGen.Tests.Tools.RequiredToolFeatureMissingException"></exception>
        public static ToolChain Require(
            GraphicsBackend graphicsBackend) =>
            Requires(ToolFeatures.All, true, graphicsBackend).SingleOrDefault();

        /// <summary>
        /// Gets the graphicsBackends, ensuring it has the required features.
        /// </summary>
        /// <param name="requiredFeatures">The required features.</param>
        /// <param name="graphicsBackend">The graphics backend.</param>
        /// <returns></returns>
        /// <exception cref="ShaderGen.Tests.Tools.RequiredToolFeatureMissingException"></exception>
        public static ToolChain Require(
            ToolFeatures requiredFeatures,
            GraphicsBackend graphicsBackend) =>
            Requires(requiredFeatures, true, graphicsBackend).SingleOrDefault();

        /// <summary>
        /// Gets the graphicsBackends, ensuring it has the required features.
        /// </summary>
        /// <param name="requiredFeatures">The required features.</param>
        /// <param name="throwOnFail">if set to <c>true</c> throws an error if any of the <paramref name="graphicsBackends" />
        /// do not have the <paramref name="requiredFeatures">required features</paramref>.</param>
        /// <param name="graphicsBackend">The graphics backend.</param>
        /// <returns></returns>
        /// <exception cref="ShaderGen.Tests.Tools.RequiredToolFeatureMissingException"></exception>
        public static ToolChain Require(
            ToolFeatures requiredFeatures,
            bool throwOnFail,
            GraphicsBackend graphicsBackend) =>
            Requires(requiredFeatures, throwOnFail, graphicsBackend).SingleOrDefault();

        /// <summary>
        /// Gets all the graphicsBackends, ensuring they have the required features.
        /// </summary>
        /// <param name="graphicsBackends">The graphicsBackends required (leave empty to get all).</param>
        /// <returns></returns>
        /// <exception cref="ShaderGen.Tests.Tools.RequiredToolFeatureMissingException"></exception>
        public static IReadOnlyList<ToolChain> Requires(params GraphicsBackend[] graphicsBackends)
            => Requires(ToolFeatures.All, true, (IEnumerable<GraphicsBackend>)graphicsBackends);

        /// <summary>
        /// Gets all the graphicsBackends, ensuring they have the required features.
        /// </summary>
        /// <param name="graphicsBackends">The graphicsBackends required (leave empty to get all).</param>
        /// <returns></returns>
        /// <exception cref="ShaderGen.Tests.Tools.RequiredToolFeatureMissingException"></exception>
        public static IReadOnlyList<ToolChain> Requires(IEnumerable<GraphicsBackend> graphicsBackends)
            => Requires(ToolFeatures.All, true, graphicsBackends);

        /// <summary>
        /// Gets all the graphicsBackends, ensuring they have the required features.
        /// </summary>
        /// <param name="requiredFeatures">The required features.</param>
        /// <param name="graphicsBackends">The graphicsBackends required (leave empty to get all).</param>
        /// <returns></returns>
        /// <exception cref="ShaderGen.Tests.Tools.RequiredToolFeatureMissingException"></exception>
        public static IReadOnlyList<ToolChain> Requires(ToolFeatures requiredFeatures,
            params GraphicsBackend[] graphicsBackends)
            => Requires(requiredFeatures, true, (IEnumerable<GraphicsBackend>)graphicsBackends);

        /// <summary>
        /// Gets all the graphicsBackends, ensuring they have the required features.
        /// </summary>
        /// <param name="requiredFeatures">The required features.</param>
        /// <param name="graphicsBackends">The graphicsBackends required (leave empty to get all).</param>
        /// <returns></returns>
        /// <exception cref="ShaderGen.Tests.Tools.RequiredToolFeatureMissingException"></exception>
        public static IReadOnlyList<ToolChain> Requires(ToolFeatures requiredFeatures, IEnumerable<GraphicsBackend> graphicsBackends)
            => Requires(requiredFeatures, true, graphicsBackends);

        /// <summary>
        /// Gets all the graphicsBackends, ensuring they have the required features.
        /// </summary>
        /// <param name="requiredFeatures">The required features.</param>
        /// <param name="throwOnFail">if set to <c>true</c> throws an error if any of the <paramref name="graphicsBackends" />
        /// do not have the <paramref name="requiredFeatures">required features</paramref>.</param>
        /// <param name="graphicsBackends">The graphicsBackends required (leave empty to get all).</param>
        /// <returns></returns>
        /// <exception cref="ShaderGen.Tests.Tools.RequiredToolFeatureMissingException"></exception>
        public static IReadOnlyList<ToolChain> Requires(ToolFeatures requiredFeatures, bool throwOnFail,
            params GraphicsBackend[] graphicsBackends)
            => Requires(requiredFeatures, throwOnFail, (IEnumerable<GraphicsBackend>)graphicsBackends);

        /// <summary>
        /// Gets the first tool chain (if any) with all the specified features.
        /// </summary>
        /// <param name="features">The features.</param>
        /// <returns></returns>
        public static ToolChain Get(ToolFeatures features) =>
            Requires(features, false).FirstOrDefault();

        /// <summary>
        /// Gets all the graphicsBackends, ensuring they have the required features.
        /// </summary>
        /// <param name="requiredFeatures">The required features.</param>
        /// <param name="throwOnFail">if set to <c>true</c> throws an error if any of the <paramref name="graphicsBackends" />
        /// do not have the <paramref name="requiredFeatures">required features</paramref>.</param>
        /// <param name="graphicsBackends">The graphics graphicsBackends.</param>
        /// <returns></returns>
        /// <exception cref="ShaderGen.Tests.Tools.RequiredToolFeatureMissingException"></exception>
        public static IReadOnlyList<ToolChain> Requires(
            ToolFeatures requiredFeatures,
            bool throwOnFail,
            IEnumerable<GraphicsBackend> graphicsBackends)
        {
            GraphicsBackend[] backends = (graphicsBackends ?? _toolChainsByGraphicsBackend.Keys).ToArray();
            if (backends.Length < 1)
            {
                backends = _toolChainsByGraphicsBackend.Keys.ToArray();
            }

            List<string> missingBackends = new List<string>(backends.Length);
            List<ToolChain> found = new List<ToolChain>(backends.Length);
            foreach (GraphicsBackend backend in backends)
            {
                ToolChain toolChain = Get(backend);
                if (toolChain == null)
                {
                    missingBackends.Add($"{backend} backend does not have a tool chain");
                    continue;
                }

                if (!toolChain.Features.HasFlag(requiredFeatures))
                {
                    missingBackends.Add(
                        $"{backend} tool chain does not have the required {~toolChain.Features & requiredFeatures} feature(s)");
                    continue;
                }

                found.Add(toolChain);
            }

            if (throwOnFail && missingBackends.Count > 0)
            {
                string last = missingBackends.LastOrDefault();
                throw new RequiredToolFeatureMissingException(
                    missingBackends.Count < 2
                        ? $"The {last}."
                        : $"The {string.Join(", ", missingBackends.Take(missingBackends.Count - 1))} and {last}.");
            }

            found.TrimExcess();
            return found.AsReadOnly();
        }

        /// <summary>
        /// The compilation function.
        /// </summary>
        private readonly CompileDelegate _compileFunction;

        /// <summary>
        /// The function to create a <see cref="LanguageBackend"/>.
        /// </summary>
        private readonly Func<Compilation, LanguageBackend> _createBackend;

        /// <summary>
        /// Function to create a headless graphics device.
        /// </summary>
        private readonly Func<GraphicsDevice> _createHeadless;

        /// <summary>
        /// TODO For future expansion, will need to review signature of function.
        /// Function to create a headless graphics device.
        /// </summary>
        private readonly Func<GraphicsDevice> _createWindowed;

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
        /// Gets a value indicating which <see cref="ToolFeatures"/> are available.
        /// </summary>
        public readonly ToolFeatures Features;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolChain" /> class.  For now tool chains are single tools, but this
        /// could be easily extended to support multiple steps.
        /// </summary>
        /// <param name="graphicsBackend">The graphics backend.</param>
        /// <param name="backendType">Type of the backend.</param>
        /// <param name="createBackend">The function to create the backend.</param>
        /// <param name="compileFunction">The compile function.</param>
        /// <param name="createHeadless">The function to create a headless graphics device.</param>
        /// <param name="createWindowed">The create windowed.</param>
        /// <exception cref="ArgumentOutOfRangeException">backendType</exception>
        private ToolChain(GraphicsBackend graphicsBackend,
            Type backendType,
            Func<Compilation, LanguageBackend> createBackend,
            CompileDelegate compileFunction,
            Func<GraphicsDevice> createHeadless,
            Func<GraphicsDevice> createWindowed)
        {
            if (!backendType.IsSubclassOf(typeof(LanguageBackend)))
            {
                throw new ArgumentOutOfRangeException(nameof(backendType),
                    $"{backendType.Name} is not a descendent of {nameof(LanguageBackend)}.");
            }

            BackendType = backendType;

            // Calculate name (strip 'Backend' if present).
            Name = backendType.Name;
            if (Name.EndsWith("Backend", StringComparison.InvariantCultureIgnoreCase))
            {
                Name = Name.Substring(0, Name.Length - 7);
            }

            GraphicsBackend = graphicsBackend;

            ToolFeatures features = ToolFeatures.None;

            if (createBackend != null)
            {
                _createBackend = createBackend;
                features |= ToolFeatures.Transpilation;
            }

            if (compileFunction != null)
            {
                _compileFunction = compileFunction;

                features |= ToolFeatures.Compilation;
            }

            // Don't allow creation of graphics devices on CI Servers.
            bool onCiServer = Environment.GetEnvironmentVariable("CI")?.ToLowerInvariant() == "true";
            if (!onCiServer &&
                createHeadless != null &&
                GraphicsDevice.IsBackendSupported(graphicsBackend))
            {
                try
                {
                    // Try to create a headless graphics device
                    using (createHeadless()) { }
                    _createHeadless = createHeadless;
                    features |= ToolFeatures.HeadlessGraphicsDevice;
                }
                catch
                {
                }
            }

            // TODO For future expansion, will need to review signature of function.
            if (!onCiServer &&
                createWindowed != null &&
                GraphicsDevice.IsBackendSupported(graphicsBackend))
            {
                try
                {
                    // Try to create a headless graphics device
                    using (createWindowed()) { }
                    _createWindowed = createWindowed;
                    features |= ToolFeatures.WindowedGraphicsDevice;
                }
                catch
                {
                }
            }

            Features = features;

            // Add to lookup dictionaries.
            _toolChainsByGraphicsBackend.Add(graphicsBackend, this);
            _toolChainsByBackendType.Add(backendType, this);
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
        public GraphicsDevice CreateHeadless() =>
            _createHeadless != null
                ? _createHeadless()
                : throw new InvalidOperationException(
                    $"The {GraphicsBackend} headless graphics device is not available on this system!");

        /// <summary>
        /// Compiles the specified path.
        /// </summary>
        /// <param name="code">The shader code.</param>
        /// <param name="stage">The stage.</param>
        /// <param name="entryPoint">The entry point.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public CompileResult Compile(string code, Stage stage, string entryPoint) =>
            _compileFunction != null
                ? _compileFunction(code, stage, entryPoint)
                : throw new InvalidOperationException(
                    $"The {GraphicsBackend} tool chain does not support compilation!");


        /// <summary>
        /// Executes a compile tool.
        /// </summary>
        /// <param name="toolPath">The tool path.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="code">The code.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="outputPath">The output path.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static CompileResult Execute(
            string toolPath,
            string arguments,
            string code,
            string outputPath = null,
            Encoding encoding = default(Encoding))
        {
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();
                // Add handlers to handle data
                // ReSharper disable AccessToDisposedClosure
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };
                // ReSharper restore AccessToDisposedClosure

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                int exitCode;
                if (!process.WaitForExit(DefaultTimeout) || !outputWaitHandle.WaitOne(DefaultTimeout) ||
                    !errorWaitHandle.WaitOne(DefaultTimeout))
                {
                    if (output.Length > 0)
                    {
                        output.AppendLine("TIMED OUT!").AppendLine();
                    }

                    error.AppendLine($"Timed out calling: \"{toolPath}\" {process.StartInfo.Arguments}");
                    exitCode = int.MinValue;
                }
                else
                {
                    exitCode = process.ExitCode;
                }

                // Get compiled output (if any).
                byte[] outputBytes;
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    // No output expected, just encode the existing code into bytes.
                    outputBytes = (encoding ?? Encoding.Default).GetBytes(code);
                }
                else
                {
                    if (File.Exists(outputPath))
                    {
                        try
                        {
                            // Attemp to read output file
                            outputBytes = File.ReadAllBytes(outputPath);
                        }
                        catch (Exception e)
                        {
                            outputBytes = Array.Empty<byte>();
                            error.AppendLine($"Failed to read the output file, \"{outputPath}\": {e.Message}");
                        }
                    }
                    else
                    {
                        outputBytes = Array.Empty<byte>();
                        error.AppendLine($"The output file \"{outputPath}\" was not found!");
                    }
                }

                return new CompileResult(code, exitCode, output.ToString(), error.ToString(), outputBytes);
            }
        }


        /*
         * FXC Tool
         */
        /// <summary>
        /// The FXC path.
        /// </summary>
        private static readonly Lazy<string> _fxcPath = new Lazy<string>(
            () => !Directory.Exists(WindowsKitsFolder)
                ? null
                : Directory.EnumerateFiles(
                        WindowsKitsFolder,
                        "fxc.exe",
                        SearchOption.AllDirectories)
                    // TODO This seems particularly broad brush, perhaps use Path.DirectorySeparatorChar+"arm"?
                    .OrderBy(f => f.Contains("arm") ? 1 : 0)
                    .FirstOrDefault(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        private static CompileResult FxcCompile(string code, Stage stage, string entryPoint)
        {
            using (TempFile inputFile = new TempFile())
            using (TempFile outputFile = new TempFile())
            {
                File.WriteAllText(inputFile, code);

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

                args.Append($" /E \"{entryPoint}\" /Fo \"{outputFile.FilePath}\" \"{inputFile.FilePath}\"");

                return Execute(_fxcPath.Value, args.ToString(), code, outputFile);
            }
        }

        private static GraphicsDevice CreateHeadlessD3D() =>
            GraphicsDevice.CreateD3D11(
                new GraphicsDeviceOptions(
                    true,
                    PixelFormat.R16_UNorm,
                    true,
                    ResourceBindingModel.Improved));

        /*
         * Open GL
         */
        private static readonly Lazy<string> _glslvPath = new Lazy<string>(
            () =>
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
                    using (Process p = Process.Start(psi))
                    {
                        p.StandardOutput.ReadToEndAsync();
                        p.StandardError.ReadToEndAsync();
                        p.WaitForExit(2000);
                    }

                    return "glslangvalidator";
                }
                catch
                {
                }

                // Check if the Vulkan SDK is installed, and use the compiler bundled there.
                string vulkanSdkPath = Environment.GetEnvironmentVariable(VulkanSdkEnvVar);
                if (vulkanSdkPath == null)
                {
                    return null;
                }

                string exeExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
                string exePath = Path.Combine(vulkanSdkPath, "bin", "glslangvalidator" + exeExtension);
                return File.Exists(exePath) ? exePath : null;
            },
            LazyThreadSafetyMode.ExecutionAndPublication);

        private static CompileResult GLCompile(string code, Stage stage, string entryPoint)
        {
            using (TempFile inputFile = new TempFile())
            {
                File.WriteAllText(inputFile, code);

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

                args.Append($" \"{inputFile.FilePath}\"");

                return Execute(_glslvPath.Value, args.ToString(), code);
            }
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

        /*
         * Vulkan
         */
        private static CompileResult VulkanCompile(string code, Stage stage, string entryPoint)
        {
            using (TempFile inputFile = new TempFile())
            using (TempFile outputFile = new TempFile())
            {
                File.WriteAllText(inputFile, code);

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
                args.Append($" -V -o \"{outputFile.FilePath}\" \"{inputFile.FilePath}\"");

                return Execute(_glslvPath.Value, args.ToString(), code, outputFile);
            }
        }

        private static GraphicsDevice CreateHeadlessVulkan() =>
            GraphicsDevice.CreateVulkan(
                new GraphicsDeviceOptions(
                    true,
                    PixelFormat.R16_UNorm,
                    true,
                    ResourceBindingModel.Improved));

        /*
         * Metal tool
         */
        private static readonly Lazy<string> _metalPath = new Lazy<string>(
            () => File.Exists(DefaultMetalPath) ? DefaultMetalPath : null,
            LazyThreadSafetyMode.ExecutionAndPublication);

        /*
         * Metallib tool
         */
        private static readonly Lazy<string> _metallibPath = new Lazy<string>(
            () => File.Exists(DefaultMetallibPath) ? DefaultMetallibPath : null,
            LazyThreadSafetyMode.ExecutionAndPublication);

        private static CompileResult MetalCompile(string code, Stage stage, string entryPoint)
        {
            using (TempFile inputFile = new TempFile())
            using (TempFile metalOutputFile = new TempFile())
            using (TempFile outputFile = new TempFile())
            {
                File.WriteAllText(inputFile, code, Encoding.UTF8);

                string metalArgs = $"-x metal -mmacosx-version-min=10.12 -o \"{metalOutputFile.FilePath}\" \"{inputFile.FilePath}\"";
                CompileResult bitcodeResult = Execute(_metalPath.Value, metalArgs, code, metalOutputFile, Encoding.UTF8);
                if (bitcodeResult.HasError)
                {
                    return bitcodeResult;
                }

                string metallibArgs = $"{metalOutputFile.FilePath} -o {outputFile.FilePath}";
                return Execute(_metallibPath.Value, metallibArgs, code, outputFile.FilePath);
            }
        }

        private static GraphicsDevice CreateHeadlessMetal() =>
            GraphicsDevice.CreateMetal(
                new GraphicsDeviceOptions(
                    true,
                    PixelFormat.R16_UNorm,
                    true,
                    ResourceBindingModel.Improved));
    }

    public class RequiredToolFeatureMissingException : Exception
    {
        public RequiredToolFeatureMissingException(string message) : base(message) { }
    }
}
