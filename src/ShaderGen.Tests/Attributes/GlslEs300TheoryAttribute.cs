using ShaderGen.Glsl;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="TheoryAttribute"/> override will cause it to be skipped if
    /// the HLSL tool chain is unavailable.
    /// </summary>
    /// <seealso cref="Xunit.TheoryAttribute" />
    public sealed class GlslEs300TheoryAttribute : BackendTheoryAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlslEs300TheoryAttribute"/> class.
        /// </summary>
        public GlslEs300TheoryAttribute() : base(typeof(GlslEs300Backend)) { }
    }
}