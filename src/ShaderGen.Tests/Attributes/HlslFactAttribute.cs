using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using ShaderGen.Hlsl;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="FactAttribute"/> override will cause it to be skipped if
    /// the HLSL tool chain is unavailable.
    /// </summary>
    /// <seealso cref="Xunit.FactAttribute" />
    public sealed class HlslFactAttribute : BackendFactAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HlslFactAttribute"/> class.
        /// </summary>
        public HlslFactAttribute() : base(typeof(HlslBackend)) { }
    }
}