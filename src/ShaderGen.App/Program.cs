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
    internal class Program
    {
        public static int Main(string[] args) {
            try {
                var arguments = new CommandLineArguments(args);
                var program = new Program(arguments);
                program.CompileFiles();
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
            return 0;
        }

        private readonly CommandLineArguments _arguments;
        private readonly Encoding _outputEncoding = new UTF8Encoding(false);
        private List<string> _generatedFilePaths;

        public Program(CommandLineArguments arguments) {
            _arguments = arguments;
        }

        public void CompileFiles()
        {
            AssertThatArgumentsAreValid();

            var referenceItems = File.ReadAllLines(_arguments.ReferenceItemsResponsePath);
            var compileItems = File.ReadAllLines(_arguments.CompileItemsResponsePath);

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

            var hlsl = new HlslBackend(compilation);
            var glsl450 = new Glsl450Backend(compilation);
            var metal = new MetalBackend(compilation);
            
            var languages = new LanguageBackend[]
            {
                hlsl,
                new Glsl330Backend(compilation),
                glsl450,
                metal,
            };

            var processors = new List<IShaderSetProcessor>();
            if (_arguments.ProcessorPath != null)
            {
                try
                {
                    var assm = Assembly.LoadFrom(_arguments.ProcessorPath);
                    var processorTypes = assm.GetTypes().Where(t => t.GetInterface(nameof(ShaderGen) + "." + nameof(IShaderSetProcessor)) != null);
                    foreach (var type in processorTypes)
                    {
                        var processor = (IShaderSetProcessor)Activator.CreateInstance(type);
                        processor.UserArgs = _arguments.ProcessorArgs;
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

            _generatedFilePaths = new List<string>();
            var binaryCompilers = new IBinaryCompiler[] {
                new Glsl450ToSpirVCompiler(glsl450), 
                new HlslBinaryCompiler(hlsl), 
                new MetalBinaryCompiler(metal)
            };
            foreach (var compiler in binaryCompilers) {
                var sets = shaderGenResult.GetOutput(compiler.Language);
                foreach (var set in sets)
                {
                    CompileCodeIntoBinaries(set.Name, set.VertexShaderCode, "vertex", compiler, set.VertexFunction.Name, ShaderFunctionType.VertexEntryPoint);
                    CompileCodeIntoBinaries(set.Name, set.FragmentShaderCode, "fragment", compiler, set.FragmentFunction.Name, ShaderFunctionType.FragmentEntryPoint);
                    CompileCodeIntoBinaries(set.Name, set.ComputeShaderCode, "compute", compiler, set.ComputeFunction.Name, ShaderFunctionType.ComputeEntryPoint);
                }
            }
            File.WriteAllLines(_arguments.GenListFilePath, _generatedFilePaths);
        }

        private void CompileCodeIntoBinaries(string name, 
                                             string code, 
                                             string type, 
                                             IBinaryCompiler lang, 
                                             string functionName,
                                             ShaderFunctionType functionType) {
            if (code == null) return;

            var extension = lang.GeneratedFileExtension;
            var outName = $"{name}-{type}.{extension}";
            var outPath = Path.Combine(_arguments.OutputPath, outName);
            File.WriteAllText(outPath, code, _outputEncoding);
            var succeeded = CompileBinaries(
                lang,
                outPath,
                functionName,
                functionType,
                out var genPath);
            
            if (succeeded || _arguments.ListAllFiles) {
                _generatedFilePaths.Add(genPath);
            }
        }

        private static bool CompileBinaries(IBinaryCompiler lang, string shaderPath, string entryPoint, ShaderFunctionType type, out string path)
        {
            if (lang.CompilationToolsAreAvailable()) {
                return lang.CompileCode(shaderPath, entryPoint, type, out path);
            }
            path = null;
            return false;
        }

        private void AssertThatArgumentsAreValid() {
            if (!File.Exists(_arguments.ReferenceItemsResponsePath)) {
                throw new InvalidArgumentException("Reference items response file does not exist: " + _arguments.ReferenceItemsResponsePath);
            }

            if (!File.Exists(_arguments.CompileItemsResponsePath)) {
                throw new InvalidArgumentException("Compile items response file does not exist: " + _arguments.CompileItemsResponsePath);
            }

            if (!Directory.Exists(_arguments.OutputPath)) {
                try {
                    Directory.CreateDirectory(_arguments.OutputPath);
                }
                catch {
                    throw new InvalidArgumentException($"Unable to create the output directory \"{_arguments.OutputPath}\".");
                }
            }
        }
    }
}