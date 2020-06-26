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
        private static string s_fxcPath;
        private static bool? s_fxcAvailable;
        private static bool? s_glslangValidatorAvailable;

        private static bool? s_metalMacOSToolsAvailable;
        private static string s_metalMacPath;
        private static string s_metallibMacPath;

        private static bool? s_metaliOSAvailable;
        private static string s_metaliOSPath;
        private static string s_metallibiOSPath;

        const string metalBinPath = @"/usr/bin/metal";
        const string metallibBinPath = @"/usr/bin/metallib";

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
            if (path == null)
            {
                return null;
            }
            else
            {
                return path.Trim();
            }
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
            else if (langType == typeof(Glsl450Backend) && IsGlslangValidatorAvailable())
            {
                bool result = CompileSpirv(shaderPath, entryPoint, type, out string path);
                paths = new[] { path };
                return result;
            }
            else if (langType == typeof(MetalBackend) && AreMetalMacOSToolsAvailable() && AreMetaliOSToolsAvailable())
            {
                bool macOSresult = CompileMetal(shaderPath, true, out string pathMacOS);
                bool iosResult = CompileMetal(shaderPath, false, out string pathiOS);
                paths = new[] { pathMacOS, pathiOS };
                return macOSresult && iosResult;
            }
            else
            {
                paths = Array.Empty<string>();
                return false;
            }
        }

        private static bool CompileHlsl(string shaderPath, string entryPoint, ShaderFunctionType type, out string path, bool debug)
        {
            return CompileHlslByDXC(shaderPath, entryPoint, type, out path, debug);
        }

        [Obsolete]
        private static bool CompileHlslByFXC(string shaderPath, string entryPoint, ShaderFunctionType type, out string path, bool debug)
        {
            try
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
                ProcessStartInfo psi = new ProcessStartInfo(fxcPath, args);
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                Process p = new Process() { StartInfo = psi };
                p.Start();
                var stdOut = p.StandardOutput.ReadToEndAsync();
                var stdErr = p.StandardError.ReadToEndAsync();
                bool exited = p.WaitForExit(60000);

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

        private static bool CompileHlslByDXC(string shaderPath, string entryPoint, ShaderFunctionType type, out string path, bool debug)
        {
            // https://github.com/microsoft/DirectXShaderCompiler
            // https://translate.google.co.jp/translate?hl=ja&sl=ja&tl=en&u=https%3A%2F%2Fmonobook.org%2Fwiki%2FDirectX_Shader_Compiler
            var cmd = "dxc";

            var optimize = debug ? "-Od -Zi" : "-O3";

            //var profile = type switch
            //{
            //    ShaderFunctionType.VertexEntryPoint   => "vs_6_0",
            //    ShaderFunctionType.FragmentEntryPoint => "ps_6_0",
            //    _ => "cs_6_0",
            //};
            var profile = "";
            switch (type)
            {
                case ShaderFunctionType.VertexEntryPoint  : profile = "vs_6_0";break;
                case ShaderFunctionType.FragmentEntryPoint: profile = "ps_6_0";break;
                case ShaderFunctionType.ComputeEntryPoint : profile = "cs_6_0";break;
                default: throw new NotSupportedException();
            }

            var outputPath = shaderPath + ".dxil";

            var args = $"{optimize} -T {profile} -E {entryPoint} {shaderPath} -Fo {outputPath} ";

            try
            {
                var psi = new ProcessStartInfo(cmd, args);
                psi.RedirectStandardError  = true;
                psi.RedirectStandardOutput = true;
                var p = Process.Start(psi);
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
                Console.WriteLine($"Unable to launch {cmd} tool.");
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

        private static bool CompileMetal(string shaderPath, bool mac, out string path)
        {
            string metalPath = mac ? s_metalMacPath : s_metaliOSPath;
            string metallibPath = mac ? s_metallibMacPath : s_metallibiOSPath;

            string shaderPathWithoutExtension = Path.ChangeExtension(shaderPath, null);
            string extension = mac ? ".metallib" : ".ios.metallib";
            string outputPath = shaderPathWithoutExtension + extension;
            string bitcodePath = Path.GetTempFileName();
            string metalArgs = $"-c -o {bitcodePath} {shaderPath}";
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

        [Obsolete]
        public static bool IsFxcAvailable()
        {
            if (!s_fxcAvailable.HasValue)
            {
                s_fxcPath = FindFxcExe();
                s_fxcAvailable = s_fxcPath != null;
            }

            return s_fxcAvailable.Value;
        }

        public static bool IsGlslangValidatorAvailable()
        {
            if (!s_glslangValidatorAvailable.HasValue)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("glslangValidator");
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    Process.Start(psi);
                    s_glslangValidatorAvailable = true;
                }
                catch { s_glslangValidatorAvailable = false; }
            }

            return s_glslangValidatorAvailable.Value;
        }

        public static bool AreMetalMacOSToolsAvailable()
        {
            if (!s_metalMacOSToolsAvailable.HasValue)
            {
                s_metalMacPath = FindXcodeTool("macosx", "metal");
                s_metallibMacPath = FindXcodeTool("macosx", "metallib");

                s_metalMacOSToolsAvailable = s_metalMacPath != null && s_metallibMacPath != null;
            }

            return s_metalMacOSToolsAvailable.Value;
        }

        public static bool AreMetaliOSToolsAvailable()
        {
            if (!s_metaliOSAvailable.HasValue)
            {
                s_metaliOSPath = FindXcodeTool("iphoneos", "metal");
                s_metallibiOSPath = FindXcodeTool("iphoneos", "metallib");

                s_metaliOSAvailable = s_metalMacPath != null && s_metallibMacPath != null;
            }

            return s_metaliOSAvailable.Value;
        }

        private static string FindXcodeTool(string sdk, string tool)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "xcrun",
                Arguments = $"-sdk {sdk} --find {tool}",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                using (Process process = Process.Start(startInfo))
                using (StreamReader reader = process.StandardOutput)
                {
                    return reader.ReadLine();
                }
            }
            catch
            {
            }

            return null;
        }

        private static string BackendExtension(LanguageBackend lang)
        {
            if (lang.GetType() == typeof(HlslBackend))
            {
                return "hlsl";
            }
            else if (lang.GetType() == typeof(Glsl330Backend))
            {
                return "330.glsl";
            }
            else if (lang.GetType() == typeof(GlslEs300Backend))
            {
                return "300.glsles";
            }
            else if (lang.GetType() == typeof(Glsl450Backend))
            {
                return "450.glsl";
            }
            else if (lang.GetType() == typeof(MetalBackend))
            {
                return "metal";
            }

            throw new InvalidOperationException("Invalid backend type: " + lang.GetType().Name);
        }

        [Obsolete]
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

        private static string GetXcodePlatformPath(string sdk)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "xcrun",
                    Arguments = $"-sdk {sdk} --show-sdk-platform-path",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                try
                {
                    using (Process process = Process.Start(startInfo))
                    using (StreamReader reader = process.StandardOutput)
                    {
                        return reader.ReadLine();
                    }
                }
                catch
                {
                }
            }
            return null;
        }
    }
}
