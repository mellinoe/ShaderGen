using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Reflection;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Metal;

namespace ShaderGen.App
{
    internal static class Program
    {
        private static string _fxcPath;
        private static bool? _fxcAvailable;
        private static bool? _glslangValidatorAvailable;
        private static bool? s_metalToolsAvailable;

        const string metalPath = @"/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/usr/bin/metal";
        const string metallibPath = @"/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/usr/bin/metallib";

        public static int Main(string[] args) {
            try {
                var arguments = new CommandLineArguments(args);
                CompileFiles(arguments);
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
            return 0;
        }
        
        public static void CompileFiles(CommandLineArguments arguments)
        {
            AssertThatArgumentsAreValid(arguments);

            var referenceItems = File.ReadAllLines(arguments.ReferenceItemsResponsePath);
            var compileItems = File.ReadAllLines(arguments.CompileItemsResponsePath);

            var references = new List<MetadataReference>();
            foreach (var referencePath in referenceItems)
            {
                if (!File.Exists(referencePath))
                {
                    throw new FileNotFoundException("Error: reference does not exist: " + referencePath);
                }

                using (var fs = File.OpenRead(referencePath))
                {
                    references.Add(MetadataReference.CreateFromStream(fs, filePath: referencePath));
                }
            }

            var syntaxTrees = new List<SyntaxTree>();
            foreach (var sourcePath in compileItems)
            {
                var fullSourcePath = Path.Combine(Environment.CurrentDirectory, sourcePath);
                if (!File.Exists(fullSourcePath))
                {
                    throw new FileNotFoundException("Error: source file does not exist: " + fullSourcePath);
                }

                using (var fs = File.OpenRead(fullSourcePath))
                {
                    var text = SourceText.From(fs);
                    syntaxTrees.Add(CSharpSyntaxTree.ParseText(text, path: fullSourcePath));
                }
            }

            Compilation compilation = CSharpCompilation.Create(
                "ShaderGen.App.GenerateShaders",
                syntaxTrees,
                references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var languages = new ILanguageBackend[]
            {
                new HlslBackend(compilation),
                new Glsl330Backend(compilation),
                new Glsl450Backend(compilation),
                new MetalBackend(compilation),
            };

            var processors = new List<IShaderSetProcessor>();
            if (arguments.ProcessorPath != null)
            {
                try
                {
                    var assm = Assembly.LoadFrom(arguments.ProcessorPath);
                    var processorTypes = assm.GetTypes().Where(t => t.GetInterface(nameof(ShaderGen) + "." + nameof(IShaderSetProcessor)) != null);
                    foreach (var type in processorTypes)
                    {
                        var processor = (IShaderSetProcessor)Activator.CreateInstance(type);
                        processor.UserArgs = arguments.ProcessorArgs;
                        processors.Add(processor);
                    }
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    var msg = string.Join(Environment.NewLine, rtle.LoaderExceptions.Select(e => e.ToString()));
                    throw new Exception(msg);
                }
            }

            var sg = new ShaderGenerator(compilation, languages, processors.ToArray());
            ShaderGenerationResult shaderGenResult;
            try
            {
                shaderGenResult = sg.GenerateShaders();
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                throw new Exception("An error was encountered while generating shader code:" + ex, ex);
            }

            Encoding outputEncoding = new UTF8Encoding(false);
            var generatedFilePaths = new List<string>();
            foreach (var lang in languages)
            {
                var extension = lang.GeneratedFileExtension;
                var sets = shaderGenResult.GetOutput(lang);
                foreach (var set in sets)
                {
                    var name = set.Name;
                    if (set.VertexShaderCode != null)
                    {
                        var vsOutName = name + "-vertex." + extension;
                        var vsOutPath = Path.Combine(arguments.OutputPath, vsOutName);
                        File.WriteAllText(vsOutPath, set.VertexShaderCode, outputEncoding);
                        var succeeded = CompileCode(
                            lang,
                            vsOutPath,
                            set.VertexFunction.Name,
                            ShaderFunctionType.VertexEntryPoint,
                            out var genPath);
                        if (succeeded)
                        {
                            generatedFilePaths.Add(genPath);
                        }
                        if (!succeeded || arguments.ListAllFiles)
                        {
                            generatedFilePaths.Add(vsOutPath);
                        }
                    }
                    if (set.FragmentShaderCode != null)
                    {
                        var fsOutName = name + "-fragment." + extension;
                        var fsOutPath = Path.Combine(arguments.OutputPath, fsOutName);
                        File.WriteAllText(fsOutPath, set.FragmentShaderCode, outputEncoding);
                        var succeeded = CompileCode(
                            lang,
                            fsOutPath,
                            set.FragmentFunction.Name,
                            ShaderFunctionType.FragmentEntryPoint,
                            out var genPath);
                        if (succeeded)
                        {
                            generatedFilePaths.Add(genPath);
                        }
                        if (!succeeded || arguments.ListAllFiles)
                        {
                            generatedFilePaths.Add(fsOutPath);
                        }
                    }
                    if (set.ComputeShaderCode != null)
                    {
                        var csOutName = name + "-compute." + extension;
                        var csOutPath = Path.Combine(arguments.OutputPath, csOutName);
                        File.WriteAllText(csOutPath, set.ComputeShaderCode, outputEncoding);
                        var succeeded = CompileCode(
                            lang,
                            csOutPath,
                            set.ComputeFunction.Name,
                            ShaderFunctionType.ComputeEntryPoint,
                            out var genPath);
                        if (succeeded)
                        {
                            generatedFilePaths.Add(genPath);
                        }
                        if (!succeeded || arguments.ListAllFiles)
                        {
                            generatedFilePaths.Add(csOutPath);
                        }
                    }
                }
            }

            File.WriteAllLines(arguments.GenListFilePath, generatedFilePaths);
        }

        private static void AssertThatArgumentsAreValid(CommandLineArguments arguments) {
            if (!File.Exists(arguments.ReferenceItemsResponsePath)) {
                throw new InvalidArgumentException("Reference items response file does not exist: " + arguments.ReferenceItemsResponsePath);
            }

            if (!File.Exists(arguments.CompileItemsResponsePath)) {
                throw new InvalidArgumentException("Compile items response file does not exist: " + arguments.CompileItemsResponsePath);
            }

            if (!Directory.Exists(arguments.OutputPath)) {
                try {
                    Directory.CreateDirectory(arguments.OutputPath);
                }
                catch {
                    throw new InvalidArgumentException($"Unable to create the output directory \"{arguments.OutputPath}\".");
                }
            }
        }

        private static bool CompileCode(ILanguageBackend lang, string shaderPath, string entryPoint, ShaderFunctionType type, out string path)
        {
            var langType = lang.GetType();
            if (langType == typeof(HlslBackend) && IsFxcAvailable())
            {
                return CompileHlsl(shaderPath, entryPoint, type, out path);
            }
            else if (langType == typeof(Glsl450Backend) && IsGlslangValidatorAvailable())
            {
                return CompileSpirv(shaderPath, entryPoint, type, out path);
            }
            else if (langType == typeof(MetalBackend) && AreMetalToolsAvailable())
            {
                return CompileMetal(shaderPath, out path);
            }
            else
            {
                path = null;
                return false;
            }
        }

        private static bool CompileHlsl(string shaderPath, string entryPoint, ShaderFunctionType type, out string path)
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

        private static bool CompileSpirv(string shaderPath, string entryPoint, ShaderFunctionType type, out string path)
        {
            string stage = type == ShaderFunctionType.VertexEntryPoint ? "vert"
                : type == ShaderFunctionType.FragmentEntryPoint ? "frag"
                : "comp";
            string outputPath = shaderPath + ".spv";
            string args = $"-V -S {stage} {shaderPath} -o {outputPath}";
            try
            {

                ProcessStartInfo psi = new ProcessStartInfo("glslangValidator", args);
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                Process p = Process.Start(psi);
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    path = outputPath;
                    return true;
                }
                else
                {
                    throw new ShaderGenerationException(p.StandardOutput.ReadToEnd());
                }
            }
            catch (Win32Exception)
            {
                Console.WriteLine("Unable to launch glslangValidator tool.");
            }

            path = null;
            return false;
        }

        private static bool CompileMetal(string shaderPath, out string path)
        {
            string shaderPathWithoutExtension = Path.ChangeExtension(shaderPath, null);
            string outputPath = shaderPathWithoutExtension + ".metallib";
            string bitcodePath = Path.GetTempFileName();
            string metalArgs = $"-x metal -o {bitcodePath} {shaderPath}";
            try
            {
                ProcessStartInfo metalPSI = new ProcessStartInfo(metalPath, metalArgs);
                metalPSI.RedirectStandardError = true;
                metalPSI.RedirectStandardOutput = true;
                Process metalProcess = Process.Start(metalPSI);
                metalProcess.WaitForExit();

                if (metalProcess.ExitCode != 0)
                {
                    throw new ShaderGenerationException(metalProcess.StandardError.ReadToEnd());
                }

                string metallibArgs = $"-o {outputPath} {bitcodePath}";
                ProcessStartInfo metallibPSI = new ProcessStartInfo(metallibPath, metallibArgs);
                metallibPSI.RedirectStandardError = true;
                metallibPSI.RedirectStandardOutput = true;
                Process metallibProcess = Process.Start(metallibPSI);
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

        public static bool IsFxcAvailable()
        {
            if (!_fxcAvailable.HasValue)
            {
                _fxcPath = FindFxcExe();
                _fxcAvailable = _fxcPath != null;
            }

            return _fxcAvailable.Value;
        }

        public static bool IsGlslangValidatorAvailable()
        {
            if (!_glslangValidatorAvailable.HasValue)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("glslangValidator");
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    Process.Start(psi);
                    _glslangValidatorAvailable = true;
                }
                catch { _glslangValidatorAvailable = false; }
            }

            return _glslangValidatorAvailable.Value;
        }

        public static bool AreMetalToolsAvailable()
        {
            if (!s_metalToolsAvailable.HasValue)
            {
                s_metalToolsAvailable = File.Exists(metalPath) && File.Exists(metallibPath);
            }

            return s_metalToolsAvailable.Value;
        }

        private static string FindFxcExe()
        {
            const string WindowsKitsFolder = @"C:\Program Files (x86)\Windows Kits";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Directory.Exists(WindowsKitsFolder))
            {
                IEnumerable<string> paths = Directory.EnumerateFiles(
                    WindowsKitsFolder,
                    "fxc.exe",
                    SearchOption.AllDirectories);
                string path = paths.FirstOrDefault(s => !s.Contains("arm"));
                return path;
            }

            return null;
        }
    }
}