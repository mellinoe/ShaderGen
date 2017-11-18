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
    }
}
