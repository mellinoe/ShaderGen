using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace ShaderGen
{
    public class DuplicateWithSuffixGenerator : ICodeGenerator
    {
        private readonly string suffix;

        public DuplicateWithSuffixGenerator(AttributeData attributeData)
        {
            Requires.NotNull(attributeData, nameof(attributeData));
            this.suffix = (string)attributeData.ConstructorArguments[0].Value;
        }

        public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            var results = SyntaxFactory.List<MemberDeclarationSyntax>();

            // Our generator is applied to any class that our attribute is applied to.
            var applyToClass = (ClassDeclarationSyntax)context.ProcessingMember;

            // Apply a suffix to the name of a copy of the class.
            var copy = applyToClass
                .WithIdentifier(SyntaxFactory.Identifier(applyToClass.Identifier.ValueText + this.suffix));

            ShaderSyntaxWalker walker = new ShaderSyntaxWalker(context);
            walker.Visit(context.SemanticModel.SyntaxTree.GetRoot());
            walker.WriteToFile("outputtext.hlsl");
            // Return our modified copy. It will be added to the user's project for compilation.
            results = results.Add(copy);
            return Task.FromResult<SyntaxList<MemberDeclarationSyntax>>(results);
        }
    }
}