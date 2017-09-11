using System;
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
    }
}
