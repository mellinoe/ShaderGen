using System;
using System.Collections.Generic;
using System.Text;

namespace ShaderGen
{
    public class ShaderSetProcessorInput
    {
        public string SetName { get; }
        public ShaderFunction VertexFunction { get; }
        public ShaderFunction FragmentFunction { get; }
        public ShaderModel Model { get; }

        public ShaderSetProcessorInput(
            string name,
            ShaderFunction vertexFunction,
            ShaderFunction fragmentFunction,
            ShaderModel model)
        {
            SetName = name;
            VertexFunction = vertexFunction;
            FragmentFunction = fragmentFunction;
            Model = model;
        }
    }
}
