using System.Linq;

namespace ShaderGen
{
    public struct InvocationParameterInfo
    {
        public string FullTypeName;
        public string Identifier;

        public static string GetInvocationParameterList(InvocationParameterInfo[] parameterInfos)
        {
            return string.Join(", ", parameterInfos.Select(pi => pi.Identifier));
        }
    }
}
