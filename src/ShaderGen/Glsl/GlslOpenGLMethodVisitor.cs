﻿using System.Linq;
using Microsoft.CodeAnalysis;

namespace ShaderGen.Glsl
{
    internal class GlslOpenGLMethodVisitor : ShaderMethodVisitor
    {
        public GlslOpenGLMethodVisitor(Compilation compilation, string setName, ShaderFunction shaderFunction, LanguageBackend backend)
            : base(compilation, setName, shaderFunction, backend)
        {
        }

        protected override string FormatParameter(ParameterDefinition pd)
        {
            string parameterType;

            if (_backend.GetContext(_setName).Resources.Any(x => x.IsTextureUsedAsDepthTexture && x.ParameterSymbols.Contains(pd.Symbol)))
            {
                switch (pd.Type.Name)
                {
                    case "ShaderGen.Texture2DResource":
                        parameterType = "sampler2DShadow";
                        break;

                    case "ShaderGen.Texture2DArrayResource":
                        parameterType = "sampler2DArrayShadow";
                        break;

                    default:
                        throw new ShaderGenerationException($"Unexpected texture parameter type: {pd.Type.Name}.");
                }
            }
            else
            {
                parameterType = _backend.CSharpToShaderType(pd.Type);
            }

            return $"{_backend.ParameterDirection(pd.Direction)} {parameterType} {_backend.CorrectIdentifier(pd.Name)}";
        }
    }
}
