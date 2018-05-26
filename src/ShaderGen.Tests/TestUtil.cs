using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;
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
            => GetCompilation((IEnumerable<SyntaxTree>) syntaxTrees);

        public static Compilation GetCompilation(IEnumerable<SyntaxTree> syntaxTrees)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees,
                GetProjectReferences(),
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
            foreach (string sourceItem in GetCompileItems())
            {
                using (FileStream fs = File.OpenRead(sourceItem))
                {
                    SourceText st = SourceText.From(fs);
                    yield return CSharpSyntaxTree.ParseText(st, path: sourceItem);
                }
            }
        }

        private static IEnumerable<MetadataReference> GetProjectReferences()
        {
            string[] referenceItems = GetReferenceItems();
            string[] packageDirs = GetPackageDirs();
            foreach (string refItem in referenceItems)
            {
                MetadataReference reference = GetFirstReference(refItem, packageDirs);
                if (reference == null)
                {
                    throw new InvalidOperationException("Unable to find reference: " + refItem);
                }

                yield return reference;
            }
        }

        private static MetadataReference GetFirstReference(string path, string[] packageDirs)
        {
            foreach (string packageDir in packageDirs)
            {
                string transformed = path.Replace("{nupkgdir}", packageDir);
                transformed = transformed.Replace("{appcontextbasedirectory}", AppContext.BaseDirectory);
                if (File.Exists(transformed))
                {
                    using (FileStream fs = File.OpenRead(transformed))
                    {
                        var result = MetadataReference.CreateFromStream(fs, filePath: transformed);
                        return result;
                    }
                }
            }

            return null;
        }

        private static string[] GetCompileItems()
        {
            return Directory.EnumerateFiles(ProjectBasePath, "*.cs", SearchOption.AllDirectories).ToArray();
        }

        private static string[] GetReferenceItems()
        {
            string[] lines = File.ReadAllLines(Path.Combine(ProjectBasePath, "References.txt"));
            return lines.Select(l => l.Trim()).ToArray(); ;
        }

        public static string[] GetPackageDirs()
        {
            List<string> dirs = new List<string>();
            dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dirs.Add(@"C:\Program Files\dotnet\sdk\NuGetFallbackFolder");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                dirs.Add("/usr/local/share/dotnet/sdk/NuGetFallbackFolder");
            }
            else
            {
                dirs.Add("/usr/share/dotnet/sdk/NuGetFallbackFolder");
            }

            return dirs.ToArray();
        }

        public static LanguageBackend[] GetAllBackends(Compilation compilation, ToolFeatures features = ToolFeatures.Transpilation)
            => ToolChain.Requires(features, false).Select(t => t.CreateBackend(compilation))
                .ToArray();
    }
}
