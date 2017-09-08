using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace ShaderGen
{
    public abstract class TypeTranslator
    {
        private readonly TransformationContext _context;

        public TypeTranslator(TransformationContext context)
        {
            _context = context;
        }

        public string CSharpToShaderType(string fullType)
        {
            if (fullType == null)
            {
                throw new ArgumentNullException(nameof(fullType));
            }

            return CSharpToShaderTypeCore(fullType);
        }

        protected abstract string CSharpToShaderTypeCore(string fullType);

        public string CSharpToShaderType(TypeSyntax typeSyntax)
        {
            return CSharpToShaderTypeCore(_context.GetFullTypeName(typeSyntax));
        }
    }

    public class HlslTypeTranslator : TypeTranslator
    {
        public HlslTypeTranslator(TransformationContext context) : base(context)
        {
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return HlslKnownTypes.GetMappedName(fullType)
                .Replace(".", "_");
        }
    }
}
