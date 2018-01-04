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
            string prefix = string.Empty;
            if (_shaderFunction.Type == ShaderFunctionType.FragmentEntryPoint)
            {
                prefix = "fragment ";
            }
            else if (_shaderFunction.Type == ShaderFunctionType.VertexEntryPoint)
            {
                prefix = "vertex ";
            }
            else if (_shaderFunction.Type == ShaderFunctionType.ComputeEntryPoint)
            {
                prefix = "kernel ";
            }
            string fullDeclType = _backend.CSharpToShaderType(_shaderFunction.DeclaringType);
            string funcName = _shaderFunction.IsEntryPoint
                ? _shaderFunction.Name
                : fullDeclType + "_" + _shaderFunction.Name;
            string baseParameterList = GetParameterDeclList();
            string resourceParameterList = null;
            if (_shaderFunction.Type != ShaderFunctionType.Normal)
            {
                resourceParameterList = ((MetalBackend)_backend).GetResourceParameterList(_shaderFunction, _setName);
            }
            string builtinParameterList = GetBuiltinParameterList();
            string fullParameterList = string.Join(
                ", ",
                new string[]
                {
                    baseParameterList, resourceParameterList, builtinParameterList
                }.Where(s => !string.IsNullOrEmpty(s)));

            string functionDeclStr = $"{prefix}{returnType} {funcName}({fullParameterList})";
            return functionDeclStr;
        }

        private string GetBuiltinParameterList()
        {
            if (!_shaderFunction.IsEntryPoint)
            {
                return string.Empty;
            }

            List<string> values = new List<string>();
            if (_shaderFunction.UsesVertexID)
            {
                values.Add("uint _builtins_VertexID [[ vertex_id ]]");
            }
            if (_shaderFunction.UsesInstanceID)
            {
                values.Add("uint _builtins_InstanceID [[ instance_id ]]");
            }
            if (_shaderFunction.UsesDispatchThreadID)
            {
                values.Add("uint3 _builtins_DispatchThreadID [[ thread_position_in_grid ]]");
            }
            if (_shaderFunction.UsesGroupThreadID)
            {
                values.Add("uint3 _builtins_GroupThreadID [[ thread_position_in_threadgroup ]]");
            }
            if (_shaderFunction.UsesFrontFace)
            {
                values.Add("bool _builtins_IsFrontFace [[ front_facing ]]");
            }

            return string.Join(", ", values);
        }

        protected override string FormatParameter(ParameterDefinition pd)
        {
            string suffix = _shaderFunction.Type != ShaderFunctionType.Normal ? " [[ stage_in ]]" : string.Empty;
            return $"{_backend.CSharpToShaderType(pd.Type.Name)} {_backend.CorrectIdentifier(pd.Name)}{suffix}";
        }
    }
}
