using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace ShaderGen
{
    internal class HlslMethodVisitor : ShaderMethodVisitor
    {
        public HlslMethodVisitor(Compilation compilation, string setName, ShaderFunction shaderFunction, LanguageBackend backend)
            : base(compilation, setName, shaderFunction, backend)
        {
        }

        protected override string GetFunctionDeclStr()
        {
            string returnType = _backend.CSharpToShaderType(_shaderFunction.ReturnType.Name);
            string suffix = string.Empty;
            if (_shaderFunction.Type == ShaderFunctionType.FragmentEntryPoint)
            {
                if (_shaderFunction.ReturnType.Name == "System.Numerics.Vector4")
                {
                    suffix = " : SV_Target";
                }
            }
            string fullDeclType = _backend.CSharpToShaderType(_shaderFunction.DeclaringType);
            string funcName = _shaderFunction.IsEntryPoint
                ? _shaderFunction.Name
                : fullDeclType + "_" + _shaderFunction.Name;
            string baseParameterList = GetParameterDeclList();
            string builtinParameterList = GetBuiltinParameterList();
            string fullParameterList = string.Empty;
            if (!string.IsNullOrEmpty(baseParameterList))
            {
                if (!string.IsNullOrEmpty(builtinParameterList))
                {
                    fullParameterList = string.Join(", ", baseParameterList, builtinParameterList);
                }
                else
                {
                    fullParameterList = baseParameterList;
                }
            }
            else if (!string.IsNullOrEmpty(builtinParameterList))
            {
                fullParameterList = builtinParameterList;
            }
            string functionDeclStr = $"{returnType} {funcName}({fullParameterList}){suffix}";
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
                values.Add("uint _builtins_VertexID : SV_VertexID");
            }
            if (_shaderFunction.UsesInstanceID)
            {
                values.Add("uint _builtins_InstanceID : SV_InstanceID");
            }
            if (_shaderFunction.UsesDispatchThreadID)
            {
                values.Add("uint3 _builtins_DispatchThreadID : SV_DispatchThreadID");
            }
            if (_shaderFunction.UsesGroupThreadID)
            {
                values.Add("uint3 _builtins_GroupThreadID : SV_GroupThreadID");
            }

            return string.Join(", ", values);
        }
    }
}
