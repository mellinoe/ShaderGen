using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using ShaderGen.Tests.Attributes;
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
        private readonly ITestOutputHelper _output;

        public ShaderBuiltinsTests(ITestOutputHelper output)
        {
            _output = output;
        }


        [GlslEs300Fact]
        public void TestShaderBuiltins_GlslEs300()
            => TestShaderBuiltins(ToolChain.GlslEs300);

        [Glsl330Fact]
        public void TestShaderBuiltins_Glsl330()
            => TestShaderBuiltins(ToolChain.Glsl330);

        [Glsl450Fact]
        public void TestShaderBuiltins_Glsl450()
            => TestShaderBuiltins(ToolChain.Glsl450);

        [HlslFact]
        public void TestShaderBuiltins_Hlsl()
            => TestShaderBuiltins(ToolChain.Hlsl);

        [MetalFact]
        public void TestShaderBuiltins_Metal()
            => TestShaderBuiltins(ToolChain.Metal);

        private void TestShaderBuiltins(ToolChain toolChain)
        {
            string csFunctionName =
                $"{nameof(TestShaders)}.{nameof(ShaderBuiltinsComputeTest)}.{nameof(ShaderBuiltinsComputeTest.CS)}";
            Compilation compilation = TestUtil.GetTestProjectCompilation();

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

            ToolResult compilationResult =
                toolChain.Compile(set.ComputeShaderCode, Stage.Compute, set.ComputeFunction.Name);
            if (compilationResult.HasError)
            {
                _output.WriteLine($"Failed to compile Compute Shader from set \"{set.Name}\"!");
                _output.WriteLine(compilationResult.ToString());
                Assert.True(false);
            }
            else
                _output.WriteLine($"Compiled Compute Shader from set \"{set.Name}\"!");

            /*
             * Build test data in parallel
             */
            // We need two copies, one for the CPU & one for GPU
            ComputeShaderParameters[] cpuParameters = new ComputeShaderParameters[ShaderBuiltinsComputeTest.Methods];
            ComputeShaderParameters[] gpuParameters = new ComputeShaderParameters[ShaderBuiltinsComputeTest.Methods];

            int sizeOfParametersStruct = Unsafe.SizeOf<ComputeShaderParameters>();
            Parallel.For(
                0,
                ShaderBuiltinsComputeTest.Methods,
                i => cpuParameters[i] = gpuParameters[i] = GetRandom<ComputeShaderParameters>());

            _output.WriteLine($"Generated random parameters for {ShaderBuiltinsComputeTest.Methods} methods.");

            /*
             * Run shader on CPU in parallel
             */
            ShaderBuiltinsComputeTest cpuTest = new ShaderBuiltinsComputeTest
            {
                InOutParameters = new RWStructuredBuffer<ComputeShaderParameters>(ref cpuParameters)
            };

            Parallel.For(0, ShaderBuiltinsComputeTest.Methods,
                i => cpuTest.DoCS(new UInt3 { X = (uint)i, Y = 0, Z = 0 }));

            /*
             * Run shader on GPU.
             */
            WindowCreateInfo windowCreateInfo = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = $"Test Shader Builtins using {toolChain}",
                WindowInitialState = WindowState.Hidden
            };
            Sdl2Window window = VeldridStartup.CreateWindow(ref windowCreateInfo);
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                true,
                PixelFormat.R16_UNorm,
                true,
                ResourceBindingModel.Improved);
            using (GraphicsDevice graphicsDevice =
                VeldridStartup.CreateGraphicsDevice(window, options, toolChain.GraphicsBackend))
            {
                ResourceFactory factory = graphicsDevice.ResourceFactory;
                using (DeviceBuffer inOutBuffer = factory.CreateBuffer(
                    new BufferDescription(
                        (uint)sizeOfParametersStruct * ShaderBuiltinsComputeTest.Methods,
                        BufferUsage.StructuredBufferReadWrite | BufferUsage.Dynamic,
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
                    _output.WriteLine("Created compute pipeline.");

                    Assert.True(window.Exists, "The graphics device window does not exist!");
                    window.PumpEvents();

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

                    // Read back parameters
                    MappedResourceView<ComputeShaderParameters> map = graphicsDevice.Map<ComputeShaderParameters>(inOutBuffer, MapMode.Write);
                    for (int i = 0; i < gpuParameters.Length; i++)
                        gpuParameters[i] = map[i];
                    graphicsDevice.Unmap(inOutBuffer);
                    graphicsDevice.WaitForIdle();
                }
            }

            _output.WriteLine($"Executed compute shader using {toolChain.GraphicsBackend}.");

            /*
             * Compare results
             */

            // Get dictionary of parameter fields
            Dictionary<string, FieldInfo> fieldInfos = typeof(ComputeShaderParameters).GetFields().ToDictionary(f => f.Name);
            bool failed = false;
            for (int i = 0; i < ShaderBuiltinsComputeTest.Methods; i++)
            {
                var cpu = cpuParameters[i];
                var gpu = gpuParameters[i];

                foreach (var kvp in fieldInfos)
                {
                    object aValue = kvp.Value.GetValue(cpu);
                    object bValue = kvp.Value.GetValue(gpu);
                    if (Equals(aValue, bValue)) continue;

                    _output.WriteLine($"Method {i} inconsistent: CPU.{kvp.Key} '{aValue}' != GPU.{kvp.Key} '{bValue}'");
                    failed = true;
                }
            }

            Assert.False(failed, "GPU and CPU results were not identical!");

            _output.WriteLine("CPU & CPU results were identical for all methods!");
        }

        /// <summary>
        /// The random number generators for each thread.
        /// </summary>
        private static readonly ThreadLocal<Random> _randomGenerators =
            new ThreadLocal<Random>(() => new Random());

        /// <summary>
        /// Create a type with random data.
        /// </summary>
        /// <typeparam name="T">The random type</typeparam>
        /// <param name="size">The optional number of bytes to fill.</param>
        /// <returns></returns>
        public unsafe T GetRandom<T>(int size = default(int))
        {
            Random random = _randomGenerators.Value;
            size = Math.Min(Unsafe.SizeOf<T>(), size < 1 ? Int32.MaxValue : size);
            T result = Activator.CreateInstance<T>();
            // This buffer holds a random number
            byte[] buffer = new byte[4];
            int pi = 0;

            // Grab pointer to struct
            ref byte asRefByte = ref Unsafe.As<T, byte>(ref result);
            fixed (byte* ptr = &asRefByte)
                while (pi < size)
                {
                    int b = pi % 4;
                    if (b == 0)
                        // Update random number in buffer every 4 bytes
                        random.NextBytes(buffer);
                    *(ptr + pi++) = buffer[b];
                }

            return result;
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