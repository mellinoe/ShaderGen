using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="FactAttribute"/> override will cause it to be skipped if
    /// the <see cref="RequiredTools"/> are not available.
    /// </summary>
    /// <seealso cref="Xunit.FactAttribute" />
    public class ToolFactAttribute : FactAttribute
    {
        public Tool RequiredTools = Tool.None;
        
        private readonly string _toolSkip;

        public override string Skip => base.Skip ?? _toolSkip;

        public ToolFactAttribute()
        {
            if (RequiredTools.HasFlag(Tool.Metal) && !MetalTool.IsAvailable)
                _toolSkip = "Metal compilation is required to run this test.";
            else if (RequiredTools.HasFlag(Tool.GlsLangValidator) && !GlsLangValidatorTool.IsAvailable)
                _toolSkip = "GlsLangValidator is required to run this test.";
            else if (RequiredTools.HasFlag(Tool.Fxc) && !FxcTool.IsAvailable)
                _toolSkip = "Fxc is required to run this test.";
        }
    }
}