using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ShaderGen
{
    public abstract class LanguageBackend
    {
        protected readonly Compilation Compilation;

        internal class BackendContext
        {
            internal List<StructureDefinition> Structures { get; } = new List<StructureDefinition>();
            internal List<ResourceDefinition> Resources { get; } = new List<ResourceDefinition>();
            internal List<ShaderFunctionAndMethodDeclarationSyntax> Functions { get; } = new List<ShaderFunctionAndMethodDeclarationSyntax>();
        }

        internal Dictionary<string, BackendContext> Contexts = new Dictionary<string, BackendContext>();

        private readonly Dictionary<ShaderFunction, MethodProcessResult> _processedFunctions
            = new Dictionary<ShaderFunction, MethodProcessResult>();

        internal LanguageBackend(Compilation compilation)
        {
            Compilation = compilation;
        }

        // Must be called before attempting to retrieve the context.
        internal void InitContext(string setName)
        {
            if (Contexts.ContainsKey(setName))
            {
                throw new InvalidOperationException("A set was initialized twice: " + setName);
            }

            Contexts.Add(setName, new BackendContext());
        }

        internal BackendContext GetContext(string setName)
        {
            if (!Contexts.TryGetValue(setName, out BackendContext ret))
            {
                throw new InvalidOperationException("There was no Shader Set generated with the name " + setName);
            }
            return ret;
        }


        internal ShaderModel GetShaderModel(string setName)
        {
            BackendContext context = GetContext(setName);

            foreach (ResourceDefinition rd in context.Resources
                .Where(rd =>
                    rd.ResourceKind == ShaderResourceKind.Uniform
                    || rd.ResourceKind == ShaderResourceKind.RWStructuredBuffer
                    || rd.ResourceKind == ShaderResourceKind.StructuredBuffer))
            {
                ForceTypeDiscovery(setName, rd.ValueType);
            }
            // HACK: Discover all field structure types.
            foreach (StructureDefinition sd in context.Structures.ToArray())
            {
                foreach (FieldDefinition fd in sd.Fields)
                {
                    ForceTypeDiscovery(setName, fd.Type);
                }
            }

            ResourceDefinition[] vertexResources = null;
            ResourceDefinition[] fragmentResources = null;
            ResourceDefinition[] computeResources = null;

            // HACK: Discover all method input structures.
            foreach (ShaderFunctionAndMethodDeclarationSyntax sf in context.Functions.ToArray())
            {
                if (sf.Function.IsEntryPoint)
                {
                    MethodProcessResult processedFunction = ProcessEntryFunction(setName, sf.Function);

                    if (sf.Function.Type == ShaderFunctionType.VertexEntryPoint)
                    {
                        vertexResources = processedFunction.ResourcesUsed.ToArray();
                    }
                    else if (sf.Function.Type == ShaderFunctionType.FragmentEntryPoint)
                    {
                        fragmentResources = processedFunction.ResourcesUsed.ToArray();
                    }
                    else
                    {
                        Debug.Assert(sf.Function.Type == ShaderFunctionType.ComputeEntryPoint);
                        computeResources = processedFunction.ResourcesUsed.ToArray();
                    }
                }
            }

            return new ShaderModel(
                context.Structures.ToArray(),
                context.Resources.ToArray(),
                context.Functions.Select(sfabs => sfabs.Function).ToArray(),
                vertexResources,
                fragmentResources,
                computeResources);
        }

        internal virtual string CorrectAssignedValue(
            string leftExprType,
            string rightExpr,
            string rightExprType)
        {
            return rightExpr;
        }

        private void ForceTypeDiscovery(string setName, TypeReference fd)
        {
            if (ShaderPrimitiveTypes.IsPrimitiveType(fd.Name))
            {
                return;
            }
            if (!TryDiscoverStructure(setName, fd.Name, out StructureDefinition sd))
            {
                throw new ShaderGenerationException("" +
                    "Resource type's field could not be resolved: " + fd.Name + " " + fd.Name);
            }
            foreach (FieldDefinition field in sd.Fields)
            {
                ForceTypeDiscovery(setName, field.Type);
            }
        }

        public MethodProcessResult ProcessEntryFunction(string setName, ShaderFunction function)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (!_processedFunctions.TryGetValue(function, out MethodProcessResult result))
            {
                if (!function.IsEntryPoint)
                {
                    throw new ShaderGenerationException("Functions listed in a ShaderSet attribute must have either VertexFunction or FragmentFunction attributes.");
                }

                result = GenerateFullTextCore(setName, function);
                _processedFunctions.Add(function, result);
            }

            return result;
        }

        internal string CSharpToShaderType(string fullType)
        {
            if (fullType == null)
            {
                throw new ArgumentNullException(nameof(fullType));
            }

            return CSharpToShaderTypeCore(fullType);
        }

        internal virtual void AddStructure(string setName, StructureDefinition sd)
        {
            if (sd == null)
            {
                throw new ArgumentNullException(nameof(sd));
            }

            List<StructureDefinition> structures = GetContext(setName).Structures;
            if (!structures.Any(old => old.Name == sd.Name))
            {
                structures.Add(sd);
            }
        }

        internal virtual bool IsIndexerAccess(SymbolInfo member)
        {
            return Utilities.GetFullMetadataName(member.Symbol.ContainingType) == "System.Numerics.Matrix4x4"
                && member.Symbol.Name[0] == 'M'
                && char.IsDigit(member.Symbol.Name[1]);
        }

        internal virtual void AddResource(string setName, ResourceDefinition rd)
        {
            if (rd == null)
            {
                throw new ArgumentNullException(nameof(rd));
            }

            GetContext(setName).Resources.Add(rd);
        }

        internal virtual void AddFunction(string setName, ShaderFunctionAndMethodDeclarationSyntax sf)
        {
            if (sf == null)
            {
                throw new ArgumentNullException(nameof(sf));
            }

            var context = GetContext(setName);

            if (!context.Functions.Contains(sf))
            {
                context.Functions.Add(sf);
            }
        }

        internal virtual string CSharpToShaderIdentifierName(SymbolInfo symbolInfo)
        {
            string typeName = symbolInfo.Symbol.ContainingType.ToDisplayString();
            string identifier = symbolInfo.Symbol.Name;

            return CorrectIdentifier(CSharpToIdentifierNameCore(typeName, identifier));
        }

        internal string FormatInvocation(string setName, string type, string method, InvocationParameterInfo[] parameterInfos)
        {
            Debug.Assert(setName != null);
            Debug.Assert(type != null);
            Debug.Assert(method != null);
            Debug.Assert(parameterInfos != null);

            ShaderFunctionAndMethodDeclarationSyntax function = GetContext(setName).Functions
                .SingleOrDefault(
                    sfabs => sfabs.Function.DeclaringType == type && sfabs.Function.Name == method
                        && parameterInfos.Length == sfabs.Function.Parameters.Length);
            if (function != null)
            {
                ParameterDefinition[] funcParameters = function.Function.Parameters;
                string[] formattedParams = new string[funcParameters.Length];
                for (int i = 0; i < formattedParams.Length; i++)
                {
                    formattedParams[i] = FormatInvocationParameter(funcParameters[i], parameterInfos[i]);
                }

                string invocationList = string.Join(", ", formattedParams);
                string fullMethodName = CSharpToShaderType(function.Function.DeclaringType) + "_" + function.Function.Name;
                return $"{fullMethodName}({invocationList})";
            }
            else
            {
                return FormatInvocationCore(setName, type, method, parameterInfos);
            }
        }

        protected virtual string FormatInvocationParameter(ParameterDefinition def, InvocationParameterInfo ipi)
        {
            return CSharpToIdentifierNameCore(ipi.FullTypeName, ipi.Identifier);
        }

        protected void ValidateRequiredSemantics(string setName, ShaderFunction function, ShaderFunctionType type)
        {
            if (type == ShaderFunctionType.VertexEntryPoint)
            {
                StructureDefinition outputType = GetRequiredStructureType(setName, function.ReturnType);
                foreach (FieldDefinition field in outputType.Fields)
                {
                    if (field.SemanticType == SemanticType.None)
                    {
                        throw new ShaderGenerationException("Function return type is missing semantics on field: " + field.Name);
                    }
                }
            }
            if (type != ShaderFunctionType.Normal)
            {
                foreach (ParameterDefinition pd in function.Parameters)
                {
                    StructureDefinition pType = GetRequiredStructureType(setName, pd.Type);
                    foreach (FieldDefinition field in pType.Fields)
                    {
                        if (field.SemanticType == SemanticType.None)
                        {
                            throw new ShaderGenerationException(
                                $"Function parameter {pd.Name}'s type is missing semantics on field: {field.Name}");
                        }
                    }
                }
            }
        }

        protected virtual StructureDefinition GetRequiredStructureType(string setName, TypeReference type)
        {
            StructureDefinition result = GetContext(setName).Structures.SingleOrDefault(sd => sd.Name == type.Name);
            if (result == null)
            {
                if (!TryDiscoverStructure(setName, type.Name, out result))
                {
                    throw new ShaderGenerationException("Type referred by was not discovered: " + type.Name);
                }
            }

            return result;
        }

        internal virtual string CorrectBinaryExpression(
            string leftExpr,
            string leftExprType,
            string operatorToken,
            string rightExpr,
            string rightExprType)
        {
            return $"{leftExpr} {operatorToken} {rightExpr}";
        }

        internal virtual string CorrectFieldAccess(SymbolInfo symbolInfo)
        {
            string mapped = CSharpToShaderIdentifierName(symbolInfo);
            return CorrectIdentifier(mapped);
        }

        protected bool TryDiscoverStructure(string setName, string name, out StructureDefinition sd)
        {
            INamedTypeSymbol type = Compilation.GetTypeByMetadataName(name);
            if (type == null || type.OriginalDefinition.DeclaringSyntaxReferences.Length == 0)
            {
                throw new ShaderGenerationException("Unable to obtain compilation type metadata for " + name);
            }
            SyntaxNode declaringSyntax = type.OriginalDefinition.DeclaringSyntaxReferences[0].GetSyntax();
            if (declaringSyntax is StructDeclarationSyntax sds)
            {
                if (ShaderSyntaxWalker.TryGetStructDefinition(Compilation.GetSemanticModel(sds.SyntaxTree), sds, out sd))
                {
                    AddStructure(setName, sd);
                    return true;
                }
            }

            sd = null;
            return false;
        }

        internal abstract string CorrectIdentifier(string identifier);
        protected abstract string CSharpToShaderTypeCore(string fullType);
        protected abstract string CSharpToIdentifierNameCore(string typeName, string identifier);
        protected abstract MethodProcessResult GenerateFullTextCore(string setName, ShaderFunction function);
        protected abstract string FormatInvocationCore(string setName, string type, string method, InvocationParameterInfo[] parameterInfos);
        internal abstract string GetComputeGroupCountsDeclaration(UInt3 groupCounts);

        internal string CorrectLiteral(string literal)
        {
            if (!literal.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && literal.EndsWith("f", StringComparison.OrdinalIgnoreCase))
            {
                if (!literal.Contains("."))
                {
                    // This isn't a hack at all
                    return literal.Insert(literal.Length - 1, ".");
                }
            }

            return literal;
        }

        internal abstract string ParameterDirection(ParameterDirection direction);

        internal virtual string CorrectCastExpression(string type, string expression)
        {
            return $"({type}) {expression}";
        }

        protected virtual ShaderMethodVisitor VisitShaderMethod(Compilation compilation,string setName,ShaderFunction func)
        {
            return new ShaderMethodVisitor(Compilation, setName,func, this);
        }

        protected HashSet<ResourceDefinition> ProcessFunctions(string setName, ShaderFunctionAndMethodDeclarationSyntax entryPoint,out String funcs,out String entry)
        {
            HashSet<ResourceDefinition> resourcesUsed = new HashSet<ResourceDefinition>();
            StringBuilder sb = new StringBuilder();

            foreach (ShaderFunctionAndMethodDeclarationSyntax f in entryPoint.OrderedFunctionList)
            {
                if (!f.Function.IsEntryPoint)
                {
                    MethodProcessResult processResult = VisitShaderMethod(Compilation, setName, f.Function).VisitFunction(f.MethodDeclaration);
                    foreach (ResourceDefinition rd in processResult.ResourcesUsed)
                    {
                        resourcesUsed.Add(rd);
                    }
                    sb.AppendLine(processResult.FullText);
                }
            }
            funcs = sb.ToString();

            MethodProcessResult result = VisitShaderMethod(Compilation, setName, entryPoint.Function).VisitFunction(entryPoint.MethodDeclaration);
            foreach (ResourceDefinition rd in result.ResourcesUsed)
            {
                resourcesUsed.Add(rd);
            }

            entry = result.FullText;

            return resourcesUsed;
        }
    }
}
