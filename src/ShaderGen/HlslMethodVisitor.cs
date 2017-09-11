using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    internal class HlslMethodVisitor : ShaderMethodVisitor
    {
        public HlslMethodVisitor(Compilation compilation, ShaderFunction shaderFunction, LanguageBackend backend)
            : base(compilation, shaderFunction, backend)
        {
        }

        protected override string GetFunctionDeclStr()
        {
            string returnType = _backend.CSharpToShaderType(_shaderFunction.ReturnType.Name);
            string suffix = _shaderFunction.Type == ShaderFunctionType.FragmentEntryPoint ? " : SV_Target" : string.Empty;
            string functionDeclStr = $"{returnType} {_shaderFunction.Name}({GetParameterDeclList()}){suffix}";
            return functionDeclStr;
        }
    }
}
