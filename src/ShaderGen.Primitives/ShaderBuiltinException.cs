using System;
using System.Runtime.CompilerServices;

namespace ShaderGen
{
    public class ShaderBuiltinException : Exception
    {
        internal ShaderBuiltinException([CallerMemberName] string memberName = null)
            : base($"{nameof(ShaderBuiltins)}.{memberName} can only be executed on the GPU.")
        {
        }
    }
}