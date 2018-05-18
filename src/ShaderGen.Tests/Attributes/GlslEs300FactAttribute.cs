using ShaderGen.Glsl;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="FactAttribute"/> override will cause it to be skipped if
    /// the HLSL tool chain is unavailable.
    /// </summary>
    /// <seealso cref="Xunit.FactAttribute" />
    public sealed class GlslEs300FactAttribute : BackendFactAttribute
    {
        public GlslEs300FactAttribute() : base(typeof(GlslEs300Backend)) { }
    }
}