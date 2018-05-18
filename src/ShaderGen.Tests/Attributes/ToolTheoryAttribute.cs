using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="FactAttribute"/> override will cause it to be skipped if
    /// the <see cref="RequiredTools"/> are not available.
    /// </summary>
    /// <seealso cref="Xunit.FactAttribute" />
    public class ToolTheoryAttribute : TheoryAttribute
    {
        public Tool RequiredTools;

        public override string Skip
        {
            get
            {
                if (base.Skip != null) return base.Skip;
                
                if (RequiredTools.HasFlag(Tool.Metal) && !MetalTool.IsAvailable)
                    return "Metal compilation is required to run this test.";
                if (RequiredTools.HasFlag(Tool.GlsLangValidator) && !GlsLangValidatorTool.IsAvailable)
                    return "GlsLangValidator is required to run this test.";
                if (RequiredTools.HasFlag(Tool.Fxc) && !FxcTool.IsAvailable)
                    return "Fxc is required to run this test.";
                return null;
            }
        }

        public ToolTheoryAttribute()
        {
        }
    }
}