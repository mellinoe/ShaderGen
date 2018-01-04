using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ShaderGen
{
    internal class MetalMethodVisitor : ShaderMethodVisitor
    {
        public MetalMethodVisitor(Compilation compilation, string setName, ShaderFunction shaderFunction, LanguageBackend backend)
            : base(compilation, setName, shaderFunction, backend)
        {
        }

        protected override string GetFunctionDeclStr()
        {
            string returnType = _backend.CSharpToShaderType(_shaderFunction.ReturnType.Name);
            string fullDeclType = _backend.CSharpToShaderType(_shaderFunction.DeclaringType);
            string funcName = _shaderFunction.IsEntryPoint
                ? _shaderFunction.Name
                : fullDeclType + "_" + _shaderFunction.Name;
            string baseParameterList = GetParameterDeclList();
            string builtinParameterList = string.Join(
                ", ",
                MetalBackend.GetBuiltinParameterList(_shaderFunction).Select(b => $"{b.Type} {b.Name}"));
            string fullParameterList = string.Join(
                ", ",
                new string[]
                {
                    baseParameterList, builtinParameterList
                }.Where(s => !string.IsNullOrEmpty(s)));

            string functionDeclStr = $"{returnType} {funcName}({fullParameterList})";
            return functionDeclStr;
        }

        protected override string FormatParameter(ParameterDefinition pd)
        {
            return $"{_backend.CSharpToShaderType(pd.Type.Name)} {_backend.CorrectIdentifier(pd.Name)}";
        }
    }
}
