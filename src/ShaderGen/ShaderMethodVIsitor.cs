using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using CodeGeneration.Roslyn;

namespace ShaderGen
{
    public class HlslMethodVisitor : CSharpSyntaxWalker
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly TransformationContext _context;
        private readonly ShaderFunction _shaderFunction;

        public HlslMethodVisitor(TransformationContext context, ShaderFunction shaderFunction)
        {
            _context = context;
            _shaderFunction = shaderFunction;
        }

        public void GenerateHlslText()
        {
            string returnType = HlslKnownTypes.GetMappedName(_shaderFunction.Name);
            
            _sb.AppendLine($"{_sb.}")
        }
    }
}
