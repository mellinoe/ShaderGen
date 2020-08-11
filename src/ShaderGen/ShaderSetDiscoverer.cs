using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ShaderGen
{
    internal class ShaderSetDiscoverer : CSharpSyntaxWalker
    {
        private readonly HashSet<string> _discoveredNames = new HashSet<string>();
        private readonly List<ShaderSetInfo> _shaderSets = new List<ShaderSetInfo>();
        public string DanglingVS { get; set;  }
        public string DanglingFS { get; set;  }
        public string DanglingCS { get; set;  }
        public override void VisitAttribute(AttributeSyntax node)
        {
            // TODO: Only look at assembly-level attributes.
            if (node.Name.ToFullString().Contains("ComputeShaderSet"))
            {
                string name = GetStringParam(node, 0);
                string cs = GetStringParam(node, 1);
                if (!TypeAndMethodName.Get(cs, out TypeAndMethodName csName))
                {
                    throw new ShaderGenerationException("ComputeShaderSetAttribute has an incomplete or invalid compute shader name.");
                }

                _shaderSets.Add(new ShaderSetInfo(name, csName));
            }
            else if (node.Name.ToFullString().Contains("ShaderSet"))
            {
                string name = GetStringParam(node, 0);

                TypeAndMethodName vsName = null;
                string vs = GetStringParam(node, 1);
                if (vs != null && !TypeAndMethodName.Get(vs, out vsName))
                {
                    throw new ShaderGenerationException("ShaderSetAttribute has an incomplete or invalid vertex shader name.");
                }


                TypeAndMethodName fsName = null;
                string fs = GetStringParam(node, 2);
                if (fs != null && !TypeAndMethodName.Get(fs, out fsName))
                {
                    throw new ShaderGenerationException("ShaderSetAttribute has an incomplete or invalid fragment shader name.");
                }

                if (vsName == null && fsName == null)
                {
                    throw new ShaderGenerationException("ShaderSetAttribute must specify at least one shader name.");
                }

                if (!_discoveredNames.Add(name))
                {
                    throw new ShaderGenerationException("Multiple shader sets with the same name were defined: " + name);
                }

                _shaderSets.Add(new ShaderSetInfo(
                    name,
                    vsName,
                    fsName));
            }
            else if (node.Name.ToFullString().Contains("VertexShader"))
            {
                var methodDeclaration = ((MethodDeclarationSyntax) node.Ancestors().First(x => x is MethodDeclarationSyntax));
                var classDeclaration = ((ClassDeclarationSyntax) node.Ancestors().First(x => x is ClassDeclarationSyntax));
                DanglingVS = classDeclaration?.Identifier.ValueText + "." + methodDeclaration?.Identifier.ValueText;
                if (classDeclaration?.Parent is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    //Likely a better way of doing this
                    DanglingVS = namespaceDeclaration.Name + "." + DanglingVS;
                }
            }
            else if (node.Name.ToFullString().Contains("FragmentShader"))
            {
                var methodDeclaration = ((MethodDeclarationSyntax) node.Ancestors().First(x => x is MethodDeclarationSyntax));
                var classDeclaration = ((ClassDeclarationSyntax) node.Ancestors().First(x => x is ClassDeclarationSyntax));
                DanglingFS = classDeclaration?.Identifier.ValueText + "." + methodDeclaration?.Identifier.ValueText;
                if (classDeclaration?.Parent is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    //Likely a better way of doing this
                    DanglingFS = namespaceDeclaration.Name + "." + DanglingFS;
                }
            }
            else if (node.Name.ToFullString().Contains("ComputeShader"))
            {
                var methodDeclaration = ((MethodDeclarationSyntax) node.Ancestors().First(x => x is MethodDeclarationSyntax));
                var classDeclaration = ((ClassDeclarationSyntax) node.Ancestors().First(x => x is ClassDeclarationSyntax));
                DanglingCS = classDeclaration?.Identifier.ValueText + "." + methodDeclaration?.Identifier.ValueText;
                if (classDeclaration?.Parent is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    //Likely a better way of doing this
                    DanglingCS = namespaceDeclaration.Name + "." + DanglingCS;
                }
            }
        }

        private string GetStringParam(AttributeSyntax node, int index)
        {
            string text = node.ArgumentList.Arguments[index].ToFullString();
            if (text == "null")
            {
                return null;
            }
            else
            {
                return text.Trim().TrimStart('"').TrimEnd('"');
            }
        }

        public ShaderSetInfo[] GetShaderSets() => _shaderSets.ToArray();
    }
}
