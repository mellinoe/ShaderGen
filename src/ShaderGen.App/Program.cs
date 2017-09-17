using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ShaderGen.App
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            string referenceItemsResponsePath = null;
            string compileItemsResponsePath = null;
            string outputPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].Replace("\\\\", "\\");
            }

            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.DefineOption("ref", ref referenceItemsResponsePath, true, "The semicolon-separated list of references to compile against.");
                syntax.DefineOption("src", ref compileItemsResponsePath, true, "The semicolon-separated list of source files to compile.");
                syntax.DefineOption("out", ref outputPath, true, "The output path for the generated shaders.");
            });

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
                    Console.Error.WriteLine("Unable to create the output directory.");
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
            Glsl450Backend glsl450 = new Glsl450Backend(compilation);
            LanguageBackend[] languages = new LanguageBackend[]
            {
                hlsl,
                glsl330,
                glsl450
            };

            ShaderGenerator sg = new ShaderGenerator(compilation, languages);
            ShaderGenerationResult shaderGenResult;
            try
            {
                shaderGenResult = sg.GenerateShaders();
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("An error was encountered while generating shader code:");
                sb.AppendLine(e.ToString());
                Console.Error.WriteLine(sb.ToString());
                return -1;
            }

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
                        File.WriteAllText(vsOutPath, set.VertexShaderCode);
                        CompileCode(lang, vsOutPath, set.VertexFunction.Name, true);
                    }
                    if (set.FragmentShaderCode != null)
                    {
                        string fsOutName = name + "-fragment." + extension;
                        string fsOutPath = Path.Combine(outputPath, fsOutName);
                        File.WriteAllText(fsOutPath, set.FragmentShaderCode);
                        CompileCode(lang, fsOutPath, set.FragmentFunction.Name, false);
                    }
                }
            }

            return 0;
        }

        private static void CompileCode(LanguageBackend lang, string shaderPath, string entryPoint, bool isVertex)
        {
            Type langType = lang.GetType();
            if (langType == typeof(HlslBackend))
            {
                CompileHlsl(shaderPath, entryPoint, isVertex);
            }
            else if (langType == typeof(Glsl450Backend))
            {
                CompileSpirv(shaderPath, entryPoint, isVertex);
            }
        }

        private static void CompileHlsl(string shaderPath, string entryPoint, bool isVertex)
        {
            string outputPath = shaderPath + ".bytes";
            string args = $"/T {(isVertex ? "vs_5_0" : "ps_5_0")} /E {entryPoint} {shaderPath} /Fo {outputPath}";
            try
            {
                Process.Start("fxc", args).WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to compile HLSL bytecode: " + e);
            }
        }

        private static void CompileSpirv(string shaderPath, string entryPoint, bool isVertex)
        {
            string outputPath = shaderPath + ".spv";
            string args = $"-V -S {(isVertex ? "vert" : "frag")} {shaderPath} -o {outputPath}";
            try
            {
                Process.Start("glslangvalidator", args).WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to compile SPIR-V bytecode: " + e);
            }
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
            else if (lang.GetType() == typeof(Glsl450Backend))
            {
                return "450.glsl";
            }

            throw new InvalidOperationException("Invalid backend type: " + lang.GetType().Name);
        }
    }
}
