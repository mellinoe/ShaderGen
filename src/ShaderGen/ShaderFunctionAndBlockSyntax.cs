using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace ShaderGen
{
    public class ShaderFunctionAndBlockSyntax : IEquatable<ShaderFunctionAndBlockSyntax>
    {
        public ShaderFunction Function { get; }
        public BlockSyntax Block { get; }

        public ShaderFunctionAndBlockSyntax(ShaderFunction function, BlockSyntax block)
        {
            Function = function;
            Block = block;
        }

        public override string ToString() => Function.ToString();

        public bool Equals(ShaderFunctionAndBlockSyntax other)
        {
            return Function.DeclaringType == other.Function.DeclaringType
                && Function.Name == other.Function.Name;
        }
    }
}
