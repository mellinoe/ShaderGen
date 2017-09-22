using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace ShaderGen.Tests
{
    public class IShaderModelProcessorTests
    {
        [Fact]
        private void TestProcessor_UsersArgs()
        {
            Compilation compilation = TestUtil.GetTestProjectCompilation();
            HlslBackend backend = new HlslBackend(compilation);
            TestProcessor processor = new TestProcessor();
            ShaderGenerator sg = new ShaderGenerator(
                compilation,
                "TestShaders.ProcessorTestShaders.VS",
                "TestShaders.ProcessorTestShaders.FS",
                new[] { backend },
                new[] { processor });
            sg.GenerateShaders();
            Assert.Equal("This Sentence Should Be Printed By_Enumerating All Resources In Order", processor.Result);
        }

        private class TestProcessor : IShaderModelProcessor
        {
            public string Result { get; private set; }

            public string UserArgs { get; set; }

            public void ProcessShaderModel(ShaderModel model)
            {
                Result = string.Join(" ", model.Resources.Select(rd => rd.Name));
            }
        }
    }
}
