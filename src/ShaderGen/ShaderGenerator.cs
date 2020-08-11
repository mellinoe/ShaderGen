using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderGen
{
    public partial class ShaderGenerator
    {
        private readonly Compilation _compilation;
        private readonly IReadOnlyList<ShaderSetInfo> _shaderSets = new List<ShaderSetInfo>();
        private readonly IReadOnlyList<LanguageBackend> _languages;
        private readonly IShaderSetProcessor[] _processors;

        public ShaderGenerator(
            Compilation compilation,
            LanguageBackend[] languages,
            params IShaderSetProcessor[] processors)
            : this(compilation, languages, null, null, null, processors) { }

        public ShaderGenerator(
            Compilation compilation,
            LanguageBackend language,
            params IShaderSetProcessor[] processors)
            : this(compilation, new[] { language }, null, null, null, processors) { }

        public ShaderGenerator(
            Compilation compilation,
            LanguageBackend language,
            string vertexFunctionName = null,
            string fragmentFunctionName = null,
            string computeFunctionName = null,
            params IShaderSetProcessor[] processors)
        : this(compilation, new[] { language }, vertexFunctionName, fragmentFunctionName, computeFunctionName, processors) { }

        public ShaderGenerator(
            Compilation compilation,
            LanguageBackend[] languages,
            string vertexFunctionName = null,
            string fragmentFunctionName = null,
            string computeFunctionName = null,
            params IShaderSetProcessor[] processors)
        {

            if (languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
            }

            if (languages.Length < 1)
            {
                throw new ArgumentException("At least one LanguageBackend must be provided.");
            }

            _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            _languages = languages.ToArray();
            _processors = processors;

            // If we've not specified any names, we're auto-discovering
            if (string.IsNullOrWhiteSpace(vertexFunctionName) &&
                string.IsNullOrWhiteSpace(fragmentFunctionName) &&
                string.IsNullOrWhiteSpace(computeFunctionName))
            {
                ShaderSetDiscoverer ssd = new ShaderSetDiscoverer();
                foreach (SyntaxTree tree in _compilation.SyntaxTrees)
                {
                    ssd.Visit(tree.GetRoot());
                }

                if (_shaderSets.Count > 0)
                {
                    _shaderSets = ssd.GetShaderSets();
                    return;
                }
                
                if (string.IsNullOrEmpty(ssd.DanglingVS) && string.IsNullOrEmpty(ssd.DanglingFS) && string.IsNullOrEmpty(ssd.DanglingCS))
                {
                    throw new ShaderGenerationException("No shader sets discovered and no entry points specified");
                }

                vertexFunctionName = ssd.DanglingVS;
                fragmentFunctionName = ssd.DanglingFS;
                computeFunctionName = ssd.DanglingCS;

            }

            // We've explicitly specified shaders so find them directly.
            List<ShaderSetInfo> shaderSets = new List<ShaderSetInfo>();

            TypeAndMethodName vertex = null;
            if (!string.IsNullOrWhiteSpace(vertexFunctionName)
                && !TypeAndMethodName.Get(vertexFunctionName, out vertex))
            {
                throw new ShaderGenerationException(
                    $"The name passed to {nameof(vertexFunctionName)} must be a fully-qualified type and method.");
            }

            TypeAndMethodName fragment = null;
            if (!string.IsNullOrWhiteSpace(fragmentFunctionName)
                && !TypeAndMethodName.Get(fragmentFunctionName, out fragment))
            {
                throw new ShaderGenerationException(
                    $"The name passed to {nameof(fragmentFunctionName)} must be a fully-qualified type and method.");
            }

            if (vertex != null || fragment != null)
            {
                // We have either a vertex or fragment, so create a graphics shader set.
                string setName = string.Empty;

                if (vertex != null)
                {
                    setName = vertexFunctionName;
                }

                if (fragment != null)
                {
                    if (setName == string.Empty)
                    {
                        setName = fragmentFunctionName;
                    }
                    else
                    {
                        setName += "+" + fragmentFunctionName;
                    }
                }

                shaderSets.Add(new ShaderSetInfo(setName, vertex, fragment));
            }

            TypeAndMethodName compute = null;
            if (!string.IsNullOrWhiteSpace(computeFunctionName)
                && !TypeAndMethodName.Get(computeFunctionName, out compute))
            {
                throw new ShaderGenerationException(
                    $"The name passed to {nameof(computeFunctionName)} must be a fully-qualified type and method.");
            }

            if (compute != null)
            {
                shaderSets.Add(new ShaderSetInfo(computeFunctionName, compute));
            }

            _shaderSets = shaderSets.ToArray();
        }

        public ShaderGenerationResult GenerateShaders()
        {
            ShaderGenerationResult result = new ShaderGenerationResult();
            foreach (ShaderSetInfo ss in _shaderSets)
            {
                GenerateShaders(ss, result);
            }

            // Activate processors
            foreach (IShaderSetProcessor processor in _processors)
            {
                // Kind of a hack, but the relevant info should be the same.
                foreach (GeneratedShaderSet gss in result.GetOutput(_languages.First()))
                {
                    ShaderSetProcessorInput input = new ShaderSetProcessorInput(
                        gss.Name,
                        gss.VertexFunction,
                        gss.FragmentFunction,
                        gss.Model);
                    processor.ProcessShaderSet(input);
                }
            }

            return result;
        }

        private void GenerateShaders(ShaderSetInfo ss, ShaderGenerationResult result)
        {
            TypeAndMethodName vertexFunctionName = ss.VertexShader;
            TypeAndMethodName fragmentFunctionName = ss.FragmentShader;
            TypeAndMethodName computeFunctionName = ss.ComputeShader;

            HashSet<SyntaxTree> treesToVisit = new HashSet<SyntaxTree>();
            if (vertexFunctionName != null)
            {
                GetTrees(treesToVisit, vertexFunctionName.TypeName);
            }
            if (fragmentFunctionName != null)
            {
                GetTrees(treesToVisit, fragmentFunctionName.TypeName);
            }
            if (computeFunctionName != null)
            {
                GetTrees(treesToVisit, computeFunctionName.TypeName);
            }

            foreach (LanguageBackend language in _languages)
            {
                language.InitContext(ss.Name);
            }

            ShaderSyntaxWalker walker = new ShaderSyntaxWalker(_compilation, _languages.ToArray(), ss);
            foreach (SyntaxTree tree in treesToVisit)
            {
                walker.Visit(tree.GetRoot());
            }

            foreach (LanguageBackend language in _languages)
            {
                ShaderModel model = language.GetShaderModel(ss.Name);
                ShaderFunction vsFunc = (ss.VertexShader != null)
                    ? model.GetFunction(ss.VertexShader.FullName)
                    : null;
                ShaderFunction fsFunc = (ss.FragmentShader != null)
                    ? model.GetFunction(ss.FragmentShader.FullName)
                    : null;
                ShaderFunction csFunc = (ss.ComputeShader != null)
                    ? model.GetFunction(ss.ComputeShader.FullName)
                    : null;
                string vsCode = null;
                string fsCode = null;
                string csCode = null;
                if (vsFunc != null)
                {
                    vsCode = language.ProcessEntryFunction(ss.Name, vsFunc).FullText;
                }
                if (fsFunc != null)
                {
                    fsCode = language.ProcessEntryFunction(ss.Name, fsFunc).FullText;
                }
                if (csFunc != null)
                {
                    csCode = language.ProcessEntryFunction(ss.Name, csFunc).FullText;
                }

                result.AddShaderSet(
                    language,
                    new GeneratedShaderSet(ss.Name, vsCode, fsCode, csCode, vsFunc, fsFunc, csFunc, model));
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
