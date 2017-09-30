using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderGen
{
    public class ShaderModel
    {
        public StructureDefinition[] Structures { get; }
        public ResourceDefinition[] Resources { get; }
        public ShaderFunction[] Functions { get; }

        public ShaderModel(StructureDefinition[] structures, ResourceDefinition[] resources, ShaderFunction[] functions)
        {
            Structures = structures;
            Resources = resources;
            Functions = functions;
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
            else
            {
                StructureDefinition sd = GetStructureDefinition(tr);
                int totalSize = 0;
                foreach (FieldDefinition fd in sd.Fields)
                {
                    totalSize += GetTypeSize(fd.Type);
                }

                if (totalSize == 0)
                {
                    throw new InvalidOperationException("Unable to determine the size fo type: " + tr.Name);
                }

                return totalSize;
            }
        }

        private static readonly Dictionary<string, int> s_knownTypeSizes = new Dictionary<string, int>()
        {
            { "System.Single", 4 },
            { "System.Numerics.Vector2", 8 },
            { "System.Numerics.Vector3", 12 },
            { "System.Numerics.Vector4", 16 },
        };
    }
}
