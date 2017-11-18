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
        public string ComputeShaderCode { get; }
        public ShaderFunction VertexFunction { get; }
        public ShaderFunction FragmentFunction { get; }
        public ShaderFunction ComputeFunction { get; }
        public ShaderModel Model { get; }

        public GeneratedShaderSet(
            string name,
            string vsCode,
            string fsCode,
            string csCode,
            ShaderFunction vertexfunction,
            ShaderFunction fragmentFunction,
            ShaderFunction computeFunction,
            ShaderModel model)
        {
            if (string.IsNullOrEmpty(vsCode) && string.IsNullOrEmpty(fsCode) && string.IsNullOrEmpty(csCode))
            {
                throw new ShaderGenerationException("At least one of vsCode, fsCode, or csCode must be non-empty");
            }

            Name = name;
            VertexShaderCode = vsCode;
            FragmentShaderCode = fsCode;
            ComputeShaderCode = csCode;
            VertexFunction = vertexfunction;
            FragmentFunction = fragmentFunction;
            ComputeFunction = computeFunction;
            Model = model;
        }
    }
}
