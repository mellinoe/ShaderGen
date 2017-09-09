using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderGen
{
    public class ShaderFunctionAndBlockSyntax
    {
        public ShaderFunction Function { get; }
        public BlockSyntax BlockSyntax { get; }

        public ShaderFunctionAndBlockSyntax(ShaderFunction function, BlockSyntax block)
        {
            Function = function;
            BlockSyntax = block;
        }

        public ShaderFunctionAndBlockSyntax WithReturnType(TypeReference returnType)
        {
            ShaderFunction sf = Function.WithReturnType(returnType);
            return new ShaderFunctionAndBlockSyntax(sf, BlockSyntax);
        }
    }
}
