using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Marking a test with this <see cref="FactAttribute"/> override will cause it to be skipped if
    /// the Metal compilation tool is not available. 
    /// </summary>
    /// <seealso cref="Xunit.FactAttribute" />
    public sealed class MetalToolFactAttribute : ToolFactAttribute
    {
        public MetalToolFactAttribute() => RequiredTools = Tool.Metal;
    }
}
