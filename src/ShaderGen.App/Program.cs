using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading.Tasks;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Metal;
using SharpDX.D3DCompiler;

namespace ShaderGen.App
{
    internal static class Program
    {
        private const string s_windowsKitsFolder = @"C:\Program Files (x86)\Windows Kits";

        public static int Main(string[] args)
        {
            string referenceItemsResponsePath = null;
            string compileItemsResponsePath = null;
            string outputPath = null;
            string genListFilePath = null;
            bool listAllFiles = false;
            string processorPath = null;
            string processorArgs = null;
            bool debug = false;

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].Replace("\\\\", "\\");
            }

            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.DefineOption("ref", ref referenceItemsResponsePath, true, "The semicolon-separated list of references to compile against.");
                syntax.DefineOption("src", ref compileItemsResponsePath, true, "The semicolon-separated list of source files to compile.");
                syntax.DefineOption("out", ref outputPath, true, "The output path for the generated shaders.");
                syntax.DefineOption("genlist", ref genListFilePath, true, "The output file to store the list of generated files.");
                syntax.DefineOption("listall", ref listAllFiles, false, "Forces all generated files to be listed in the list file. By default, only bytecode files will be listed and not the original shader code.");
                syntax.DefineOption("processor", ref processorPath, false, "The path of an assembly containing IShaderSetProcessor types to be used to post-process GeneratedShaderSet objects.");
                syntax.DefineOption("processorargs", ref processorArgs, false, "Custom information passed to IShaderSetProcessor.");
                syntax.DefineOption("debug", ref debug, false, "Compiles the shader with debug information when supported.");
            });

            referenceItemsResponsePath = NormalizePath(referenceItemsResponsePath);
            compileItemsResponsePath = NormalizePath(compileItemsResponsePath);
            outputPath = NormalizePath(outputPath);
            genListFilePath = NormalizePath(genListFilePath);
            processorPath = NormalizePath(processorPath);

            if (!File.Exists(referenceItemsResponsePath))
            {
                Console.Error.WriteLine("Reference items response file does not exist: " + referenceItemsResponsePath);
                return -1;
            }
            if (!File.Exists(compileItemsResponsePath))
            {
                Console.Error.WriteLine("Compile items response file does not exist: " + compileItemsResponsePath);
                return -1;
            }
            if (!Directory.Exists(outputPath))
            {
                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch
                {
                    Console.Error.WriteLine($"Unable to create the output directory \"{outputPath}\".");
                    return -1;
                }
            }

            string[] referenceItems = File.ReadAllLines(referenceItemsResponsePath);
            string[] compileItems = File.ReadAllLines(compileItemsResponsePath);

            List<MetadataReference> references = new List<MetadataReference>();
            foreach (string referencePath in referenceItems)
            {
                if (!File.Exists(referencePath))
                {
                    Console.Error.WriteLine("Error: reference does not exist: " + referencePath);
                    return 1;
                }

                using (FileStream fs = File.OpenRead(referencePath))
                {
                    references.Add(MetadataReference.CreateFromStream(fs, filePath: referencePath));
                }
            }

            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (string sourcePath in compileItems)
            {
                string fullSourcePath = Path.Combine(Environment.CurrentDirectory, sourcePath);
                if (!File.Exists(fullSourcePath))
                {
                    Console.Error.WriteLine("Error: source file does not exist: " + fullSourcePath);
                    return 1;
                }

                using (FileStream fs = File.OpenRead(fullSourcePath))
                {
                    SourceText text = SourceText.From(fs);
                    syntaxTrees.Add(CSharpSyntaxTree.ParseText(text, path: fullSourcePath));
                }
            }

            Compilation compilation = CSharpCompilation.Create(
                "ShaderGen.App.GenerateShaders",
                syntaxTrees,
                references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            HlslBackend hlsl = new HlslBackend(compilation);
            Glsl330Backend glsl330 = new Glsl330Backend(compilation);
            GlslEs300Backend glsles300 = new GlslEs300Backend(compilation);
            Glsl450Backend glsl450 = new Glsl450Backend(compilation);
            MetalBackend metal = new MetalBackend(compilation);
            LanguageBackend[] languages = new LanguageBackend[]
            {
                hlsl,
                glsl330,
                glsles300,
                glsl450,
                metal,
            };

            List<IShaderSetProcessor> processors = new List<IShaderSetProcessor>();
            if (processorPath != null)
            {
                try
                {
                    Assembly assm = Assembly.LoadFrom(processorPath);
                    IEnumerable<Type> processorTypes = assm.GetTypes().Where(
                        t => t.GetInterface(nameof(ShaderGen) + "." + nameof(IShaderSetProcessor)) != null);
                    foreach (Type type in processorTypes)
                    {
                        IShaderSetProcessor processor = (IShaderSetProcessor)Activator.CreateInstance(type);
                        processor.UserArgs = processorArgs;
                        processors.Add(processor);
                    }
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    string msg = string.Join(Environment.NewLine, rtle.LoaderExceptions.Select(e => e.ToString()));
                    Console.WriteLine("FAIL: " + msg);
                    throw new Exception(msg);
                }
            }

            ShaderGenerator sg = new ShaderGenerator(compilation, languages, processors.ToArray());
            ShaderGenerationResult shaderGenResult;
            try
            {
                shaderGenResult = sg.GenerateShaders();
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("An error was encountered while generating shader code:");
                sb.AppendLine(e.ToString());
                Console.Error.WriteLine(sb.ToString());
                return -1;
            }

            Encoding outputEncoding = new UTF8Encoding(false);
            List<string> generatedFilePaths = new List<string>();
            foreach (LanguageBackend lang in languages)
            {
                string extension = BackendExtension(lang);
                IReadOnlyList<GeneratedShaderSet> sets = shaderGenResult.GetOutput(lang);
                foreach (GeneratedShaderSet set in sets)
                {
                    string name = set.Name;
                    if (set.VertexShaderCode != null)
                    {
                        string vsOutName = name + "-vertex." + extension;
                        string vsOutPath = Path.Combine(outputPath, vsOutName);
                        File.WriteAllText(vsOutPath, set.VertexShaderCode, outputEncoding);
                        bool succeeded = CompileCode(
                            lang,
                            vsOutPath,
                            set.VertexFunction.Name,
                            ShaderFunctionType.VertexEntryPoint,
                            out string[] genPaths,
                            debug);
                        if (succeeded)
                        {
                            generatedFilePaths.AddRange(genPaths);
                        }
                        if (!succeeded || listAllFiles)
                        {
                            generatedFilePaths.Add(vsOutPath);
                        }
                    }
                    if (set.FragmentShaderCode != null)
                    {
                        string fsOutName = name + "-fragment." + extension;
                        string fsOutPath = Path.Combine(outputPath, fsOutName);
                        File.WriteAllText(fsOutPath, set.FragmentShaderCode, outputEncoding);
                        bool succeeded = CompileCode(
                            lang,
                            fsOutPath,
                            set.FragmentFunction.Name,
                            ShaderFunctionType.FragmentEntryPoint,
                            out string[] genPaths,
                            debug);
                        if (succeeded)
                        {
                            generatedFilePaths.AddRange(genPaths);
                        }
                        if (!succeeded || listAllFiles)
                        {
                            generatedFilePaths.Add(fsOutPath);
                        }
                    }
                    if (set.ComputeShaderCode != null)
                    {
                        string csOutName = name + "-compute." + extension;
                        string csOutPath = Path.Combine(outputPath, csOutName);
                        File.WriteAllText(csOutPath, set.ComputeShaderCode, outputEncoding);
                        bool succeeded = CompileCode(
                            lang,
                            csOutPath,
                            set.ComputeFunction.Name,
                            ShaderFunctionType.ComputeEntryPoint,
                            out string[] genPaths,
                            debug);
                        if (succeeded)
                        {
                            generatedFilePaths.AddRange(genPaths);
                        }
                        if (!succeeded || listAllFiles)
                        {
                            generatedFilePaths.Add(csOutPath);
                        }
                    }
                }
            }

            File.WriteAllLines(genListFilePath, generatedFilePaths);

            return 0;
        }

        private static string NormalizePath(string path)
        {
            // TODO Should this use Path.GetFullPath()?
            return path?.Trim();
        }

        private static bool CompileCode(LanguageBackend lang, string shaderPath, string entryPoint, ShaderFunctionType type, out string[] paths, bool debug)
        {
            Type langType = lang.GetType();
            if (langType == typeof(HlslBackend))
            {
                bool result = CompileHlsl(shaderPath, entryPoint, type, out string path, debug);
                paths = new[] { path };
                return result;
            }

            if (langType == typeof(Glsl450Backend))
            {
                bool result = CompileSpirv(shaderPath, entryPoint, type, out string path);
                paths = new[] { path };
                return result;
            }

            if (langType == typeof(MetalBackend))
            {
                bool macOSresult = CompileMetal(shaderPath, true, out string pathMacOS);
                bool iosResult = CompileMetal(shaderPath, false, out string pathiOS);
                paths = new[] { pathMacOS, pathiOS };
                return macOSresult && iosResult;
            }

            // No compilation required
            paths = new[] {shaderPath};
            return true;
        }

        private static bool CompileHlsl(string shaderPath, string entryPoint, ShaderFunctionType type, out string path, bool debug)
        {
            // Try SharpDX, and fall back to FXC if available.
            return CompileHlslBySharpDX(shaderPath, entryPoint, type, out path, debug) ||
                   CompileHlslByFXC(shaderPath, entryPoint, type, out path, debug);
        }

        private static bool CompileHlslByFXC(string shaderPath, string entryPoint, ShaderFunctionType type, out string path, bool debug)
        {
            string profile = type == ShaderFunctionType.VertexEntryPoint ? "vs_5_0"
                : type == ShaderFunctionType.FragmentEntryPoint ? "ps_5_0"
                : "cs_5_0";
            string outputPath = shaderPath + ".bytes";
            string args = $"/T \"{profile}\" /E \"{entryPoint}\" \"{shaderPath}\" /Fo \"{outputPath}\"";
            if (debug)
            {
                args += " /Od /Zi";
            }
            else
            {
                args += " /O3";
            }

            string fxcPath = FindFxcExe();
            if (fxcPath != null)
            {
                string result = RunProcess(fxcPath, args);
                if (result == null)
                {
                    path = outputPath;
                    return true;
                }
                Console.Error.WriteLine($"Failed to compile HLSL shader using \"{fxcPath}\" {args}: {result}");
            }

            path = null;
            return false;
        }

        private static bool CompileHlslBySharpDX(string shaderPath, string entryPoint, ShaderFunctionType type, out string path, bool debug)
        {
            try
            {
                string profile = type == ShaderFunctionType.VertexEntryPoint ? "vs_5_0"
                    : type == ShaderFunctionType.FragmentEntryPoint ? "ps_5_0"
                    : "cs_5_0";
                string outputPath = shaderPath + ".bytes";

                ShaderFlags shaderFlags = debug
                    ? ShaderFlags.SkipOptimization | ShaderFlags.Debug
                    : ShaderFlags.OptimizationLevel3;
                CompilationResult compilationResult = ShaderBytecode.CompileFromFile(
                    shaderPath,
                    entryPoint,
                    profile,
                    shaderFlags,
                    EffectFlags.None);

                if (compilationResult.Bytecode != null &&
                    !compilationResult.HasErrors &&
                    compilationResult.ResultCode == 0)
                {
                    using (FileStream fileStream = File.OpenWrite(outputPath))
                    {
                        compilationResult.Bytecode.Save(fileStream);
                        path = outputPath;
                        return true;
                    }
                }

                Console.Error.WriteLine($"Failed to compile HLSL shader using SharpDX: {compilationResult.Message}.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error when compiling HLSL using SharpDX: {e.Message}");
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
            string result = RunProcess("glslangValidator", args);
            if (result == null)
            {
                path = outputPath;
                return true;
            }

            Console.Error.WriteLine($"Failed to compile OpenGL shader using glslangValidator {args}: {result}");
            path = null;
            return false;
        }

        private static bool CompileMetal(string shaderPath, bool mac, out string path)
        {
            string metalPath = FindXcodeTool(mac, "metal");
            string metallibPath = FindXcodeTool(mac, "metal");
            if (metalPath == null || metallibPath == null)
            {
                path = null;
                return false;
            }

            string shaderPathWithoutExtension = Path.ChangeExtension(shaderPath, null);
            string extension = mac ? ".metallib" : ".ios.metallib";
            string outputPath = shaderPathWithoutExtension + extension;
            string bitcodePath = Path.GetTempFileName();
            string metalArgs = $"-c -o {bitcodePath} {shaderPath}";
            
            string result = RunProcess(metalPath, metalArgs);
            if (result == null)
            {
                string metallibArgs = $"-o {outputPath} {bitcodePath}";
                result = RunProcess(metallibPath, metallibArgs);

                try
                {
                    File.Delete(bitcodePath);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Failed to delete Metal bitcode at \"{bitcodePath}\": {exception.Message}");
                }

                if (result == null)
                {
                    path = outputPath;
                    return true;
                }

                Console.Error.WriteLine($"Failed to compile Metal bitcode using \"{metallibPath}\" {metallibArgs}: {result}");
            }
            else
            {
                Console.Error.WriteLine($"Failed to compile Metal shader using \"{metalPath}\" {metalArgs}: {result}");
            }

            path = null;
            return false;
        }

        private static string FindXcodeTool(bool mac, string tool)
        {
            string sdk = mac ? "macosx" : "iphoneos";
            if (!RunProcess("xcrun", $"-sdk {sdk} --find {tool}", out string stdOut, out string stdErr, out int exitCode))
            {
                Console.WriteLine($"The {sdk} {tool} tool was not found! Exit code: {exitCode}, StdOut: {stdOut}, StdErr: {stdErr}");
                return null;
            }
            // Return first line (if any)
            return new StringReader(stdOut).ReadLine();
        }

        private static string FindFxcExe()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Directory.Exists(s_windowsKitsFolder))
            {
                IEnumerable<string> paths = Directory.EnumerateFiles(
                    s_windowsKitsFolder,
                    "fxc.exe",
                    SearchOption.AllDirectories);
                string path = paths.FirstOrDefault(s => !s.Contains("arm"));
                return path;
            }

            Console.WriteLine($"The FXC.Exe tool was not found below {s_windowsKitsFolder}!");
            return null;
        }

        [Obsolete]
        private static string GetXcodePlatformPath(bool mac)
        {
            string sdk = mac ? "macosx" : "iphoneos";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (!RunProcess("xcrun", $"-sdk {sdk} --show-sdk-platform-path", out string stdOut, out string stdErr, out int exitCode))
                {
                    Console.WriteLine($"The {sdk} platform path was not found! Exit code: {exitCode}, StdOut: {stdOut}, StdErr: {stdErr}");
                    return null;
                }
                // Return first line (if any)
                return new StringReader(stdOut).ReadLine();
            }
            return null;
        }

        private static string RunProcess(string filename, string arguments, int timeout = 60000)
        {
            return RunProcess(filename, arguments, out string stdOut, out string stdErr, out int exitCode, timeout)
                ? null
                : $"Exit code: {exitCode}, StdOut: {stdOut}, StdErr: {stdErr}";
        }

        private static bool RunProcess(string filename, string arguments, out string stdOut, out string stdErr, out int exitCode, int timeout = 60000)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(filename, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                Process p = new Process { StartInfo = psi };
                p.Start();
                Task<string> stdOutTask = p.StandardOutput.ReadToEndAsync();
                Task<string> stdErrTask = p.StandardError.ReadToEndAsync();
                bool exited = p.WaitForExit(timeout);

                stdOut = stdOutTask.IsCompleted ? stdOutTask.Result : "[Timed out]";
                stdErr = stdErrTask.IsCompleted ? stdErrTask.Result : "[Timed out]";
                exitCode = exited ? p.ExitCode : -1;
                if (exitCode == 0)
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                stdOut = "[Exception thrown]";
                stdErr = exception.Message;
                exitCode = -1;
            }

            return false;
        }

        private static string BackendExtension(LanguageBackend lang)
        {
            if (lang.GetType() == typeof(HlslBackend))
            {
                return "hlsl";
            }
            if (lang.GetType() == typeof(Glsl330Backend))
            {
                return "330.glsl";
            }
            if (lang.GetType() == typeof(GlslEs300Backend))
            {
                return "300.glsles";
            }
            if (lang.GetType() == typeof(Glsl450Backend))
            {
                return "450.glsl";
            }
            if (lang.GetType() == typeof(MetalBackend))
            {
                return "metal";
            }

            throw new InvalidOperationException("Invalid backend type: " + lang.GetType().Name);
        }
    }
}
