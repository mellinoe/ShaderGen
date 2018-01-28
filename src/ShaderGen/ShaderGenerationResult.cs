using System;
using System.Collections.Generic;

namespace ShaderGen
{
    public class ShaderGenerationResult
    {
        private readonly Dictionary<LanguageBackend, List<GeneratedShaderSet>> _generatedShaders
            = new Dictionary<LanguageBackend, List<GeneratedShaderSet>>();

        public IReadOnlyList<GeneratedShaderSet> GetOutput(LanguageBackend backend)
        {
            if (_generatedShaders.Count == 0)
            {
                return Array.Empty<GeneratedShaderSet>();
            }

            if (!_generatedShaders.TryGetValue(backend, out var list))
            {
                throw new InvalidOperationException($"The backend {backend} was not used to generate shaders for this object.");
            }

            return list;
        }

        internal void AddShaderSet(LanguageBackend backend, GeneratedShaderSet gss)
        {
            if (!_generatedShaders.TryGetValue(backend, out var list))
            {
                list = new List<GeneratedShaderSet>();
                _generatedShaders.Add(backend, list);
            }

            list.Add(gss);
        }
    }
}
