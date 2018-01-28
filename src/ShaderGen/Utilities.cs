﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShaderGen
{
    internal static class Utilities
    {
        public static string GetFullTypeName(this SemanticModel model, ExpressionSyntax type)
        {
            return GetFullTypeName(model, type, out bool _);
        }

        public static string GetFullTypeName(this SemanticModel model, ExpressionSyntax type, out bool isArray)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (type.SyntaxTree != model.SyntaxTree)
            {
                model = GetSemanticModel(model.Compilation, type.SyntaxTree);
            }

            TypeInfo typeInfo = model.GetTypeInfo(type);
            if (typeInfo.Type == null)
            {
                typeInfo = model.GetSpeculativeTypeInfo(0, type, SpeculativeBindingOption.BindAsTypeOrNamespace);
                if (typeInfo.Type == null || typeInfo.Type is IErrorTypeSymbol)
                {
                    throw new InvalidOperationException("Unable to resolve type: " + type + " at " + type.GetLocation());
                }
            }

            return GetFullTypeName(typeInfo.Type, out isArray);
        }

        public static string GetFullTypeName(ITypeSymbol type, out bool isArray)
        {
            if (type is IArrayTypeSymbol ats)
            {
                isArray = true;
                return GetFullMetadataName(ats.ElementType);
            }
            else
            {
                isArray = false;
                return GetFullMetadataName(type);
            }
        }

        public static string GetFullMetadataName(this ISymbol s)
        {
            if (s == null || IsRootNamespace(s))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(s.MetadataName);
            ISymbol last = s;

            s = s.ContainingSymbol;

            while (!IsRootNamespace(s))
            {
                if (s is ITypeSymbol && last is ITypeSymbol)
                {
                    sb.Insert(0, '+');
                }
                else
                {
                    sb.Insert(0, '.');
                }

                sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                s = s.ContainingSymbol;
            }

            return sb.ToString();
        }

        private static bool IsRootNamespace(ISymbol symbol)
        {
            return symbol is INamespaceSymbol ns && ns.IsGlobalNamespace;
        }

        private static SemanticModel GetSemanticModel(Compilation compilation, SyntaxTree syntaxTree)
        {
            return compilation.GetSemanticModel(syntaxTree);
        }

        public static string GetFullName(INamespaceSymbol ns)
        {
            string currentNamespace = ns.Name;
            if (ns.ContainingNamespace != null && !ns.ContainingNamespace.IsGlobalNamespace)
            {
                return GetFullName(ns.ContainingNamespace) + "." + currentNamespace;
            }
            return currentNamespace;
        }

        public static string GetFullName(INamedTypeSymbol symbol)
        {
            string name = symbol.Name;
            if (symbol.ContainingNamespace != null && !symbol.ContainingNamespace.IsGlobalNamespace)
            {
                return GetFullName(symbol.ContainingNamespace) + "." + name;
            }
            return name;
        }

        public static string GetFullNamespace(SyntaxNode node)
        {
            if (!SyntaxNodeHelper.TryGetParentSyntax(node, out NamespaceDeclarationSyntax namespaceDeclarationSyntax))
            {
                return string.Empty; // or whatever you want to do in this scenario
            }
            return namespaceDeclarationSyntax.Name.ToString();
        }

        public static string GetFullNestedTypePrefix(SyntaxNode node, out bool nested)
        {
            string ns = GetFullNamespace(node);
            List<string> nestedTypeParts = new List<string>();
            while (SyntaxNodeHelper.TryGetParentSyntax(node, out ClassDeclarationSyntax cds))
            {
                nestedTypeParts.Add(cds.Identifier.ToFullString().Trim());
                node = cds;
            }

            string nestedTypeStr = string.Join("+", nestedTypeParts);
            if (string.IsNullOrEmpty(ns))
            {
                nested = true;
                return nestedTypeStr;
            }
            else
            {
                if (string.IsNullOrEmpty(nestedTypeStr))
                {
                    nested = false;
                    return ns;
                }
                else
                {
                    nested = true;
                    return ns + "." + nestedTypeStr;
                }
            }
        }

        public static AttributeSyntax[] GetMemberAttributes(CSharpSyntaxNode vds, string name)
        {
            return vds.Parent.Parent.DescendantNodes().OfType<AttributeSyntax>()
                .Where(attrSyntax => attrSyntax.Name.ToString().Contains(name)).ToArray();
        }

        public static AttributeSyntax[] GetMethodAttributes(MethodDeclarationSyntax mds, string name)
        {
            return mds.DescendantNodes().OfType<AttributeSyntax>()
            .Where(attrSyntax => attrSyntax.Name.ToString().Contains(name)).ToArray();
        }

        /// <summary>
        /// Gets the full namespace + name for the given SymbolInfo.
        /// </summary>
        public static string GetFullName(SymbolInfo symbolInfo)
        {
            string fullName = symbolInfo.Symbol.Name;
            string ns = GetFullName(symbolInfo.Symbol.ContainingNamespace);
            if (!string.IsNullOrEmpty(ns))
            {
                fullName = ns + "." + fullName;
            }

            return fullName;
        }

        internal static string JoinIgnoreNull(string separator, IEnumerable<string> value)
        {
            return string.Join(separator, value.Where(s => !string.IsNullOrEmpty(s)));
        }
    }
}
