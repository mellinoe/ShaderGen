using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics;
using System.IO;

namespace ShaderGen
{
    internal static class Extensions
    {
        public static string GetFullTypeName(this TransformationContext context, TypeSyntax type)
        {
            if (context == null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            if (type == null)
            {
                throw new System.ArgumentNullException(nameof(type));
            }

            TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(type);
            if (typeInfo.Type == null)
            {
                typeInfo = context.SemanticModel.GetSpeculativeTypeInfo(0, type, SpeculativeBindingOption.BindAsTypeOrNamespace);
                if (typeInfo.Type != null)
                {
                    return type.ToFullString();
                }
            }

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

        public static string GetFullNamespace(SyntaxNode node)
        {
            if (!SyntaxNodeHelper.TryGetParentSyntax(node, out NamespaceDeclarationSyntax namespaceDeclarationSyntax))
            {
                return string.Empty; // or whatever you want to do in this scenario
            }

            string namespaceName = namespaceDeclarationSyntax.Name.ToString();
            return namespaceName;
        }
    }
}
