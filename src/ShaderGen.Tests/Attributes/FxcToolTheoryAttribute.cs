using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="TheoryAttribute"/> override will cause it to be skipped if
    /// the Fxc compilation tool is not available. 
    /// </summary>
    /// <seealso cref="Xunit.TheoryAttribute" />
    public sealed class FxcToolTheoryAttribute : ToolTheoryAttribute
    {
        public FxcToolTheoryAttribute() => RequiredTools = Tool.Fxc;
    }
}