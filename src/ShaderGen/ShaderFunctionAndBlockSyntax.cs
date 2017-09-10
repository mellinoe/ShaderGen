using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderGen
{
    public class ShaderFunctionAndBlockSyntax
    {
        public ShaderFunction Function { get; }
        public BlockSyntax Block { get; }

        public ShaderFunctionAndBlockSyntax(ShaderFunction function, BlockSyntax block)
        {
            Function = function;
            Block = block;
        }

        public ShaderFunctionAndBlockSyntax WithReturnType(TypeReference returnType)
        {
            ShaderFunction sf = Function.WithReturnType(returnType);
            return new ShaderFunctionAndBlockSyntax(sf, Block);
        }

        public ShaderFunctionAndBlockSyntax WithParameter(int index, TypeReference type)
        {
            ShaderFunction sf = Function.WithParameter(index, type);
            return new ShaderFunctionAndBlockSyntax(sf, Block);
        }
    }
}
