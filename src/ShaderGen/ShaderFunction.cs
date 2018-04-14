namespace ShaderGen
{
    public class ShaderFunction
    {
        public string DeclaringType { get; }
        public string Name { get; }
        public TypeReference ReturnType { get; }
        public int ColorOutputCount { get; } // TODO: This always returns 0.
        public ParameterDefinition[] Parameters { get; }
        public bool IsEntryPoint => Type != ShaderFunctionType.Normal;
        public ShaderFunctionType Type { get; }
        public UInt3 ComputeGroupCounts { get; }
        public bool UsesVertexID { get; internal set; }
        public bool UsesInstanceID { get; internal set; }
        public bool UsesDispatchThreadID { get; internal set; }
        public bool UsesGroupThreadID { get; internal set; }
        public bool UsesFrontFace { get; internal set; }
        public bool UsesTexture2DMS { get; internal set; }
        public bool UsesStructuredBuffer { get; internal set; }

        public ShaderFunction(
            string declaringType,
            string name,
            TypeReference returnType,
            ParameterDefinition[] parameters,
            ShaderFunctionType type,
            UInt3 computeGroupCounts)
        {
            DeclaringType = declaringType;
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            Type = type;
            ComputeGroupCounts = computeGroupCounts;
        }

        public override string ToString() => $"{DeclaringType}.{Name} [{Type}]";
    }
}
