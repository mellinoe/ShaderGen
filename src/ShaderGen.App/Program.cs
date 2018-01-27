using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Metal;

namespace ShaderGen.App
{
    internal static class Program
    {
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
            if (lang.CompilationToolsAreAvailable()) {
                return lang.CompileCode(shaderPath, entryPoint, type, out path);
            }
            path = null;
            return false;
        }
    }
}