﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderGen
{
    public class ShaderGenerator
    {
        private readonly Compilation _compilation;
        private readonly List<ShaderSetInfo> _shaderSets = new List<ShaderSetInfo>();
        private readonly LanguageBackend[] _languages;
        private readonly IShaderSetProcessor[] _processors;

        public ShaderGenerator(
            Compilation compilation,
            string vertexFunctionName,
            string fragmentFunctionName,
            params LanguageBackend[] languages)
            : this(
                compilation,
                vertexFunctionName,
                fragmentFunctionName,
                languages,
                Array.Empty<IShaderSetProcessor>())
        { }

        public ShaderGenerator(
            Compilation compilation,
            string vertexFunctionName,
            string fragmentFunctionName,
            LanguageBackend[] languages,
            IShaderSetProcessor[] processors)
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
            if (processors == null)
            {
                throw new ArgumentNullException(nameof(processors));
            }
            if (languages.Length == 0)
            {
                throw new ArgumentException("At least one LanguageBackend must be provided.");
            }

            _compilation = compilation;
            _languages = languages.ToArray();
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

            string setName = string.Empty;

            if (vertexFunctionName != null)
            {
                setName = vertexFunctionName;
            }
            if (fragmentFunctionName != null)
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

            _shaderSets.Add(new ShaderSetInfo(
                setName,
                _vertexFunctionName,
                _fragmentFunctionName));

            _processors = processors;
        }

        public ShaderGenerator(
            Compilation compilation,
            LanguageBackend[] languages)
            : this(compilation, languages, Array.Empty<IShaderSetProcessor>())
        { }

        public ShaderGenerator(
            Compilation compilation,
            LanguageBackend[] languages,
            IShaderSetProcessor[] processors)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            if (languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
            }
            if (processors == null)
            {
                throw new ArgumentNullException(nameof(processors));
            }
            if (languages.Length == 0)
            {
                throw new ArgumentException("At least one LanguageBackend must be provided.");
            }

            _compilation = compilation;
            _languages = languages.ToArray();

            ShaderSetDiscoverer ssd = new ShaderSetDiscoverer();
            foreach (SyntaxTree tree in _compilation.SyntaxTrees)
            {
                ssd.Visit(tree.GetRoot());
            }
            _shaderSets.AddRange(ssd.GetShaderSets());

            _processors = processors;
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
                foreach (GeneratedShaderSet gss in result.GetOutput(_languages[0]))
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

            foreach (var language in _languages)
            {
                language.InitContext(ss.Name);
            }

            ShaderSyntaxWalker walker = new ShaderSyntaxWalker(_compilation, _languages, ss);
            foreach (SyntaxTree tree in treesToVisit)
            {
                walker.Visit(tree.GetRoot());
            }

            foreach (var language in _languages)
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
