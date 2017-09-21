using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;

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
                        var result = MetadataReference.CreateFromStream(fs, filePath: transformed);
                        return result;
                    }
                }
            }

            return null;
        }

        private static int s_thing = 0;

        private static string[] GetCompileItems()
        {
            return Directory.EnumerateFiles(ProjectBasePath, "*.cs", SearchOption.AllDirectories).ToArray();
        }

        private static string[] GetReferenceItems()
        {
            string[] lines = File.ReadAllLines(Path.Combine(ProjectBasePath, "References.txt"));
            return lines.Select(l => l.Trim()).ToArray();;
        }

        public static string[] GetPackageDirs()
        {
            List<string> dirs = new List<string>();
            dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dirs.Add(@"C:\Program Files\dotnet\sdk\NuGetFallbackFolder");
            }
            else
            {
                dirs.Add("/usr/share/dotnet/sdk/NuGetFallbackFolder");
            }

            return dirs.ToArray();
        }

        public static LanguageBackend[] GetAllBackends(Compilation compilation)
        {
            return new LanguageBackend[]
            {
                new HlslBackend(compilation),
                new Glsl330Backend(compilation),
                new Glsl450Backend(compilation)
            };
        }
    }

    public class TempFile : IDisposable
    {
        public readonly string FilePath;

        public TempFile() : this(Path.GetTempFileName()) { }
        public TempFile(string path)
        {
            FilePath = path;
        }

        public static implicit operator string(TempFile tf) => tf.FilePath;

        public void Dispose()
        {
            File.Delete(FilePath);
        }
    }

    public class TempFile2 : IDisposable
    {
        public readonly string FilePath0;
        public readonly string FilePath1;

        public TempFile2() : this(Path.GetTempFileName(), Path.GetTempFileName()) { }
        public TempFile2(string path0, string path1)
        {
            FilePath0 = path0;
            FilePath1 = path1;
        }

        public void Dispose()
        {
            File.Delete(FilePath0);
            File.Delete(FilePath1);
        }
    }
}
