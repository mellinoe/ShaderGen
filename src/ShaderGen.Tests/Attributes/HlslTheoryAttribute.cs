using ShaderGen.Hlsl;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="TheoryAttribute"/> override will cause it to be skipped if
    /// the HLSL tool chain is unavailable.
    /// </summary>
    /// <seealso cref="Xunit.TheoryAttribute" />
    public sealed class HlslTheoryAttribute : BackendTheoryAttribute
    {
        public HlslTheoryAttribute() : base(typeof(HlslBackend)) { }
    }
}