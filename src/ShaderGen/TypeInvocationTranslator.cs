using ShaderGen.Hlsl;

namespace ShaderGen
{
    internal abstract class TypeInvocationTranslator
    {
        public abstract bool GetTranslator(
            string method,
            InvocationParameterInfo[] parameters,
            out InvocationTranslator translator);
    }
}
