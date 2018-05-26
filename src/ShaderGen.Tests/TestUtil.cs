using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;
using System.Threading;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Tests.Tools;

namespace ShaderGen.Tests
{
    internal class TestUtil
    {
        private static readonly string ProjectBasePath = Path.Combine(AppContext.BaseDirectory, "TestAssets");

        public static Compilation GetCompilation()
            => GetCompilation(GetSyntaxTrees());
        public static Compilation GetCompilation(string code)
            => GetCompilation(CSharpSyntaxTree.ParseText(code));

        public static Compilation GetCompilation(params SyntaxTree[] syntaxTrees)
            => GetCompilation((IEnumerable<SyntaxTree>)syntaxTrees);

        public static Compilation GetCompilation(IEnumerable<SyntaxTree> syntaxTrees)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees,
                ProjectReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            return compilation;
        }


        public static SyntaxTree GetSyntaxTree(Compilation compilation, string name)
        {
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                if (Path.GetFileName(tree.FilePath) == name)
                {
                    return tree;
                }
            }

            throw new InvalidOperationException("Couldn't find a syntax tree with name " + name);
        }

        private static IEnumerable<SyntaxTree> GetSyntaxTrees()
        {
            foreach (string sourceItem in Directory.EnumerateFiles(ProjectBasePath, "*.cs", SearchOption.AllDirectories).ToArray())
            {
                using (FileStream fs = File.OpenRead(sourceItem))
                {
                    SourceText st = SourceText.From(fs);
                    yield return CSharpSyntaxTree.ParseText(st, path: sourceItem);
                }
            }
        }

        private static readonly Lazy<IReadOnlyList<string>> _projectReferencePaths
            = new Lazy<IReadOnlyList<string>>(
                () =>
                {
                    // Get all paths from References.txt
                    string[] paths = File.ReadAllLines(Path.Combine(ProjectBasePath, "References.txt"))
                        .Select(l => l.Trim())
                        .ToArray();


                    List<string> dirs = new List<string>
                    {
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget",
                            "packages")
                    };
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        dirs.Add(@"C:\Program Files\dotnet\sdk\NuGetFallbackFolder");
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        dirs.Add("/usr/local/share/dotnet/sdk/NuGetFallbackFolder");
                    else
                        dirs.Add("/usr/share/dotnet/sdk/NuGetFallbackFolder");

                    IReadOnlyCollection<string> packageDirs = dirs.Where(Directory.Exists).ToArray();

                    for (int index = 0; index < paths.Length; index++)
                    {
                        string path = paths[index];
                        bool found = false;
                        foreach (string packageDir in packageDirs)
                        {
                            string transformed = path.Replace("{nupkgdir}", packageDir);
                            transformed = transformed.Replace("{appcontextbasedirectory}", AppContext.BaseDirectory);
                            if (File.Exists(transformed))
                            {
                                found = true;
                                paths[index] = transformed;
                                break;
                            }
                        }

                        if (!found)
                            throw new InvalidOperationException($"Unable to find reference \"{path}\".");
                    }

                    return paths;
                },
                LazyThreadSafetyMode.ExecutionAndPublication);

        public static IReadOnlyList<string> ProjectReferencePaths => _projectReferencePaths.Value;

        private static readonly Lazy<IReadOnlyList<MetadataReference>> _projectReferences
            = new Lazy<IReadOnlyList<MetadataReference>>(
                () =>
                {
                    IReadOnlyList<string> paths = _projectReferencePaths.Value;
                    MetadataReference[] references = new MetadataReference[paths.Count];
                    for (int index = 0; index < paths.Count; index++)
                    {
                        string path = paths[index];
                        using (FileStream fs = File.OpenRead(path))
                            references[index] = MetadataReference.CreateFromStream(fs, filePath: path);
                    }

                    return references;
                },
                LazyThreadSafetyMode.ExecutionAndPublication);

        public static IReadOnlyList<MetadataReference> ProjectReferences => _projectReferences.Value;

        public static LanguageBackend[] GetAllBackends(Compilation compilation, ToolFeatures features = ToolFeatures.Transpilation)
            => ToolChain.Requires(features, false).Select(t => t.CreateBackend(compilation))
                .ToArray();
    }
}