using ShaderGen.Glsl;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="FactAttribute"/> override will cause it to be skipped if
    /// the HLSL tool chain is unavailable.
    /// </summary>
    /// <seealso cref="Xunit.FactAttribute" />
    public sealed class Glsl450FactAttribute : BackendFactAttribute
    {
        public Glsl450FactAttribute() : base(typeof(Glsl450Backend)) { }
    }
}