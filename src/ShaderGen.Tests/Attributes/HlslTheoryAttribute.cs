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
        /// <summary>
        /// Initializes a new instance of the <see cref="HlslTheoryAttribute"/> class.
        /// </summary>
        /// <param name="requireHeadless">if set to <c>true</c> requires headless graphics device.</param>
        public HlslTheoryAttribute(bool requireHeadless = false) : base(typeof(HlslBackend)) { }
    }
}