using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics;
using System.IO;

namespace ShaderGen
{
    internal static class Extensions
    {
        public static string GetFullTypeName(this SemanticModel model, TypeSyntax type)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            TypeInfo typeInfo = model.GetTypeInfo(type);
            if (typeInfo.Type == null)
            {
                typeInfo = model.GetSpeculativeTypeInfo(0, type, SpeculativeBindingOption.BindAsTypeOrNamespace);
                if (typeInfo.Type == null || typeInfo.Type is IErrorTypeSymbol)
                {
                    throw new InvalidOperationException("Unable to resolve type: " + type + " at " + type.GetLocation());
                }
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
