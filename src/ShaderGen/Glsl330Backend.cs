using System;
using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    public class Glsl330Backend : LanguageBackend
    {
        internal Glsl330Backend(Compilation compilation) : base(compilation)
        {
        }

        protected override string CSharpToIdentifierNameCore(string typeName, string identifier)
        {
            throw new NotImplementedException();
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            throw new NotImplementedException();
        }

        protected override string FormatInvocationCore(string type, string method, InvocationParameterInfo[] parameterInfos)
        {
            throw new NotImplementedException();
        }

        protected override string GenerateFullTextCore(ShaderFunction function)
        {
            throw new NotImplementedException();
        }
    }
}
