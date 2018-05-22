using ShaderGen.Glsl;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="FactAttribute"/> override will cause it to be skipped if
    /// the HLSL tool chain is unavailable.
    /// </summary>
    /// <seealso cref="Xunit.FactAttribute" />
    public sealed class Glsl330FactAttribute : BackendFactAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Glsl330FactAttribute"/> class.
        /// </summary>
        /// <param name="requireHeadless">if set to <c>true</c> requires headless graphics device.</param>
        public Glsl330FactAttribute(bool requireHeadless = false) : base(requireHeadless, typeof(Glsl330Backend)) { }
    }
}