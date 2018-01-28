using System.Linq;
using Microsoft.CodeAnalysis;

namespace ShaderGen.Metal
{
    internal class MetalMethodVisitor : ShaderMethodVisitor
    {
        public MetalMethodVisitor(Compilation compilation, string setName, ShaderFunction shaderFunction, LanguageBackend backend)
            : base(compilation, setName, shaderFunction, backend)
        {
        }

        protected override string GetFunctionDeclStr()
        {
            string returnType = Backend.CSharpToShaderType(ShaderFunction.ReturnType.Name);
            string fullDeclType = Backend.CSharpToShaderType(ShaderFunction.DeclaringType);
            string funcName = ShaderFunction.IsEntryPoint
                ? ShaderFunction.Name
                : fullDeclType + "_" + ShaderFunction.Name;
            string baseParameterList = GetParameterDeclList();
            string builtinParameterList = string.Join(
                ", ",
                MetalBackend.GetBuiltinParameterList(ShaderFunction).Select(b => $"{b.Type} {b.Name}"));
            string fullParameterList = string.Join(
                ", ",
                new[]
                {
                    baseParameterList, builtinParameterList
                }.Where(s => !string.IsNullOrEmpty(s)));

            string functionDeclStr = $"{returnType} {funcName}({fullParameterList})";
            return functionDeclStr;
        }

        protected override string FormatParameter(ParameterDefinition pd)
        {
            return $"{Backend.CSharpToShaderType(pd.Type.Name)} {Backend.CorrectIdentifier(pd.Name)}";
        }
    }
}
