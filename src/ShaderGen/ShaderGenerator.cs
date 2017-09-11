using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace ShaderGen
{
    public class ShaderGenerator
    {
        private readonly Compilation _compilation;
        private readonly string _vertexFunctionName;
        private readonly string _fragmentFunctionName;
        private readonly List<LanguageBackend> _languages;

        public ShaderGenerator(
            Compilation compilation,
            string vertexFunctionName,
            string fragmentFunctionName,
            params LanguageBackend[] languages)
        {
            _compilation = compilation;
            _languages = new List<LanguageBackend>(languages);
            _vertexFunctionName = vertexFunctionName;
            _fragmentFunctionName = fragmentFunctionName;
        }

        public ShaderModel GenerateShaders()
        {
            foreach (LanguageBackend backend in _languages)
            {
            }

            throw new NotImplementedException();
        }
    }
}
