using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace ShaderGen
{
    public class ShaderFunctionAndMethodDeclarationSyntax : IEquatable<ShaderFunctionAndMethodDeclarationSyntax>
    {
        public ShaderFunction Function { get; }
        public MethodDeclarationSyntax MethodDeclaration { get; }

        /// <summary>
        /// Only present for entry-point functions.
        /// </summary>
        public ShaderFunctionAndMethodDeclarationSyntax[] OrderedFunctionList { get; }

        public ShaderFunctionAndMethodDeclarationSyntax(ShaderFunction function, MethodDeclarationSyntax methodDeclaration, ShaderFunctionAndMethodDeclarationSyntax[] orderedFunctionList)
        {
            Function = function;
            MethodDeclaration = methodDeclaration;
            OrderedFunctionList = orderedFunctionList;
        }

        public override string ToString() => Function.ToString();

        public bool Equals(ShaderFunctionAndMethodDeclarationSyntax other)
        {
            return Function.DeclaringType == other.Function.DeclaringType
                && Function.Name == other.Function.Name;
        }

        public override int GetHashCode()
        {
            var hashCode = 1204124163;
            hashCode = hashCode * -1521134295 + Function.DeclaringType.GetHashCode();
            hashCode = hashCode * -1521134295 + Function.Name.GetHashCode();
            return hashCode;
        }
    }
}
