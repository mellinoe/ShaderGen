using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using ShaderGen.Tests.Tools;
using TestShaders;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Xunit;
using Xunit.Abstractions;

namespace ShaderGen.Tests
{
    public class BuiltinsTests
    {
        /// <summary>
        /// The skip reason, set to <see langword="null"/> to enable tests in class.
        /// </summary>
        private const string SkipReason = null;

        /// <summary>
        /// The methods to exclude from <see cref="ShaderBuiltins"/>
        /// </summary>
        /// <remarks>TODO See #78 to show why this is another reason to split ShaderBuiltins.</remarks>
        private static readonly HashSet<string> _gpuOnly = new HashSet<string>
        {
            nameof(ShaderBuiltins.Sample),
            nameof(ShaderBuiltins.SampleGrad),
            nameof(ShaderBuiltins.Load),
            nameof(ShaderBuiltins.Store),
            nameof(ShaderBuiltins.SampleComparisonLevelZero),
            nameof(ShaderBuiltins.Discard),
            nameof(ShaderBuiltins.ClipToTextureCoordinates),
            nameof(ShaderBuiltins.Ddx),
            nameof(ShaderBuiltins.DdxFine),
            nameof(ShaderBuiltins.Ddy),
            nameof(ShaderBuiltins.DdyFine),
            nameof(ShaderBuiltins.InterlockedAdd)
        };


        /// <summary>
        /// The maximum test duration for each backend.
        /// </summary>
        private static readonly TimeSpan TestDuration = TimeSpan.FromSeconds(3);

        /// <summary>
        /// The number of test iterations for each backend.
        /// </summary>
        private const int TestLoops = 1000;

        private readonly ITestOutputHelper _output;

        public BuiltinsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkippableFact(typeof(RequiredToolFeatureMissingException), Skip = SkipReason)]
        private void TestBuiltins()
        {
            // Find all backends that can create a headless graphics device on this system.
            IReadOnlyCollection<ToolChain> toolChains = ToolChain.Requires(ToolFeatures.HeadlessGraphicsDevice, false);
            if (toolChains.Count < 1)
            {
                throw new RequiredToolFeatureMissingException(
                    $"At least one tool chain capable of creating headless graphics devices is required for this test!");
            }

            string csFunctionName = "ComputeShader.CS";

            // Get all the methods we wish to test
            IReadOnlyCollection<MethodInfo> methods = typeof(ShaderBuiltins)
                .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
                .Where(m => !_gpuOnly.Contains(m.Name) && !m.IsSpecialName)
                .OrderBy(m => m.Name)
                .ToArray();

            /*
             * Auto-generate C# code for testing methods.
             */
            Mappings mappings = CreateMethodTestCompilation(methods, out Compilation compilation);

            // Note, you could use compilation.Emit(...) at this point here to compile the auto-generated code!
            // however, for now we'll invoke methods directly rather than executing the C# code that has been
            // generated, as loading emitted code into a test is currently much more difficult.
            byte[] testData = null;
            byte[] cpuResults = null;
            using (new TestTimer(
                _output,
                t => $"Generated test data ({testData.Length.ToMemorySize()}) and completed {mappings.Tests} iterations of {mappings.Methods.Count} methods (results took {cpuResults.Length.ToMemorySize()}) in {t * 1000:#.##}ms."))
            {
                (testData, cpuResults) = mappings.GenerateTestData();
            }

            /*
             * Compile backend
             */
            LanguageBackend[] backends;
            ShaderGenerationResult generationResult;

            using (new TestTimer(
                _output,
                t => $"Generated shader sets for {string.Join(", ", toolChains.Select(tc => tc.Name))} backends in {t * 1000:#.##}ms."))
            {
                backends = toolChains.Select(t => t.CreateBackend(compilation)).ToArray();

                ShaderGenerator sg = new ShaderGenerator(
                    compilation,
                    backends,
                    null,
                    null,
                    csFunctionName);

                generationResult = sg.GenerateShaders();
            }

            foreach (LanguageBackend backend in backends)
            {
                ToolChain toolChain = ToolChain.Get(backend);
                GeneratedShaderSet set;
                CompileResult compilationResult;

                using (new TestTimer(_output, $"Compiling Compute Shader for {toolChain.GraphicsBackend}"))
                {
                    set = generationResult.GetOutput(backend).Single();
                    compilationResult = toolChain.Compile(set.ComputeShaderCode, Stage.Compute, set.ComputeFunction.Name);
                }
                if (compilationResult.HasError)
                {
                    _output.WriteLine($"Failed to compile Compute Shader from set \"{set.Name}\"!");
                    _output.WriteLine(compilationResult.ToString());
                    Assert.True(false);
                }

                Assert.NotNull(compilationResult.CompiledOutput);
            }
        }

