using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ShaderGen.Hlsl
{
    internal class HlslMethodVisitor : ShaderMethodVisitor
    {
        public HlslMethodVisitor(Compilation compilation, string setName, ShaderFunction shaderFunction, LanguageBackend backend)
            : base(compilation, setName, shaderFunction, backend)
        {
        }

        protected override string GetFunctionDeclStr()
        {
            string returnType = Backend.CSharpToShaderType(ShaderFunction.ReturnType.Name);
            string suffix = string.Empty;
            if (ShaderFunction.Type == ShaderFunctionType.FragmentEntryPoint)
            {
                if (ShaderFunction.ReturnType.Name == "System.Numerics.Vector4")
                {
                    suffix = " : SV_Target";
                }
            }
            string fullDeclType = Backend.CSharpToShaderType(ShaderFunction.DeclaringType);
            string funcName = ShaderFunction.IsEntryPoint
                ? ShaderFunction.Name
                : fullDeclType + "_" + ShaderFunction.Name;
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
            if (!ShaderFunction.IsEntryPoint)
            {
                return string.Empty;
            }

            List<string> values = new List<string>();
            if (ShaderFunction.UsesVertexID)
            {
                values.Add("uint _builtins_VertexID : SV_VertexID");
            }
            if (ShaderFunction.UsesInstanceID)
            {
                values.Add("uint _builtins_InstanceID : SV_InstanceID");
            }
            if (ShaderFunction.UsesDispatchThreadID)
            {
                values.Add("uint3 _builtins_DispatchThreadID : SV_DispatchThreadID");
            }
            if (ShaderFunction.UsesGroupThreadID)
            {
                values.Add("uint3 _builtins_GroupThreadID : SV_GroupThreadID");
            }
            if (ShaderFunction.UsesFrontFace)
            {
                values.Add("bool _builtins_IsFrontFace : SV_IsFrontFace");
            }

            return string.Join(", ", values);
        }
    }
}
