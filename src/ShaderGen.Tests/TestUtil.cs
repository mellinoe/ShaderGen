using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;
using System.Threading;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
using ShaderGen.Tests.Tools;

namespace ShaderGen.Tests
{
    internal static class TestUtil
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
                        {
                            throw new InvalidOperationException($"Unable to find reference \"{path}\".");
                        }
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
                        {
                            references[index] = MetadataReference.CreateFromStream(fs, filePath: path);
                        }
                    }

                    return references;
                },
                LazyThreadSafetyMode.ExecutionAndPublication);

        public static IReadOnlyList<MetadataReference> ProjectReferences => _projectReferences.Value;

        public static LanguageBackend[] GetAllBackends(Compilation compilation, ToolFeatures features = ToolFeatures.Transpilation)
            => ToolChain.Requires(features, false).Select(t => t.CreateBackend(compilation))
                .ToArray();

        public static IReadOnlyCollection<(string fieldName, object aValue, object bValue)> DeepCompareObjectFields<T>(T a, T b)
        {
            // Creat failures list
            List<(string fieldName, object aValue, object bValue)> failures = new List<(string fieldName, object aValue, object bValue)>();

            // Get dictionary of fields by field name and type
            Dictionary<Type, IReadOnlyCollection<FieldInfo>> childFieldInfos =
                new Dictionary<Type, IReadOnlyCollection<FieldInfo>>();

            Type currentType = typeof(T);
            object aValue = a;
            object bValue = b;
            Stack<(string fieldName, Type fieldType, object aValue, object bValue)> stack
                = new Stack<(string fieldName, Type fieldType, object aValue, object bValue)>();
            stack.Push((String.Empty, currentType, aValue, bValue));

            while (stack.Count > 0)
            {
                // Pop top of stack.
                var frame = stack.Pop();
                currentType = frame.fieldType;
                aValue = frame.aValue;
                bValue = frame.bValue;

                if (Equals(aValue, bValue))
                {
                    continue;
                }

                // Get fields (cached)
                if (!childFieldInfos.TryGetValue(currentType, out IReadOnlyCollection<FieldInfo> childFields))
                {
                    childFieldInfos.Add(currentType, childFields = currentType.GetFields().Where(f => !f.IsStatic).ToArray());
                }

                if (childFields.Count < 1)
                {
                    // No child fields, we have an inequality
                    string fullName = frame.fieldName;
                    failures.Add((fullName, aValue, bValue));
                    continue;
                }

                foreach (FieldInfo childField in childFields)
                {
                    object aMemberValue = childField.GetValue(aValue);
                    object bMemberValue = childField.GetValue(bValue);

                    // Short cut equality
                    if (Equals(aMemberValue, bMemberValue))
                    {
                        continue;
                    }

                    string fullName = String.IsNullOrWhiteSpace(frame.fieldName)
                        ? childField.Name
                        : $"{frame.fieldName}.{childField.Name}";
                    stack.Push((fullName, childField.FieldType, aMemberValue, bMemberValue));
                }
            }

            return failures.AsReadOnly();
        }

        /// <summary>
        /// The random number generators for each thread.
        /// </summary>
        private static readonly ThreadLocal<Random> _randomGenerators =
            new ThreadLocal<Random>(() => new Random());

        /// <summary>
        /// Fills a struct with Random floats.
        /// </summary>
        /// <typeparam name="T">The random type</typeparam>
        /// <param name="minMantissa">The minimum mantissa.</param>
        /// <param name="maxMantissa">The maximum mantissa.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// minMantissa
        /// or
        /// maxMantissa
        /// </exception>
        public static unsafe T FillRandomFloats<T>(int minMantissa = -126, int maxMantissa = 128) where T : struct
        {
            if (minMantissa < -126)
            {
                throw new ArgumentOutOfRangeException(nameof(minMantissa));
            }
            if (maxMantissa < minMantissa || maxMantissa > 128)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMantissa));
            }
            Random random = _randomGenerators.Value;
            int floatCount = Unsafe.SizeOf<T>() / sizeof(float);
            float* floats = stackalloc float[floatCount];
            for (int i = 0; i < floatCount; i++)
            {
                floats[i] = (float)((random.NextDouble() * 2.0 - 1.0) * Math.Pow(2.0, random.Next(minMantissa, maxMantissa)));
                //floats[i] = (float)(random.NextDouble() * floatRange * 2f) - floatRange;
            }

            return Unsafe.Read<T>(floats);
        }

        /// <summary>
        /// Gets a set of random floats.
        /// </summary>
        /// <param name="floatCount">The number of floats.</param>
        /// <param name="minMantissa">The minimum mantissa.</param>
        /// <param name="maxMantissa">The maximum mantissa.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">minMantissa
        /// or
        /// maxMantissa</exception>
        public static float[] GetRandomFloats(int floatCount, int minMantissa = -126, int maxMantissa = 128)
        {
            Random random = _randomGenerators.Value;
            float[] floats = new float[floatCount];
            for (int i = 0; i < floatCount; i++)
            {
                floats[i] = (float)((random.NextDouble() * 2.0 - 1.0) * Math.Pow(2.0, random.Next(minMantissa, maxMantissa)));
                //floats[i] = (float)(random.NextDouble() * floatRange * 2f) - floatRange;
            }

            return floats;
        }

        #region ToMemorySize from https://github.com/webappsuk/CoreLibraries/blob/fbbebc99bc5c1f2e8b140c6c387e3ede4f89b40c/Utilities/UtilityExtensions.cs#L2951-L3116
        private static readonly string[] _memoryUnitsLong =
        {
            " byte",
            " kilobyte",
            " megabyte",
            " gigabyte",
            " terabyte",
            " petabyte",
            " exabyte"
        };

        private static readonly string[] _memoryUnitsShort = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

        /// <summary>
        /// Converts a number of bytes to a friendly memory size.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="longUnits">if set to <see langword="true" /> use long form unit names instead of symbols.</param>
        /// <param name="decimalPlaces">The number of decimal places between 0 and 16 (ignored for bytes).</param>
        /// <param name="breakPoint">The break point between 0 and 1024 (or 0D to base on decimal points).</param>
        /// <returns>System.String.</returns>
        public static string ToMemorySize(
            this short bytes,
            bool longUnits = false,
            uint decimalPlaces = 1,
            double breakPoint = 512D) => ToMemorySize((double)bytes, longUnits, decimalPlaces, breakPoint);

        /// <summary>
        /// Converts a number of bytes to a friendly memory size.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="longUnits">if set to <see langword="true" /> use long form unit names instead of symbols.</param>
        /// <param name="decimalPlaces">The number of decimal places between 0 and 16 (ignored for bytes).</param>
        /// <param name="breakPoint">The break point between 0 and 1024 (or 0D to base on decimal points).</param>
        /// <returns>System.String.</returns>
        public static string ToMemorySize(
            this ushort bytes,
            bool longUnits = false,
            uint decimalPlaces = 1,
            double breakPoint = 512D) => ToMemorySize((double)bytes, longUnits, decimalPlaces, breakPoint);

        /// <summary>
        /// Converts a number of bytes to a friendly memory size.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="longUnits">if set to <see langword="true" /> use long form unit names instead of symbols.</param>
        /// <param name="decimalPlaces">The number of decimal places between 0 and 16 (ignored for bytes).</param>
        /// <param name="breakPoint">The break point between 0 and 1024 (or 0D to base on decimal points).</param>
        /// <returns>System.String.</returns>
        public static string ToMemorySize(
            this int bytes,
            bool longUnits = false,
            uint decimalPlaces = 1,
            double breakPoint = 512D) => ToMemorySize((double)bytes, longUnits, decimalPlaces, breakPoint);

        /// <summary>
        /// Converts a number of bytes to a friendly memory size.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="longUnits">if set to <see langword="true" /> use long form unit names instead of symbols.</param>
        /// <param name="decimalPlaces">The number of decimal places between 0 and 16 (ignored for bytes).</param>
        /// <param name="breakPoint">The break point between 0 and 1024 (or 0D to base on decimal points).</param>
        /// <returns>System.String.</returns>
        public static string ToMemorySize(
            this uint bytes,
            bool longUnits = false,
            uint decimalPlaces = 1,
            double breakPoint = 512D) => ToMemorySize((double)bytes, longUnits, decimalPlaces, breakPoint);

        /// <summary>
        /// Converts a number of bytes to a friendly memory size.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="longUnits">if set to <see langword="true" /> use long form unit names instead of symbols.</param>
        /// <param name="decimalPlaces">The number of decimal places between 0 and 16 (ignored for bytes).</param>
        /// <param name="breakPoint">The break point between 0 and 1024 (or 0D to base on decimal points).</param>
        /// <returns>System.String.</returns>
        public static string ToMemorySize(
            this long bytes,
            bool longUnits = false,
            uint decimalPlaces = 1,
            double breakPoint = 512D) => ToMemorySize((double)bytes, longUnits, decimalPlaces, breakPoint);

        /// <summary>
        /// Converts a number of bytes to a friendly memory size.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="longUnits">if set to <see langword="true" /> use long form unit names instead of symbols.</param>
        /// <param name="decimalPlaces">The number of decimal places between 0 and 16 (ignored for bytes).</param>
        /// <param name="breakPoint">The break point between 0 and 1024 (or 0D to base on decimal points).</param>
        /// <returns>System.String.</returns>
        public static string ToMemorySize(
            this ulong bytes,
            bool longUnits = false,
            uint decimalPlaces = 1,
            double breakPoint = 512D) => ToMemorySize((double)bytes, longUnits, decimalPlaces, breakPoint);

        /// <summary>
        /// Converts a number of bytes to a friendly memory size.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="longUnits">if set to <see langword="true" /> use long form unit names instead of symbols.</param>
        /// <param name="decimalPlaces">The number of decimal places between 0 and 16 (ignored for bytes).</param>
        /// <param name="breakPoint">The break point between 0 and 1024 (or 0D to base on decimal points).</param>
        /// <returns>System.String.</returns>
        public static string ToMemorySize(
            this double bytes,
            bool longUnits = false,
            uint decimalPlaces = 1,
            double breakPoint = 512D)
        {
            if (decimalPlaces < 1)
            {
                decimalPlaces = 0;
            }
            else if (decimalPlaces > 16)
            {
                decimalPlaces = 16;
            }

            // 921.6 is 0.9*1024, this means that be default the breakpoint will round up the last decimal place.
            if (breakPoint < 1)
            {
                breakPoint = 921.6D * Math.Pow(10, -decimalPlaces);
            }
            else if (breakPoint > 1023)
            {
                breakPoint = 1023;
            }

            uint maxDecimalPlaces = 0;
            uint unit = 0;
            double amount = bytes;
            while ((Math.Abs(amount) >= breakPoint) &&
                   (unit < 6))
            {
                amount /= 1024;
                unit++;
                maxDecimalPlaces = Math.Min(decimalPlaces, maxDecimalPlaces + 3);
            }

            string format = "{0:N" + maxDecimalPlaces + "}{1}";
            return string.Format(
                format,
                amount,
                longUnits
                    ? _memoryUnitsLong[unit]
                    : _memoryUnitsShort[unit]);
        }
        #endregion
    }
}