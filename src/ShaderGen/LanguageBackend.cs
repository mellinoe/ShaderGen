using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;

namespace ShaderGen
{
    public abstract class LanguageBackend
    {
        private readonly SemanticModel _model;

        public SemanticModel Model => _model;

        public LanguageBackend(SemanticModel model)
        {
            _model = model;
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

        public void WriteStructures(StringBuilder sb, StructDefinition[] structs)
        {
            foreach (StructDefinition sd in structs)
            {
                WriteStructure(sb, sd);
            }
        }

        public void WriteUniforms(StringBuilder sb, UniformDefinition[] uniforms)
        {
            foreach (UniformDefinition ud in uniforms)
            {
                WriteUniform(sb, ud);
            }
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

        protected abstract string CSharpToShaderTypeCore(string fullType);
        protected abstract void WriteStructure(StringBuilder sb, StructDefinition sd);
        protected abstract void WriteUniform(StringBuilder sb, UniformDefinition ud);
        protected abstract string CSharpToShaderFunctionNameCore(string type, string method);
    }
}
