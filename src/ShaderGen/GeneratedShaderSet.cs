using System;
using System.Collections.Generic;
using System.Text;

namespace ShaderGen
{
    /// <summary>
    /// Represents the concrete output of a shader generation for a specific shader set,
    /// created using a specific LanguageBackend.
    /// </summary>
    public class GeneratedShaderSet
    {
        public string Name { get; }
        public string VertexShaderCode { get; }
        public string FragmentShaderCode { get; }
        public ShaderModel Model { get; }

        public GeneratedShaderSet(string name, string vsCode, string fsCode, ShaderModel model)
        {
            Name = name;
            VertexShaderCode = vsCode;
            FragmentShaderCode = fsCode;
            Model = model;
        }
    }
}
