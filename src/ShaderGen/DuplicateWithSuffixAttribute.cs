using CodeGeneration.Roslyn;
using System;
using System.Diagnostics;
using Validation;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
[CodeGenerationAttribute(typeof(DuplicateWithSuffixGenerator))]
[Conditional("CodeGeneration")]
public class DuplicateWithSuffixAttribute : Attribute
{
    public DuplicateWithSuffixAttribute(string suffix)
    {
        Requires.NotNullOrEmpty(suffix, nameof(suffix));
        this.Suffix = suffix;
    }

    public string Suffix { get; }
}
