using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShaderGen
{
    public abstract class LanguageBackend
    {
        private readonly SemanticModel _model;

        public SemanticModel Model => _model;

        public List<StructureDefinition> Structures { get; } = new List<StructureDefinition>();
        public List<UniformDefinition> Uniforms { get; } = new List<UniformDefinition>();
        public List<ShaderFunctionAndBlockSyntax> Functions { get; } = new List<ShaderFunctionAndBlockSyntax>();

        public LanguageBackend(SemanticModel model)
        {
            _model = model;
        }

        public ShaderModel GetShaderModel()
        {
            return new ShaderModel(
                Structures.ToArray(),
                Uniforms.ToArray(),
                Functions.FirstOrDefault()?.Function);
        }

        public string CSharpToShaderType(string fullType)
        {
            if (fullType == null)
            {
                throw new ArgumentNullException(nameof(fullType));
            }

            return CSharpToShaderTypeCore(fullType);
        }

        public string CSharpToShaderType(TypeSyntax typeSyntax)
        {
            return CSharpToShaderTypeCore(_model.GetFullTypeName(typeSyntax));
        }


        public virtual void AddStructure(StructureDefinition sd)
        {
            if (sd == null)
            {
                throw new ArgumentNullException(nameof(sd));
            }

            Structures.Add(sd);
        }

        public virtual void AddUniform(UniformDefinition ud)
        {
            if (ud == null)
            {
                throw new ArgumentNullException(nameof(ud));
            }

            Uniforms.Add(ud);
        }

        public virtual void AddFunction(ShaderFunctionAndBlockSyntax sf)
        {
            if (sf == null)
            {
                throw new ArgumentNullException(nameof(sf));
            }

            Functions.Add(sf);
        }

        public string CSharpToShaderFunctionName(string type, string method)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            return CSharpToShaderFunctionNameCore(type, method);
        }

        public string GenerateFullText() => GenerateFullTextCore();
        protected abstract string CSharpToShaderTypeCore(string fullType);
        protected abstract string CSharpToShaderFunctionNameCore(string type, string method);
        protected abstract string GenerateFullTextCore();
    }
}
