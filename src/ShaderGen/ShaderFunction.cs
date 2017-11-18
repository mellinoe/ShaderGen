using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ShaderGen
{
    public class ShaderFunction
    {
        public string DeclaringType { get; }
        public string Name { get; }
        public TypeReference ReturnType { get; }
        public ParameterDefinition[] Parameters { get; }
        public bool IsEntryPoint => Type != ShaderFunctionType.Normal;
        public ShaderFunctionType Type { get; }
        public bool UsesVertexID { get; internal set; }
        public bool UsesInstanceID { get; internal set; }
        public bool UsesDispatchThreadID { get; internal set; }
        public bool UsesGroupThreadID { get; internal set; }

        public ShaderFunction(
            string declaringType,
            string name,
            TypeReference returnType,
            ParameterDefinition[] parameters,
            ShaderFunctionType type)
        {
            DeclaringType = declaringType;
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            Type = type;
        }

        public ShaderFunction WithReturnType(TypeReference returnType)
        {
            return new ShaderFunction(DeclaringType, Name, returnType, Parameters, Type);
        }

        public ShaderFunction WithParameter(int index, TypeReference typeReference)
        {
            ParameterDefinition[] parameters = (ParameterDefinition[])Parameters.Clone();
            parameters[index] = new ParameterDefinition(parameters[index].Name, typeReference);
            return new ShaderFunction(DeclaringType, Name, ReturnType, parameters, Type);
        }

        public static ShaderFunction GetShaderFunction(Compilation compilation, MethodDeclarationSyntax node)
        {
            string functionName = node.Identifier.ToFullString();
            List<ParameterDefinition> parameters = new List<ParameterDefinition>();
            foreach (ParameterSyntax ps in node.ParameterList.Parameters)
            {
                parameters.Add(ParameterDefinition.GetParameterDefinition(compilation, ps));
            }

            TypeReference returnType = new TypeReference(compilation.GetSemanticModel(node.SyntaxTree).GetFullTypeName(node.ReturnType));

            bool isVertexShader, isFragmentShader = false;
            isVertexShader = Utilities.GetMethodAttributes(node, "VertexShader").Any();
            if (!isVertexShader)
            {
                isFragmentShader = Utilities.GetMethodAttributes(node, "FragmentShader").Any();
            }

            ShaderFunctionType type = isVertexShader
                ? ShaderFunctionType.VertexEntryPoint : isFragmentShader
                ? ShaderFunctionType.FragmentEntryPoint : ShaderFunctionType.Normal;

            string nestedTypePrefix = Utilities.GetFullNestedTypePrefix(node, out bool nested);
            return new ShaderFunction(nestedTypePrefix, functionName, returnType, parameters.ToArray(), type);
        }
    }
}
