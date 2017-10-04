using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ShaderGen
{
    public static class StructureDependencyGraph
    {
        public static StructureDefinition[] GetOrderedStructureList(Compilation compilation, List<StructureDefinition> allDefs)
        {
            List<StructureDefinition> results = new List<StructureDefinition>();
            foreach (StructureDefinition sd in allDefs)
            {
                Traverse(compilation, allDefs, sd, results);
            }

            return results.ToArray();
        }

        private static void Traverse(
            Compilation compilation,
            List<StructureDefinition> allDefs,
            StructureDefinition current,
            List<StructureDefinition> results)
        {
            foreach (FieldDefinition field in current.Fields)
            {
                StructureDefinition fieldTypeDef = allDefs.SingleOrDefault(sd => sd.Name == field.Type.Name);
                if (fieldTypeDef != null)
                {
                    Traverse(compilation, allDefs, fieldTypeDef, results);
                }
            }

            if (!results.Contains(current))
            {
                results.Add(current);
            }
        }
    }
}