        /// <summary>
        /// Creates the method test compilation.
        /// </summary>
        /// <param name="methods">The methods.</param>
        /// <returns></returns>
        private Mappings CreateMethodTestCompilation(IReadOnlyCollection<MethodInfo> methods, out Compilation compilation)
        {
            Assert.NotNull(methods);
            Assert.NotEmpty(methods);

            // Create compilation
            CSharpCompilationOptions cSharpCompilationOptions =
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
            compilation = CSharpCompilation.Create(
                "TestAssembly",
                null,
                TestUtil.ProjectReferences,
                cSharpCompilationOptions);

            List<MethodMap> methodMaps = new List<MethodMap>(methods.Count);
            PaddedStructCreator paddedStructCreator = new PaddedStructCreator(compilation);

            StringBuilder codeBuilder = new StringBuilder();
            codeBuilder.Append(Resource.SBSP1);
            codeBuilder.Append(methods.Count);
            codeBuilder.Append(Resource.SBSP2);

            StringBuilder argsBuilder = new StringBuilder();
            /*
             * Output test cases
             */
            int methodNumber = 0;
            foreach (MethodInfo method in methods)
            {
                Assert.True(method.IsStatic);

                ParameterInfo[] parameterInfos = method.GetParameters();
                Dictionary<ParameterInfo, string> parameterMap =
                    new Dictionary<ParameterInfo, string>(parameterInfos.Length);

                foreach (ParameterInfo parameterInfo in parameterInfos)
                {
                    if (argsBuilder.Length > 0)
                    {
                        argsBuilder.Append(",");
                    }

                    string fieldName = paddedStructCreator.GetFieldName(parameterInfo.ParameterType);
                    parameterMap.Add(parameterInfo, fieldName);
                    argsBuilder.Append(Resource.SBSParam.Replace("$$NAME$$", fieldName));
                }

                string returnName = method.ReturnType != typeof(void)
                    ? paddedStructCreator.GetFieldName(method.ReturnType)
                    : null;

                string output = returnName != null
                    ? Resource.SBSParam.Replace("$$NAME$$", returnName) + " = "
                    : string.Empty;

                codeBuilder.Append(Resource.SBSCase
                    .Replace("$$CASE$$", methodNumber.ToString())
                    .Replace("$$RESULT$$", output)
                    .Replace("$$METHOD$$", $"{method.DeclaringType.FullName}.{method.Name}")
                    .Replace("$$ARGS$$", argsBuilder.ToString()));

                methodMaps.Add(new MethodMap(methodNumber, method, parameterMap, returnName));

                methodNumber++;
                paddedStructCreator.Reset();
                argsBuilder.Clear();
            }

            codeBuilder.Append(Resource.SBSP3);

            /*
             * Output test fields
             */
            IReadOnlyList<PaddedStructCreator.Field> fields = paddedStructCreator.GetFields(out int bufferSize);
            int size = 0;
            foreach (PaddedStructCreator.Field field in fields)
            {
                codeBuilder.AppendLine($"        // {size,3}: Alignment = {field.AlignmentInfo.ShaderAlignment} {(field.IsPaddingField ? " [PADDING}" : string.Empty)}");
                codeBuilder.AppendLine($"        {(field.IsPaddingField ? "private" : "public")} {field.Type.FullName} {field.Name};");
                codeBuilder.AppendLine(string.Empty);
                size += field.AlignmentInfo.ShaderSize;
            }
            Assert.Equal(size, bufferSize);

            codeBuilder.Append(Resource.SBSP4);

            string code = codeBuilder.ToString();
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));
            return new Mappings(bufferSize, fields.ToDictionary(f => f.Name), methodMaps, TestLoops);
        }

        /// <summary>
        /// Holds information about the mappings of tested methods to the buffer.
        /// </summary>
        internal class Mappings
        {
            /// <summary>
            /// The buffer size required.
            /// </summary>
            public readonly int BufferSize;

            public readonly int ResultSetSize;

            /// <summary>
            /// The buffer fields by name.
            /// </summary>
            public readonly IReadOnlyDictionary<string, PaddedStructCreator.Field> BufferFields;

            /// <summary>
            /// The method maps.
            /// </summary>
            public readonly IReadOnlyCollection<MethodMap> Methods;

            /// <summary>
            /// The number of tests.
            /// </summary>
            public readonly int Tests;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mappings" /> class.
            /// </summary>
            /// <param name="bufferSize">Size of the buffer.</param>
            /// <param name="bufferFields">The buffer fields.</param>
            /// <param name="methods">The methods.</param>
            public Mappings(int bufferSize, IReadOnlyDictionary<string, PaddedStructCreator.Field> bufferFields, IReadOnlyCollection<MethodMap> methods, int tests)
            {
                BufferSize = bufferSize;
                BufferFields = bufferFields;
                Methods = methods;
                Tests = tests;

                // Calcualtes size required for result set
                ResultSetSize = methods.Sum(method => bufferFields[method.Return]?.AlignmentInfo.ShaderSize ?? 0);
            }

            /// <summary>
            /// Generates test data and results .
            /// </summary>
            public (byte[] testData, byte[] results) GenerateTestData()
            {
                byte[] testData = new byte[BufferSize * Tests * Methods.Count];
                byte[] results = new byte[ResultSetSize * Tests];

                int t = 0;
                int resultPos = 0;
                for (int test = 0; test < Tests; test++)
                {
                    Assert.Equal(0, resultPos % ResultSetSize);
                    Assert.Equal(test, resultPos / ResultSetSize);

                    foreach (MethodMap method in Methods)
                    {
                        method.GenerateTestData(this, testData, BufferSize * t++, results, resultPos);
                        resultPos += BufferFields[method.Return]?.AlignmentInfo.ShaderSize ?? 0;
                    }
                }

                return (testData, results);
            }
        }

        /// <summary>
        /// Holds information about the mapping of a tested method parameters and return to a buffer.
        /// </summary>
        internal class MethodMap
        {
            /// <summary>
            /// The index of the method.
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// The method info.
            /// </summary>
            public readonly MethodInfo Method;

            /// <summary>
            /// The parameter to field name map.
            /// </summary>
            public readonly IReadOnlyDictionary<ParameterInfo, string> Parameters;

            /// <summary>
            /// The return value to field name map.
            /// </summary>
            public readonly string Return;

            /// <summary>
            /// Initializes a new instance of the <see cref="MethodMap"/> class.
            /// </summary>
            /// <param name="index">The index.</param>
            /// <param name="method">The method.</param>
            /// <param name="parameters">The parameters.</param>
            /// <param name="return">The return.</param>
            public MethodMap(int index, MethodInfo method, IReadOnlyDictionary<ParameterInfo, string> parameters, string @return)
            {
                Index = index;
                Method = method;
                Parameters = parameters;
                Return = @return;
            }

            /// <summary>
            /// Generates test data for this method, executes it and stores the result.
            /// </summary>
            /// <param name="mapping">The mapping.</param>
            /// <param name="testData">The test data.</param>
            /// <param name="dataOffset">The data offset.</param>
            /// <param name="results">The results.</param>
            /// <param name="resultsOffset">The results offset.</param>
            public unsafe void GenerateTestData(Mappings mapping, byte[] testData, int dataOffset, byte[] results, int resultsOffset)
            {
                // TODO I suspect this can all be done a lot easier with Span<T> once upgraded to .Net Core 2.1
                object[] parameters = new object[Parameters.Count];
                GCHandle handle;
                IntPtr ptr;

                // Create random input values
                foreach (KeyValuePair<ParameterInfo, string> kvp in Parameters)
                {
                    ParameterInfo pInfo = kvp.Key;
                    PaddedStructCreator.Field field = mapping.BufferFields[kvp.Value];
                    int floatCount = (int)Math.Ceiling(
                        (float)Math.Max(field.AlignmentInfo.ShaderSize, field.AlignmentInfo.CSharpSize) /
                        sizeof(float));

                    // Get random floats to fill parameter structure
                    float[] floats = TestUtil.GetRandomFloats(floatCount);
                    handle = GCHandle.Alloc(floats, GCHandleType.Pinned);
                    try
                    {
                        // Create object of correct type
                        ptr = Marshal.AllocCoTaskMem(field.AlignmentInfo.CSharpSize);
                        Marshal.Copy(floats, 0, ptr, floats.Length);
                        parameters[pInfo.Position] = Marshal.PtrToStructure(ptr, field.Type);

                        // Fill test data
                        ptr = Marshal.UnsafeAddrOfPinnedArrayElement(testData, dataOffset + field.Position);
                        Marshal.Copy(floats, 0, ptr, floats.Length);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }

                object result = Method.Invoke(null, parameters);

                if (Return == null)
                {
                    Assert.Null(result);
                    return;
                }

                PaddedStructCreator.Field resultField = mapping.BufferFields[Return];
                handle = GCHandle.Alloc(result, GCHandleType.Pinned);
                try
                {
                    // Fill test data
                    Marshal.Copy(handle.AddrOfPinnedObject(), results, resultsOffset, resultField.AlignmentInfo.ShaderSize);
                }
                finally
                {
                    handle.Free();
                }
            }
        }
    }
}