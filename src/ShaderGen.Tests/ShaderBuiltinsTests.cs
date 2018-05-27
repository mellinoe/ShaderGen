using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using ShaderGen.Tests.Tools;
using TestShaders;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Xunit;
using Xunit.Abstractions;

namespace ShaderGen.Tests
{
    public class ShaderBuiltinsTests
    {
        /// <summary>
        /// The skip reason, set to <see langword="null"/> to enable tests in class.
        /// </summary>
        private const string SkipReason = null; // "Currently skipping automatic tests until closer implementations can be found.";

        /// <summary>
        /// The test will fail when the GPU & CPU has any methods that fail higher than the ratio.
        /// A value of 1.0f will never fail due to inconsistencies.
        /// </summary>
        private const float MaximumFailureRate = 1f;

        /// <summary>
        /// How close float's need to be, to be considered a match (ratio).
        /// </summary>
        private const float Tolerance = 0.001f;

        /// <summary>
        /// How close float's need to be, to be considered a match (ratio).
        /// </summary>
        private const float ToleranceRatio = 0.001f;

        /// <summary>
        /// The number of failure examples to output
        /// </summary>
        private const int FailureExamples = 3;

        /// <summary>
        /// The range of valid input float values (+/- this value).
        /// </summary>
        private static readonly float FloatRange = 10000f;

        /// <summary>
        /// Will ignore failures if either value is <see cref="float.NaN"/>.
        /// </summary>
        private const bool IgnoreNan = true;
        /// <summary>
        /// Will ignore failures if either value is <see cref="float.PositiveInfinity"/> or  <see cref="float.NegativeInfinity"/>
        /// </summary>
        private const bool IgnoreInfinity = true;

        /// <summary>
        /// The maximum test duration for each backend.
        /// </summary>
        private static readonly TimeSpan TestDuration = TimeSpan.FromSeconds(3);

        /// <summary>
        /// The maximum iteration for each backend.
        /// </summary>
        private static readonly int TestLoops = 10000;

        private readonly ITestOutputHelper _output;

        public ShaderBuiltinsTests(ITestOutputHelper output)
        {
            _output = output;
        }


        [SkippableFact(typeof(RequiredToolFeatureMissingException), Skip = SkipReason)]
        public void TestShaderBuiltins_GlslEs300()
            => TestShaderBuiltins(ToolChain.GlslEs300);

        [SkippableFact(typeof(RequiredToolFeatureMissingException), Skip = SkipReason)]
        public void TestShaderBuiltins_Glsl330()
            => TestShaderBuiltins(ToolChain.Glsl330);

        [SkippableFact(typeof(RequiredToolFeatureMissingException), Skip = SkipReason)]
        public void TestShaderBuiltins_Glsl450()
            => TestShaderBuiltins(ToolChain.Glsl450);

        [SkippableFact(typeof(RequiredToolFeatureMissingException), Skip = SkipReason)]
        public void TestShaderBuiltins_Hlsl()
            => TestShaderBuiltins(ToolChain.Direct3D11);

        [SkippableFact(typeof(RequiredToolFeatureMissingException), Skip = SkipReason)]
        public void TestShaderBuiltins_Metal()
            => TestShaderBuiltins(ToolChain.Metal);

        private void TestShaderBuiltins(ToolChain toolChain)
        {
            if (!toolChain.Features.HasFlag(ToolFeatures.ToHeadless))
            {
                throw new RequiredToolFeatureMissingException(
                    $"The {toolChain} does not support creating a headless graphics device!");
            }

            string csFunctionName =
                $"{nameof(TestShaders)}.{nameof(ShaderBuiltinsComputeTest)}.{nameof(ShaderBuiltinsComputeTest.CS)}";
            Compilation compilation = TestUtil.GetCompilation();

            LanguageBackend backend = toolChain.CreateBackend(compilation);

            /*
             * Compile backend
             */
            ShaderSetProcessor processor = new ShaderSetProcessor();

            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                backend,
                null,
                null,
                csFunctionName,
                processor);

            ShaderGenerationResult generationResult = sg.GenerateShaders();
            GeneratedShaderSet set = generationResult.GetOutput(backend).Single();
            _output.WriteLine($"Generated shader set for {toolChain.Name} backend.");

