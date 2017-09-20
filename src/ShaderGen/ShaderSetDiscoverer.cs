using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ShaderGen
{
    internal class ShaderSetDiscoverer : CSharpSyntaxWalker
    {
        private readonly HashSet<string> _discoveredNames = new HashSet<string>();
        private readonly List<ShaderSetInfo> _shaderSets = new List<ShaderSetInfo>();
        public override void VisitAttribute(AttributeSyntax node)
        {
            if (node.Name.ToFullString().Contains("ShaderSet"))
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
