using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ShaderGen
{
    public abstract class LanguageBackend
    {
        protected readonly Compilation Compilation;

        internal List<StructureDefinition> Structures { get; } = new List<StructureDefinition>();
        internal List<ResourceDefinition> Resources { get; } = new List<ResourceDefinition>();
        internal List<ShaderFunctionAndBlockSyntax> Functions { get; } = new List<ShaderFunctionAndBlockSyntax>();

        private readonly Dictionary<ShaderFunction, string> _fullTextShaders = new Dictionary<ShaderFunction, string>();

        internal LanguageBackend(Compilation compilation)
        {
            Compilation = compilation;
        }

        internal ShaderModel GetShaderModel()
        {
            return new ShaderModel(
                Structures.ToArray(),
                Resources.ToArray(),
                Functions.Select(sfabs => sfabs.Function).ToArray());
        }

        public string GetCode(ShaderFunction function)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }
            if (!function.IsEntryPoint)
            {
                throw new ArgumentException($"IsEntryPoint must be true for parameter {nameof(function)}");
            }

            if (!_fullTextShaders.TryGetValue(function, out string result))
            {
                result = GenerateFullTextCore(function);
                _fullTextShaders.Add(function, result);
            }

            return result;
        }

        internal string CSharpToShaderType(string fullType)
        {
            if (fullType == null)
            {
                throw new ArgumentNullException(nameof(fullType));
            }

            return CSharpToShaderTypeCore(fullType);
        }

        internal virtual void AddStructure(StructureDefinition sd)
        {
            if (sd == null)
            {
                throw new ArgumentNullException(nameof(sd));
            }

            Structures.Add(sd);
        }

        internal virtual void AddResource(ResourceDefinition ud)
        {
            if (ud == null)
            {
                throw new ArgumentNullException(nameof(ud));
            }

            Resources.Add(ud);
        }

        internal virtual void AddFunction(ShaderFunctionAndBlockSyntax sf)
        {
            if (sf == null)
            {
                throw new ArgumentNullException(nameof(sf));
            }

            Functions.Add(sf);
        }

        internal virtual string CSharpToShaderIdentifierName(SymbolInfo symbolInfo)
        {
            string typeName = symbolInfo.Symbol.ContainingType.ToDisplayString();
            string identifier = symbolInfo.Symbol.Name;

            return CorrectIdentifier(CSharpToIdentifierNameCore(typeName, identifier));
        }


        internal string FormatInvocation(string type, string method, InvocationParameterInfo[] parameterInfos)
        {
            Debug.Assert(type != null);
            Debug.Assert(method != null);
            Debug.Assert(parameterInfos != null);

            return FormatInvocationCore(type, method, parameterInfos);
        }

        internal abstract string CorrectIdentifier(string identifier);
        protected abstract string CSharpToShaderTypeCore(string fullType);
        protected abstract string CSharpToIdentifierNameCore(string typeName, string identifier);
        protected abstract string GenerateFullTextCore(ShaderFunction function);
        protected abstract string FormatInvocationCore(string type, string method, InvocationParameterInfo[] parameterInfos);

        internal string CorrectLiteral(string literal)
        {
            if (literal.EndsWith("f",  StringComparison.OrdinalIgnoreCase))
            {
                if (!literal.Contains("."))
                {
                    // This isn't a hack at all
                    return literal.Insert(literal.Length - 1, ".");
                }
            }

            return literal;
        }
    }
}