            CompileResult compilationResult =
                toolChain.Compile(set.ComputeShaderCode, Stage.Compute, set.ComputeFunction.Name);
            if (compilationResult.HasError)
            {
                _output.WriteLine($"Failed to compile Compute Shader from set \"{set.Name}\"!");
                _output.WriteLine(compilationResult.ToString());
                Assert.True(false);
            }
            else
            {
                _output.WriteLine($"Compiled Compute Shader from set \"{set.Name}\"!");
            }

            Assert.NotNull(compilationResult.CompiledOutput);

            int sizeOfParametersStruct = Unsafe.SizeOf<ComputeShaderParameters>();

            // Create failure data structure, first by method #, then by field name.
            Dictionary<int, List<Tuple<ComputeShaderParameters, ComputeShaderParameters,
                IReadOnlyCollection<Tuple<string, float, float>>>>> failures
                = new Dictionary<int, List<Tuple<ComputeShaderParameters, ComputeShaderParameters,
                    IReadOnlyCollection<Tuple<string, float, float>>>>>();

            // We need two copies, one for the CPU & one for GPU
            ComputeShaderParameters[] cpuParameters = new ComputeShaderParameters[ShaderBuiltinsComputeTest.Methods];
            ComputeShaderParameters[] gpuParameters = new ComputeShaderParameters[ShaderBuiltinsComputeTest.Methods];
            int loops = 0;
            long durationTicks;

            // Set start.
            long startTicks = Stopwatch.GetTimestamp();

            ShaderBuiltinsComputeTest cpuTest = new ShaderBuiltinsComputeTest();
            /*
             * Run shader on GPU.
             */
            using (GraphicsDevice graphicsDevice = toolChain.CreateHeadless())
            {
                ResourceFactory factory = graphicsDevice.ResourceFactory;
                using (DeviceBuffer inOutBuffer = factory.CreateBuffer(
                    new BufferDescription(
                        (uint)sizeOfParametersStruct * ShaderBuiltinsComputeTest.Methods,
                        BufferUsage.StructuredBufferReadWrite,
                        (uint)sizeOfParametersStruct)))

                using (Shader computeShader = factory.CreateShader(
                    new ShaderDescription(
                        ShaderStages.Compute,
                        compilationResult.CompiledOutput,
                        nameof(ShaderBuiltinsComputeTest.CS))))

                using (ResourceLayout inOutStorageLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("InOutBuffer", ResourceKind.StructuredBufferReadWrite,
                        ShaderStages.Compute))))

                using (Pipeline computePipeline = factory.CreateComputePipeline(new ComputePipelineDescription(
                    computeShader,
                    new[] { inOutStorageLayout },
                    1, 1, 1)))


                using (ResourceSet computeResourceSet = factory.CreateResourceSet(
                    new ResourceSetDescription(inOutStorageLayout, inOutBuffer)))

