using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace ShaderGen
{
    public class ShaderGenerator
    {
        private readonly Compilation _compilation;
        private readonly TypeAndMethodName _vertexFunctionName;
        private readonly TypeAndMethodName _fragmentFunctionName;
        private readonly List<LanguageBackend> _languages;

        public ShaderGenerator(
            Compilation compilation,
            string vertexFunctionName,
            string fragmentFunctionName,
            params LanguageBackend[] languages)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (vertexFunctionName == null && fragmentFunctionName == null)
            {
                throw new ArgumentException(
                    $"One of {nameof(vertexFunctionName)} or {nameof(fragmentFunctionName)} must be non-null.");
            }
            if (languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
            }
            if (languages.Length == 0)
            {
                throw new ArgumentException("At least one LanguageBackend must be provided.");
            }

            _compilation = compilation;
            _languages = new List<LanguageBackend>(languages);
            if (vertexFunctionName != null && !GetTypeAndMethodName(vertexFunctionName, out _vertexFunctionName))
            {
                throw new ShaderGenerationException(
                    $"The name passed to {nameof(vertexFunctionName)} must be a fully-qualified type and method.");
            }
            if (fragmentFunctionName != null && !GetTypeAndMethodName(fragmentFunctionName, out _fragmentFunctionName))
            {
                throw new ShaderGenerationException(
                    $"The name passed to {nameof(fragmentFunctionName)} must be a fully-qualified type and method.");
            }
        }

        public ShaderModel GenerateShaders()
        {
            HashSet<SyntaxTree> treesToVisit = new HashSet<SyntaxTree>();
            if (_vertexFunctionName != null)
            {
                GetTrees(treesToVisit, _vertexFunctionName.TypeName);
            }
            if (_fragmentFunctionName != null)
            {
                GetTrees(treesToVisit, _fragmentFunctionName.TypeName);
            }

            ShaderSyntaxWalker walker = null;
            walker = new ShaderSyntaxWalker(_compilation, _languages.ToArray());
            foreach (SyntaxTree tree in treesToVisit)
            {
                walker.Visit(tree.GetRoot());
            }

            return walker.GetShaderModel();
        }

        private void GetTrees(HashSet<SyntaxTree> treesToVisit, string typeName)
        {
            INamedTypeSymbol typeSymbol = _compilation.GetTypeByMetadataName(typeName);
            foreach (SyntaxReference syntaxRef in typeSymbol.DeclaringSyntaxReferences)
            {
                treesToVisit.Add(syntaxRef.SyntaxTree);
            }
        }

        private static bool GetTypeAndMethodName(string fullName, out TypeAndMethodName typeAndMethodName)
        {
            string[] parts = fullName.Split(new[] { '.' });
            if (parts.Length < 2)
            {
                typeAndMethodName = default(TypeAndMethodName);
                return false;
            }
            string typeName = parts[0];
            for (int i = 1; i < parts.Length - 1; i++)
            {
                typeName += "." + parts[i];
            }

            typeAndMethodName = new TypeAndMethodName { TypeName = typeName, MethodName = parts[parts.Length - 1] };
            return true;
        }

        public class TypeAndMethodName
        {
            public string TypeName;
            public string MethodName;

            public string ToFullname => TypeName + "." + MethodName;
        }
    }
}
