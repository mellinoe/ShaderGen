using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using ShaderGen.Glsl;
using ShaderGen.Hlsl;
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

        [Glsl330Fact]
        public void TestShaderBuiltins_Glsl330()
            => TestShaderBuiltins(ToolChain.Glsl330);

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
             * Build test data
             */
            ComputeShaderParameters[] cpuParameters = new ComputeShaderParameters[ShaderBuiltinsComputeTest.Methods];
            Random random = new Random();

            int sizeOfParametersStruct = Unsafe.SizeOf<ComputeShaderParameters>();
            int writeSize = sizeOfParametersStruct - (int)ShaderBuiltinsComputeTest.FlagBytes; // Don't set flag bytes
            Parallel.For(0, ShaderBuiltinsComputeTest.Methods,
                i =>
                {
                    /*
                     * Fast code to initialise all but the output flags of the compute shader to random values
                     * TODO: .net core 2.1 Span<byte> can do this even quicker.
                     */
                    unsafe
                    {
                        // Create new empty parameters object an assign it to array.
                        cpuParameters[i] = new ComputeShaderParameters();

                        // This buffer holds a random number
                        byte[] buffer = new byte[4];
                        int pi = 0;

                        // Grab pointer to struct
                        ref byte asRefByte = ref Unsafe.As<ComputeShaderParameters, byte>(ref cpuParameters[i]);
                        fixed (byte* ptr = &asRefByte)
                            while (pi < writeSize)
                            {
                                int b = pi % 4;
                                if (b == 0)
                                    // Update random number in buffer every 4 bytes
                                    random.NextBytes(buffer);
                                *(ptr + pi++) = buffer[b];
                            }
                    }
                });

            // Clone parameters for GPU
            ComputeShaderParameters[] gpuParameters = new ComputeShaderParameters[cpuParameters.Length];
            Array.Copy(cpuParameters, gpuParameters, cpuParameters.Length);

            _output.WriteLine($"Generated random parameters for {ShaderBuiltinsComputeTest.Methods} methods.");

            /*
             * Run shader on CPU.
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
            WindowCreateInfo windowCreateInfo = new WindowCreateInfo()
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
                _output.WriteLine($"Created graphics device using {toolChain.GraphicsBackend}.");

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
                    _output.WriteLine("Executed compute shaders.");

                    // Read back parameters
                    MappedResourceView<ComputeShaderParameters> map = graphicsDevice.Map<ComputeShaderParameters>(inOutBuffer, MapMode.Write);
                    for (int i = 0; i < gpuParameters.Length; i++)
                        gpuParameters[i] = map[i];
                    graphicsDevice.Unmap(inOutBuffer);
                    graphicsDevice.WaitForIdle();
                }
            }
        }

        public static float RandomFloat(Random randomGenerator)
        {
            byte[] bytes = new byte[4];
            randomGenerator.NextBytes(bytes);
            return BitConverter.ToSingle(bytes, 0);
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