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
        /// Controls the minimum mantissa when generating a floating point number (how 'small' it can go)
        /// </summary>
        /// <remarks>To test all valid floats this should be set to -126.</remarks>
        private static readonly int MinMantissa = -3;

        /// <summary>
        /// Controls the maximum mantissa when generating a floating point number (how 'big' it can go)
        /// </summary>
        /// <remarks>To test all valid floats this should be set to 128.</remarks>
        private static readonly int MaxMantissa = 3;

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
            Dictionary<int, List<(ComputeShaderParameters cpu, ComputeShaderParameters gp,
                IReadOnlyCollection<(string fieldName, float cpuValue, float gpuValue)> differences)>> failures
                = new Dictionary<int, List<(ComputeShaderParameters cpu, ComputeShaderParameters gp,
                    IReadOnlyCollection<(string fieldName, float cpuValue, float gpuValue)> differences)>>();

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
                if (!graphicsDevice.Features.ComputeShader)
                {
                    throw new RequiredToolFeatureMissingException(
                        $"The {graphicsDevice.BackendType} backend does not support compute shaders!");
                }

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
                            i => cpuParameters[i] = gpuParameters[i] = TestUtil.FillRandomFloats<ComputeShaderParameters>(MinMantissa, MaxMantissa));

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
                            IReadOnlyCollection<(string fieldName, float cpuValue, float gpuValue)>
                                deepCompareObjectFields = TestUtil.DeepCompareObjectFields(cpu, gpu)
                                        .Select<(string fieldName, object aValue, object bValue), (string fieldName,
                                            float cpuValue, float gpuValue)>(
                                            t => (t.fieldName, (float)t.aValue, (float)t.bValue))
                                        .Where(t =>
                                        {
#pragma warning disable 162
                                            // ReSharper disable ConditionIsAlwaysTrueOrFalse
                                            float a = t.cpuValue;
                                            float b = t.gpuValue;
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
                                            // ReSharper restore ConditionIsAlwaysTrueOrFalse
                                        })
                                        .ToArray();

                            if (deepCompareObjectFields.Count < 1)
                            {
                                continue;
                            }

                            if (!failures.TryGetValue(method, out var methodList))
                            {
                                failures.Add(method,
                                    methodList =
                                        new List<(ComputeShaderParameters cpu, ComputeShaderParameters gp,
                                            IReadOnlyCollection<(string fieldName, float cpuValue, float gpuValue)>)>());
                            }

                            methodList.Add((cpu, gpu, deepCompareObjectFields));
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

                int failed = 0;

                // Output failures
                foreach (var method in failures.OrderBy(kvp => kvp.Key))
                // To order by %-age failure use - .OrderByDescending(kvp =>kvp.Value.Count))
                {
                    notIdential++;
                    int methodFailureCount = method.Value.Count;
                    _output.WriteLine(string.Empty);
                    _output.WriteLine(TestUtil.Spacer1);
                    float failureRate = 100f * methodFailureCount / loops;
                    if (failureRate > MaximumFailureRate)
                    {
                        failed++;
                    }

                    _output.WriteLine(
                        $"Method {method.Key} failed {methodFailureCount} times ({failureRate:##.##}%).");


                    foreach (var group in method.Value.SelectMany(t => t.differences).ToLookup(f => f.fieldName).OrderByDescending(g => g.Count()))
                    {
                        _output.WriteLine(TestUtil.Spacer2);
                        _output.WriteLine(string.Empty);

                        int fieldFailureCount = group.Count();
                        _output.WriteLine($"  {group.Key} failed {fieldFailureCount} times ({100f * fieldFailureCount / methodFailureCount:##.##}%)");

                        int examples = 0;
                        foreach (var tuple in group)
                        {
                            if (examples++ > FailureExamples)
                            {
                                _output.WriteLine($"    ... +{fieldFailureCount - FailureExamples} more");
                                break;
                            }

                            _output.WriteLine($"    {tuple.cpuValue,13} != {tuple.gpuValue}");
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