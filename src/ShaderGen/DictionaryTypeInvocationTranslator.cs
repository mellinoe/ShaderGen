using System.Collections.Generic;
using ShaderGen.Hlsl;

namespace ShaderGen
{
    internal class DictionaryTypeInvocationTranslator : TypeInvocationTranslator
    {
        private readonly Dictionary<string, InvocationTranslator> _translators;

        public DictionaryTypeInvocationTranslator(Dictionary<string, InvocationTranslator> translators)
        {
            _translators = translators;
        }

        public override bool GetTranslator(
            string method,
            InvocationParameterInfo[] parameters,
            out InvocationTranslator translator)
        {
            return _translators.TryGetValue(method, out translator);
        }
    }
}
