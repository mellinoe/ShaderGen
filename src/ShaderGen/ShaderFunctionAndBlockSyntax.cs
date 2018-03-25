using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace ShaderGen
{
    public class ShaderFunctionAndBlockSyntax : IEquatable<ShaderFunctionAndBlockSyntax>
    {
        public ShaderFunction Function { get; }
        public BlockSyntax Block { get; }

        /// <summary>
        /// Only present for entry-point functions.
        /// </summary>
        public ShaderFunctionAndBlockSyntax[] OrderedFunctionList { get; }

        public ShaderFunctionAndBlockSyntax(ShaderFunction function, BlockSyntax block, ShaderFunctionAndBlockSyntax[] orderedFunctionList)
        {
            Function = function;
            Block = block;
            OrderedFunctionList = orderedFunctionList;
        }

        public override string ToString() => Function.ToString();

        public bool Equals(ShaderFunctionAndBlockSyntax other)
        {
            return Function.DeclaringType == other.Function.DeclaringType
                && Function.Name == other.Function.Name;
        }
    }
}
