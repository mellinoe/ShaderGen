using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace ShaderGen
{
    public class ShaderFunctionAndBlockSyntax : IEquatable<ShaderFunctionAndBlockSyntax>
    {
        public ShaderFunction Function { get; }
        public MethodDeclarationSyntax MethodDeclaration { get; }

        /// <summary>
        /// Only present for entry-point functions.
        /// </summary>
        public ShaderFunctionAndBlockSyntax[] OrderedFunctionList { get; }

        public ShaderFunctionAndBlockSyntax(ShaderFunction function, MethodDeclarationSyntax methodDeclaration, ShaderFunctionAndBlockSyntax[] orderedFunctionList)
        {
            Function = function;
            MethodDeclaration = methodDeclaration;
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
