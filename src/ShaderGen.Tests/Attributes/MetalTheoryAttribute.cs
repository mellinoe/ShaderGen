using ShaderGen.Metal;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="TheoryAttribute"/> override will cause it to be skipped if
    /// the HLSL tool chain is unavailable.
    /// </summary>
    /// <seealso cref="Xunit.TheoryAttribute" />
    public sealed class MetalTheoryAttribute : BackendTheoryAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetalTheoryAttribute"/> class.
        /// </summary>
        public MetalTheoryAttribute() : base(typeof(MetalBackend)) { }
    }
}