using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ShaderGen
{
    internal static class Utilities
    {
        public static string GetFullTypeName(this SemanticModel model, ExpressionSyntax type)
        {
            bool _; return GetFullTypeName(model, type, out _);
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
                //sb.Insert(0, s.MetadataName);
                s = s.ContainingSymbol;
            }

            return sb.ToString();
        }

        private static bool IsRootNamespace(ISymbol symbol)
        {
            INamespaceSymbol s = null;
            return ((s = symbol as INamespaceSymbol) != null) && s.IsGlobalNamespace;
        }

        private static SemanticModel GetSemanticModel(Compilation compilation, SyntaxTree syntaxTree)
        {
            return compilation.GetSemanticModel(syntaxTree);
        }

        public static string GetFullName(INamespaceSymbol ns)
        {
            Debug.Assert(ns != null);
            string currentNamespace = ns.Name;
            if (ns.ContainingNamespace != null && !ns.ContainingNamespace.IsGlobalNamespace)
            {
                return GetFullName(ns.ContainingNamespace) + "." + currentNamespace;
            }
            else
            {
                return currentNamespace;
            }
        }

        public static string GetFullName(INamedTypeSymbol symbol)
        {
            Debug.Assert(symbol != null);
            string name = symbol.Name;
            if (symbol.ContainingNamespace != null && !symbol.ContainingNamespace.IsGlobalNamespace)
            {
                return GetFullName(symbol.ContainingNamespace) + "." + name;
            }
            else
            {
                return name;
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

        private static readonly HashSet<string> s_basicNumericTypes = new HashSet<string>()
        {
            "System.Numerics.Vector2",
            "System.Numerics.Vector3",
            "System.Numerics.Vector4",
            "System.Numerics.Matrix4x4",
        };

        public static bool IsBasicNumericType(string fullName)
        {
            return s_basicNumericTypes.Contains(fullName);
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
            Debug.Assert(symbolInfo.Symbol != null);
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

        internal static ShaderFunctionAndMethodDeclarationSyntax GetShaderFunction(
            MethodDeclarationSyntax node, 
            Compilation compilation,
            bool generateOrderedFunctionList)
        {
            string functionName = node.Identifier.ToFullString();
            List<ParameterDefinition> parameters = new List<ParameterDefinition>();
            foreach (ParameterSyntax ps in node.ParameterList.Parameters)
            {
                parameters.Add(ParameterDefinition.GetParameterDefinition(compilation, ps));
            }

            SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            TypeReference returnType = new TypeReference(semanticModel.GetFullTypeName(node.ReturnType), semanticModel.GetTypeInfo(node.ReturnType));

            UInt3 computeGroupCounts = new UInt3();
            bool isFragmentShader = false, isComputeShader = false;
            bool isVertexShader = GetMethodAttributes(node, "VertexShader").Any();
            if (!isVertexShader)
            {
                isFragmentShader = GetMethodAttributes(node, "FragmentShader").Any();
            }
            if (!isVertexShader && !isFragmentShader)
            {
                AttributeSyntax computeShaderAttr = GetMethodAttributes(node, "ComputeShader").FirstOrDefault();
                if (computeShaderAttr != null)
                {
                    isComputeShader = true;
                    computeGroupCounts.X = GetAttributeArgumentUIntValue(computeShaderAttr, 0);
                    computeGroupCounts.Y = GetAttributeArgumentUIntValue(computeShaderAttr, 1);
                    computeGroupCounts.Z = GetAttributeArgumentUIntValue(computeShaderAttr, 2);
                }
            }

            ShaderFunctionType type = isVertexShader
                ? ShaderFunctionType.VertexEntryPoint
                : isFragmentShader
                    ? ShaderFunctionType.FragmentEntryPoint
                    : isComputeShader
                        ? ShaderFunctionType.ComputeEntryPoint
                        : ShaderFunctionType.Normal;

            string nestedTypePrefix = GetFullNestedTypePrefix(node, out bool nested);
            ShaderFunction sf = new ShaderFunction(
                nestedTypePrefix,
                functionName,
                returnType,
                parameters.ToArray(),
                type,
                computeGroupCounts);

            ShaderFunctionAndMethodDeclarationSyntax[] orderedFunctionList;
            if (type != ShaderFunctionType.Normal && generateOrderedFunctionList)
            {
                FunctionCallGraphDiscoverer fcgd = new FunctionCallGraphDiscoverer(
                    compilation,
                    new TypeAndMethodName { TypeName = sf.DeclaringType, MethodName = sf.Name });
                fcgd.GenerateFullGraph();
                orderedFunctionList = fcgd.GetOrderedCallList();
            }
            else
            {
                orderedFunctionList = new ShaderFunctionAndMethodDeclarationSyntax[0];
            }

            return new ShaderFunctionAndMethodDeclarationSyntax(sf, node, orderedFunctionList);
        }

        private static uint GetAttributeArgumentUIntValue(AttributeSyntax attr, int index)
        {
            if (attr.ArgumentList.Arguments.Count < index + 1)
            {
                throw new ShaderGenerationException(
                    "Too few arguments in attribute " + attr.ToFullString() + ". Required + " + (index + 1));
            }
            string fullArg0 = attr.ArgumentList.Arguments[index].ToFullString();
            if (uint.TryParse(fullArg0, out uint ret))
            {
                return ret;
            }
            else
            {
                throw new ShaderGenerationException("Incorrectly formatted attribute: " + attr.ToFullString());
            }
        }
    }
}
