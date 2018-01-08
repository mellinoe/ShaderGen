using System.Linq;
using System.Text;
using ShaderGen.Hlsl;

namespace ShaderGen
{
    internal class SwizzleTranslator : TypeInvocationTranslator
    {
        private readonly InvocationTranslator _translator = TranslateCore;

        public override bool GetTranslator(
            string method,
            InvocationParameterInfo[] parameters,
            out InvocationTranslator translator)
        {
            if (parameters.Length != 1 || method.Length < 2 || method.Length > 4 ||
                method.Any(c => (c != 'X' && c != 'Y' && c != 'Z' && c != 'W')))
            {
                translator = null;
                return false;
            }

            translator = _translator;
            return true;
        }

        private static string TranslateCore(string typeName, string methodName, InvocationParameterInfo[] parameters)
        {
            string target = parameters[0].Identifier;
            StringBuilder swizzle = new StringBuilder();
            foreach (char c in methodName)
            {
                swizzle.Append(char.ToLowerInvariant(c));
            }

            return $"{target}.{swizzle.ToString()}";
        }
    }
}