                using (CommandList commandList = factory.CreateCommandList())
                {
                    // Ensure the headless graphics device is the backend we expect.
                    Assert.Equal(toolChain.GraphicsBackend, graphicsDevice.BackendType);

                    _output.WriteLine("Created compute pipeline.");

                    do
                    {
                        /*
                         * Build test data in parallel
                         */
                        Parallel.For(
                            0,
                            ShaderBuiltinsComputeTest.Methods,
                            i => cpuParameters[i] = gpuParameters[i] = FillRandomFloats<ComputeShaderParameters>());

                        /*
                         * Run shader on CPU in parallel
                         */
                        cpuTest.InOutParameters = new RWStructuredBuffer<ComputeShaderParameters>(ref cpuParameters);
                        Parallel.For(0, ShaderBuiltinsComputeTest.Methods,
                            i => cpuTest.DoCS(new UInt3 { X = (uint)i, Y = 0, Z = 0 }));

                        // Update parameter buffer
                        graphicsDevice.UpdateBuffer(inOutBuffer, 0, gpuParameters);
                        graphicsDevice.WaitForIdle();

                        // Execute compute shaders
                        commandList.Begin();
                        commandList.SetPipeline(computePipeline);
                        commandList.SetComputeResourceSet(0, computeResourceSet);
                        commandList.Dispatch(ShaderBuiltinsComputeTest.Methods, 1, 1);
                        commandList.End();

                        graphicsDevice.SubmitCommands(commandList);
                        graphicsDevice.WaitForIdle();

                        // Read back parameters using a staging buffer
                        using (DeviceBuffer stagingBuffer =
                            factory.CreateBuffer(new BufferDescription(inOutBuffer.SizeInBytes, BufferUsage.Staging)))
                        {
                            commandList.Begin();
                            commandList.CopyBuffer(inOutBuffer, 0, stagingBuffer, 0, stagingBuffer.SizeInBytes);
                            commandList.End();
                            graphicsDevice.SubmitCommands(commandList);
                            graphicsDevice.WaitForIdle();

                            // Read back parameters
                            MappedResourceView<ComputeShaderParameters> map =
                                graphicsDevice.Map<ComputeShaderParameters>(stagingBuffer, MapMode.Read);
                            for (int i = 0; i < gpuParameters.Length; i++)
                            {
                                gpuParameters[i] = map[i];
                            }

                            graphicsDevice.Unmap(stagingBuffer);
                        }

                        /*
                         * Compare results
                         */
                        for (int method = 0; method < ShaderBuiltinsComputeTest.Methods; method++)
                        {
                            ComputeShaderParameters cpu = cpuParameters[method];
                            ComputeShaderParameters gpu = gpuParameters[method];

                            // Filter results based on tolerances.
                            IReadOnlyCollection<Tuple<string, float, float>> deepCompareObjectFields =
                                DeepCompareObjectFields(cpu, gpu)
                                    .Select(t => Tuple.Create(t.Item1, (float)t.Item2, (float)t.Item3))
                                    .Where(t =>
                                    {
#pragma warning disable 162
                                        float a = t.Item2;
                                        float b = t.Item3;
                                        bool comparable = true;
                                        if (float.IsNaN(a) || float.IsNaN(b))
                                        {
                                            if (IgnoreNan)
                                            {
                                                return false;
                                            }

                                            comparable = false;
                                        }
                                        if (float.IsInfinity(a) || float.IsInfinity(b))
                                        {
                                            if (IgnoreInfinity)
                                            {
                                                return false;
                                            }

                                            comparable = false;
                                        }

                                        return !comparable ||
                                               Math.Abs(1.0f - a / b) > ToleranceRatio &&
                                               Math.Abs(a - b) > Tolerance;
#pragma warning restore 162
                                    })
                                    .ToArray();

                            if (deepCompareObjectFields.Count < 1)
                            {
                                continue;
                            }

                            if (!failures.TryGetValue(method, out var methodList))
                            {
                                failures.Add(method, methodList =
                                    new List<Tuple<ComputeShaderParameters, ComputeShaderParameters,
                                        IReadOnlyCollection<Tuple<string, float, float>>>>());
                            }

                            methodList.Add(
                                new Tuple<ComputeShaderParameters, ComputeShaderParameters,
                                    IReadOnlyCollection<Tuple<string, float, float>>>(
                                    cpu, gpu, deepCompareObjectFields));
                        }

                        // Continue until we have done enough loops, or run out of time.
                        durationTicks = Stopwatch.GetTimestamp() - startTicks;
                    } while (loops++ < TestLoops &&
                             durationTicks < TestDuration.Ticks);
                }
            }

            TimeSpan testDuration = TimeSpan.FromTicks(durationTicks);
            _output.WriteLine(
                $"Executed compute shader using {toolChain.GraphicsBackend} {loops} times in {testDuration.TotalSeconds}s.");

            int notIdential = 0;
            if (failures.Any())
            {
                _output.WriteLine($"{failures.Count} methods experienced failures out of {ShaderBuiltinsComputeTest.Methods} ({100f * failures.Count / ShaderBuiltinsComputeTest.Methods:##.##}%).  Details follow...");

                string spacer1 = new string('=', 80);
                string spacer2 = new string('-', 80);

                int failed = 0;

                // Output failures
                foreach (KeyValuePair<int, List<Tuple<ComputeShaderParameters, ComputeShaderParameters,
                    IReadOnlyCollection<Tuple<string, float, float>>>>> method in failures
                    .OrderBy(kvp => kvp.Key))
                // To order by %-age failure use - .OrderByDescending(kvp =>kvp.Value.Count))
                {
                    notIdential++;
                    int methodFailureCount = method.Value.Count;
                    _output.WriteLine(string.Empty);
                    _output.WriteLine(spacer1);
                    float failureRate = 100f * methodFailureCount / loops;
                    if (failureRate > MaximumFailureRate)
                    {
                        failed++;
                    }

                    _output.WriteLine(
                        $"Method {method.Key} failed {methodFailureCount} times ({failureRate:##.##}%).");


                    foreach (IGrouping<string, Tuple<string, float, float>> group in method.Value.SelectMany(t => t.Item3)
                        .ToLookup(f => f.Item1).OrderByDescending(g => g.Count()))
                    {
                        _output.WriteLine(spacer2);
                        _output.WriteLine(string.Empty);

                        int fieldFailureCount = group.Count();
                        _output.WriteLine($"  {group.Key} failed {fieldFailureCount} times ({100f * fieldFailureCount / methodFailureCount:##.##}%)");

                        int examples = 0;
                        foreach (Tuple<string, float, float> tuple in group)
                        {
                            if (examples++ > FailureExamples)
                            {
                                _output.WriteLine($"    ... +{fieldFailureCount - FailureExamples} more");
                                break;
                            }

                            _output.WriteLine($"    {tuple.Item2,13} != {tuple.Item3}");
                        }
                    }
                }

                Assert.False(failed < 1, $"{failed} methods had a failure rate higher than {MaximumFailureRate * 100:##.##}%!");
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine(notIdential < 1
                ? "CPU & CPU results were identical for all methods over all iterations!"
                : $"CPU & GPU results did not match for {notIdential} methods!");
        }

        public static IReadOnlyCollection<Tuple<string, object, object>> DeepCompareObjectFields<T>(T a, T b)
        {
            // Creat failures list
            List<Tuple<string, object, object>> failures = new List<Tuple<string, object, object>>();

            // Get dictionary of fields by field name and type
            Dictionary<Type, IReadOnlyCollection<FieldInfo>> childFieldInfos =
                new Dictionary<Type, IReadOnlyCollection<FieldInfo>>();

            Type currentType = typeof(T);
            object aValue = a;
            object bValue = b;
            Stack<Tuple<string, Type, object, object>> stack = new Stack<Tuple<string, Type, object, object>>();
            stack.Push(Tuple.Create(string.Empty, currentType, aValue, bValue));

            while (stack.Count > 0)
            {
                // Pop top of stack.
                Tuple<string, Type, object, object> tuple = stack.Pop();
                currentType = tuple.Item2;
                aValue = tuple.Item3;
                bValue = tuple.Item4;

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
                    string fullName = tuple.Item1;
                    failures.Add(Tuple.Create(fullName, aValue, bValue));
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

                    string fullName = string.IsNullOrWhiteSpace(tuple.Item1)
                        ? childField.Name
                        : $"{tuple.Item1}.{childField.Name}";
                    stack.Push(Tuple.Create(fullName, childField.FieldType, aMemberValue, bMemberValue));
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
        /// <returns></returns>
        public static unsafe T FillRandomFloats<T>() where T : struct
        {
            Random random = _randomGenerators.Value;
            int floatCount = Unsafe.SizeOf<T>() / sizeof(float);
            float* floats = stackalloc float[floatCount];
            for (int i = 0; i < floatCount; i++)
            {
                floats[i] = (float)(random.NextDouble() * FloatRange * 2f) - FloatRange;
            }

            return Unsafe.Read<T>(floats);
        }

        private class ShaderSetProcessor : IShaderSetProcessor
        {
            public string Result { get; private set; }

            public string UserArgs { get; set; }

            public void ProcessShaderSet(ShaderSetProcessorInput input)
            {
                Result = string.Join(" ", input.Model.AllResources.Select(rd => rd.Name));
            }
        }
    }
}