using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace ShaderGen
{
    internal static class Extensions
    {
        public static string GetFullTypeName(this TransformationContext context, TypeSyntax type)
        {
            TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(type);
            string ns = GetFullNamespace(typeInfo.Type.ContainingNamespace);
            return ns + "." + typeInfo.Type.Name;
        }

        public static string GetFullNamespace(INamespaceSymbol ns)
        {
            Debug.Assert(ns != null);
            string currentNamespace = ns.Name;
            if (ns.ContainingNamespace != null && !ns.ContainingNamespace.IsGlobalNamespace)
            {
                return GetFullNamespace(ns.ContainingNamespace) + "." + currentNamespace;
            }
            else
            {
                return currentNamespace;
            }
        }
    }
}
