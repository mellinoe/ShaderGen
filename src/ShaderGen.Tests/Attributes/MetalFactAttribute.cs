using ShaderGen.Metal;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="FactAttribute"/> override will cause it to be skipped if
    /// the HLSL tool chain is unavailable.
    /// </summary>
    /// <seealso cref="Xunit.FactAttribute" />
    public sealed class MetalFactAttribute : BackendFactAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetalFactAttribute"/> class.
        /// </summary>
        public MetalFactAttribute() : base(typeof(MetalBackend)) { }
    }
}