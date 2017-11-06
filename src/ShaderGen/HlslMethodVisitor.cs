using Microsoft.CodeAnalysis;

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
            string functionDeclStr = $"{returnType} {funcName}({GetParameterDeclList()}){suffix}";
            return functionDeclStr;
        }
    }
}
