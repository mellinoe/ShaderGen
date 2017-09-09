using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace ShaderGen.Tests
{
    internal class TestUtil
    {
        private static readonly string ProjectBasePath = Path.Combine(AppContext.BaseDirectory, "TestAssets");

        public static Compilation GetTestProjectCompilation()
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees: GetSyntaxTrees(),
                references: GetProjectReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
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
                        return MetadataReference.CreateFromStream(fs, filePath: transformed);
                    }
                }
            }

            return null;
        }

        private static string[] GetCompileItems()
        {
            return Directory.EnumerateFiles(ProjectBasePath, "*.cs").ToArray();
        }

        private static string[] GetReferenceItems()
        {
            string[] lines = File.ReadAllLines(Path.Combine(ProjectBasePath, "References.txt"));
            return lines;
        }

        public static string[] GetPackageDirs()
        {
            return new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"),
                @"C:\Program Files\dotnet\sdk\NuGetFallbackFolder",
            };
        }
    }

    public class TempFile : IDisposable
    {
        public readonly string FilePath;

        public TempFile()
        {
            FilePath = Path.GetTempFileName();
        }

        public void Dispose()
        {
            File.Delete(FilePath);
        }
    }
}
