using Newtonsoft.Json;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="FactAttribute"/> override will cause it to be skipped if
    /// the Fxc compilation tool is not available. 
    /// </summary>
    /// <seealso cref="Xunit.FactAttribute" />
    public sealed class FxcToolFactAttribute : ToolFactAttribute
    {
        public FxcToolFactAttribute() => RequiredTools = Tool.Fxc;
    }
}