using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ShaderGen
{
    public class ShaderModel
    {
        public StructureDefinition[] Structures { get; }
        public ResourceDefinition[] AllResources { get; }
        public ShaderFunction[] Functions { get; }

        public ResourceDefinition[] VertexResources { get; }
        public ResourceDefinition[] FragmentResources { get; }
        public ResourceDefinition[] ComputeResources { get; }

        public ShaderModel(
            StructureDefinition[] structures,
            ResourceDefinition[] resources,
            ShaderFunction[] functions,
            ResourceDefinition[] vertexResources,
            ResourceDefinition[] fragmentResources,
            ResourceDefinition[] computeResources)
        {
            Structures = structures;
            AllResources = resources;
            Functions = functions;
            VertexResources = vertexResources;
            FragmentResources = fragmentResources;
            ComputeResources = computeResources;
        }

        public StructureDefinition GetStructureDefinition(TypeReference typeRef) => GetStructureDefinition(typeRef.Name);
        public StructureDefinition GetStructureDefinition(string name)
        {
            return Structures.FirstOrDefault(sd => sd.Name == name);
        }

        public ShaderFunction GetFunction(string name)
        {
            if (name.EndsWith("."))
            {
                throw new ArgumentException($"{nameof(name)} must be a valid function name.");
            }

            if (name.Contains("."))
            {
                name = name.Split(new[] { '.' }).Last();
            }

            return Functions.FirstOrDefault(sf => sf.Name == name);
        }

        public int GetTypeSize(TypeReference tr)
        {
            if (s_knownTypeSizes.TryGetValue(tr.Name, out int ret))
            {
                return ret;
            }
            else if (tr.TypeInfo.TypeKind == TypeKind.Enum)
            {
                string enumBaseType = ((INamedTypeSymbol)tr.TypeInfo).EnumUnderlyingType.GetFullMetadataName();
                if (s_knownTypeSizes.TryGetValue(enumBaseType, out int enumRet))
                {
                    return enumRet;
                }
                else
                {
                    throw new InvalidOperationException($"Unknown enum base type: {enumBaseType}");
                }
            }
            else
            {
                StructureDefinition sd = GetStructureDefinition(tr);
                if (sd == null)
                {
                    throw new InvalidOperationException("Unable to determine the size for type: " + tr.Name);
                }

                return sd.Alignment.CSharpSize;
            }
        }

        private static readonly Dictionary<string, int> s_knownTypeSizes = new Dictionary<string, int>()
        {
            { "System.Byte", 1 },
            { "System.SByte", 1 },
            { "System.UIn16", 2 },
            { "System.Int16", 2 },
            { "System.UInt32", 4 },
            { "System.Int32", 4 },
            { "System.UInt64", 8 },
            { "System.Int64", 8 },
            { "System.Single", 4 },
            { "System.Double", 8 },
            { "System.Numerics.Vector2", 8 },
            { "System.Numerics.Vector3", 12 },
            { "System.Numerics.Vector4", 16 },
            { "System.Numerics.Matrix4x4", 64 },
            { "ShaderGen.UInt2", 8 },
            { "ShaderGen.UInt3", 12 },
            { "ShaderGen.UInt4", 16 },
            { "ShaderGen.Int2", 8 },
            { "ShaderGen.Int3", 12 },
            { "ShaderGen.Int4", 16 },
        };
    }
}
