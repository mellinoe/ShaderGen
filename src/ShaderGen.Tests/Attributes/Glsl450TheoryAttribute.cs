using ShaderGen.Glsl;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="TheoryAttribute"/> override will cause it to be skipped if
    /// the HLSL tool chain is unavailable.
    /// </summary>
    /// <seealso cref="Xunit.TheoryAttribute" />
    public sealed class Glsl450TheoryAttribute : BackendTheoryAttribute
    {
        public Glsl450TheoryAttribute() : base(typeof(Glsl450Backend)) { }
    }
}