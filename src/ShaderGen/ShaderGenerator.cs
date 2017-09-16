using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace ShaderGen
{
    public partial class ShaderGenerator
    {
        private readonly Compilation _compilation;
        private readonly List<ShaderSetInfo> _shaderSets = new List<ShaderSetInfo>();
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
            TypeAndMethodName _vertexFunctionName = null;
            if (vertexFunctionName != null
                && !TypeAndMethodName.Get(vertexFunctionName, out _vertexFunctionName))
            {
                throw new ShaderGenerationException(
                    $"The name passed to {nameof(vertexFunctionName)} must be a fully-qualified type and method.");
            }

            TypeAndMethodName _fragmentFunctionName = null;
            if (fragmentFunctionName != null
                && !TypeAndMethodName.Get(fragmentFunctionName, out _fragmentFunctionName))
            {
                throw new ShaderGenerationException(
                    $"The name passed to {nameof(fragmentFunctionName)} must be a fully-qualified type and method.");
            }

            _shaderSets.Add(new ShaderSetInfo(
                vertexFunctionName + "+" + fragmentFunctionName,
                _vertexFunctionName,
                _fragmentFunctionName));
        }

        public ShaderGenerator(
            Compilation compilation,
            params LanguageBackend[] languages)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
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

            ShaderSetDiscoverer ssd = new ShaderSetDiscoverer();
            foreach (SyntaxTree tree in _compilation.SyntaxTrees)
            {
                ssd.Visit(tree.GetRoot());
            }

            _shaderSets.AddRange(ssd.GetShaderSets());
        }

        public ShaderGenerationResult GenerateShaders()
        {

            if (_shaderSets.Count == 0)
            {
                throw new ShaderGenerationException("No shader sets were discovered.");
            }

            ShaderGenerationResult result = new ShaderGenerationResult();
            foreach (ShaderSetInfo ss in _shaderSets)
            {
                GenerateShaders(ss, result);
            }

            return result;
        }

        private void GenerateShaders(ShaderSetInfo ss, ShaderGenerationResult result)
        {
            TypeAndMethodName vertexFunctionName = ss.VertexShader;
            TypeAndMethodName fragmentFunctionName = ss.FragmentShader;

            HashSet<SyntaxTree> treesToVisit = new HashSet<SyntaxTree>();
            if (vertexFunctionName != null)
            {
                GetTrees(treesToVisit, vertexFunctionName.TypeName);
            }
            if (fragmentFunctionName != null)
            {
                GetTrees(treesToVisit, fragmentFunctionName.TypeName);
            }

            ShaderSyntaxWalker walker = new ShaderSyntaxWalker(_compilation, _languages.ToArray());
            foreach (SyntaxTree tree in treesToVisit)
            {
                walker.Visit(tree.GetRoot());
            }

            ShaderModel model = walker.GetShaderModel();
            ShaderFunction vsFunc = (ss.VertexShader != null)
                ? model.GetFunction(ss.VertexShader.FullName)
                : null;
            ShaderFunction fsFunc = (ss.FragmentShader != null)
                ? model.GetFunction(ss.FragmentShader.FullName)
                : null;
            foreach (LanguageBackend language in _languages)
            {
                string vsCode = null;
                string fsCode = null;
                if (vsFunc != null)
                {
                    vsCode = language.GetCode(vsFunc);
                }
                if (fsFunc != null)
                {
                    fsCode = language.GetCode(fsFunc);
                }

                result.AddShaderSet(language, new GeneratedShaderSet(ss.Name, vsCode, fsCode, model));
            }
        }

        private void GetTrees(HashSet<SyntaxTree> treesToVisit, string typeName)
        {
            INamedTypeSymbol typeSymbol = _compilation.GetTypeByMetadataName(typeName);
            if (typeSymbol == null)
            {
                throw new ShaderGenerationException("No type was found with the name " + typeName);
            }
            foreach (SyntaxReference syntaxRef in typeSymbol.DeclaringSyntaxReferences)
            {
                treesToVisit.Add(syntaxRef.SyntaxTree);
            }
        }
    }
}
