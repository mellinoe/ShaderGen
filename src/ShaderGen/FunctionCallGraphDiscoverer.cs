using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace ShaderGen
{
    internal class FunctionCallGraphDiscoverer
    {
        public Compilation Compilation { get; }
        private CallGraphNode _rootNode;
        private Dictionary<TypeAndMethodName, CallGraphNode> _nodesByName = new Dictionary<TypeAndMethodName, CallGraphNode>();

        public FunctionCallGraphDiscoverer(Compilation compilation, TypeAndMethodName rootMethod)
        {
            Compilation = compilation;
            _rootNode = new CallGraphNode() { Name = rootMethod };
            bool foundDecl = GetDeclaration(rootMethod, out _rootNode.Declaration);
            _nodesByName.Add(rootMethod, _rootNode);
        }

        public TypeAndMethodName[] GetOrderedCallList()
        {
            HashSet<TypeAndMethodName> result = new HashSet<TypeAndMethodName>();
            TraverseNode(result, _rootNode);
            return result.ToArray();
        }

        private void TraverseNode(HashSet<TypeAndMethodName> result, CallGraphNode node)
        {
            foreach (TypeAndMethodName existing in result)
            {
                if (node.Parents.Any(cgn => cgn.Name.Equals(existing)))
                {
                    throw new ShaderGenerationException("There was a cyclical call graph involving " + existing + " and " + node.Name);
                }
            }

            foreach (CallGraphNode child in node.Children)
            {
                TraverseNode(result, child);
            }

            result.Add(node.Name);
        }

        public void GenerateFullGraph()
        {
            ExploreCallNode(_rootNode);
        }

        private void ExploreCallNode(CallGraphNode node)
        {
            MethodWalker walker = new MethodWalker(this);
            walker.Visit(node.Declaration);
            TypeAndMethodName[] childrenNames = walker.GetChildren();
            foreach (TypeAndMethodName childName in childrenNames)
            {
                CallGraphNode childNode = GetNode(childName);
                if (childNode.Declaration != null)
                {
                    childNode.Parents.Add(node);
                    node.Children.Add(childNode);
                    ExploreCallNode(childNode);
                }
            }
        }

        private CallGraphNode GetNode(TypeAndMethodName name)
        {
            if (!_nodesByName.TryGetValue(name, out CallGraphNode node))
            {
                node = new CallGraphNode() { Name = name };
                GetDeclaration(name, out node.Declaration);
                _nodesByName.Add(name, node);
            }

            return node;
        }

        private bool GetDeclaration(TypeAndMethodName name, out MethodDeclarationSyntax decl)
        {
            INamedTypeSymbol symb = Compilation.GetTypeByMetadataName(name.TypeName);
            foreach (SyntaxReference synRef in symb.DeclaringSyntaxReferences)
            {
                SyntaxNode node = synRef.GetSyntax();
                foreach (SyntaxNode child in node.ChildNodes())
                {
                    if (child is MethodDeclarationSyntax mds)
                    {
                        if (mds.Identifier.ToFullString() == name.MethodName)
                        {
                            decl = mds;
                            return true;
                        }
                    }
                }
            }

            decl = null;
            return false;
        }

        private class MethodWalker : CSharpSyntaxWalker
        {
            private readonly FunctionCallGraphDiscoverer _discoverer;
            private readonly HashSet<TypeAndMethodName> _children = new HashSet<TypeAndMethodName>();

            public MethodWalker(FunctionCallGraphDiscoverer discoverer)
            {
                _discoverer = discoverer;
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.Expression is IdentifierNameSyntax ins)
                {
                    SymbolInfo symbolInfo = _discoverer.Compilation.GetSemanticModel(node.SyntaxTree).GetSymbolInfo(ins);
                    string containingType = symbolInfo.Symbol.ContainingType.ToDisplayString();
                    string methodName = symbolInfo.Symbol.Name;
                    _children.Add(new TypeAndMethodName() { TypeName = containingType, MethodName = methodName });
                    return;
                }
                else if (node.Expression is MemberAccessExpressionSyntax maes)
                {
                    SymbolInfo methodSymbol = _discoverer.Compilation.GetSemanticModel(maes.SyntaxTree).GetSymbolInfo(maes);
                    if (methodSymbol.Symbol is IMethodSymbol ims)
                    {
                        string containingType = Utilities.GetFullMetadataName(ims.ContainingType);
                        string methodName = ims.MetadataName;
                        _children.Add(new TypeAndMethodName() { TypeName = containingType, MethodName = methodName });
                        return;
                    }
                }

                throw new NotImplementedException();
            }

            public TypeAndMethodName[] GetChildren() => _children.ToArray();
        }
    }

    internal class CallGraphNode
    {
        public TypeAndMethodName Name;
        /// <summary>
        /// May be null.
        /// </summary>
        public MethodDeclarationSyntax Declaration;
        /// <summary>
        /// Functions called by this function.
        /// </summary>
        public HashSet<CallGraphNode> Children = new HashSet<CallGraphNode>();
        public HashSet<CallGraphNode> Parents = new HashSet<CallGraphNode>();
    }
}
