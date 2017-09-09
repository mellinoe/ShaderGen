using Microsoft.CodeAnalysis;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderGen
{
    public class HlslBackend : LanguageBackend
    {
        private const string OutSemanticsSuffix = "__OUTSEMANTICS";
        public HlslBackend(SemanticModel model) : base(model)
        {
        }

        protected override string CSharpToShaderTypeCore(string fullType)
        {
            return HlslKnownTypes.GetMappedName(fullType)
                .Replace(".", "_");
        }

        protected void WriteStructure(StringBuilder sb, StructureDefinition sd)
        {
            bool outSemantics = sd.Name.EndsWith(OutSemanticsSuffix);
            sb.AppendLine($"struct {CSharpToShaderType(sd.Name)}");
            sb.AppendLine("{");
            HlslSemanticTracker tracker = new HlslSemanticTracker();
            foreach (FieldDefinition field in sd.Fields)
            {
                sb.AppendLine($"    {CSharpToShaderType(field.Type.Name.Trim())} {field.Name.Trim()} {HlslSemantic(field.SemanticType, outSemantics, ref tracker)};");
            }
            sb.AppendLine("};");
            sb.AppendLine();
        }

        private string HlslSemantic(SemanticType semanticType, bool outSemantics, ref HlslSemanticTracker tracker)
        {
            switch (semanticType)
            {
                case SemanticType.None:
                    return string.Empty;
                case SemanticType.Position:
                    if (outSemantics)
                    {
                        return ": SV_POSITION";
                    }
                    else
                    {
                        int val = tracker.Position++;
                        return ": POSITION" + val.ToString();
                    }
                case SemanticType.Normal:
                    {
                        int val = tracker.Normal++;
                        return ": NORMAL" + val.ToString();
                    }
                case SemanticType.TextureCoordinate:
                    {
                        int val = tracker.TexCoord++;
                        return ": TEXCOORD" + val.ToString();
                    }
                case SemanticType.Color:
                    {
                        int val = tracker.Color++;
                        return ": COLOR" + val.ToString();
                    }
                case SemanticType.Tangent:
                    {
                        int val = tracker.Tangent++;
                        return ": TANGENT" + val.ToString();
                    }
                default: throw new InvalidOperationException("Invalid semantic type: " + semanticType);
            }
        }

        protected void WriteUniform(StringBuilder sb, UniformDefinition ud)
        {
            sb.AppendLine($"cbuffer {ud.Name}Buffer : register(b{ud.Binding})");
            sb.AppendLine("{");
            sb.AppendLine($"    {HlslKnownTypes.GetMappedName(ud.Type.Name.Trim())} {ud.Name.Trim()};");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        protected override string CSharpToShaderFunctionNameCore(string type, string method)
        {
            return HlslKnownFunctions.GetMappedFunctionName(type, method);
        }

        protected override string GenerateFullTextCore()
        {
            if (Functions.Count > 1)
            {
                throw new NotImplementedException();
            }

            StringBuilder sb = new StringBuilder();

            ShaderFunction entryPoint = Functions[0];
            StructureDefinition input = GetRequiredStructureType(Structures, entryPoint.Parameters[0].Type);
            StructureDefinition output = CreateOutputStructure(GetRequiredStructureType(Structures, entryPoint.ReturnType));
            Functions.Remove(entryPoint);
            entryPoint = entryPoint.WithReturnType(new TypeReference(output.Name));
            Functions.Add(entryPoint);

            foreach (StructureDefinition sd in Structures)
            {
                WriteStructure(sb, sd);
            }

            foreach (UniformDefinition ud in Uniforms)
            {
                WriteUniform(sb, ud);
            }

            foreach (ShaderFunction sf in Functions)
            {
                HlslMethodVisitor visitor = new HlslMethodVisitor(Model, sf);
                visitor.VisitBlock(sf.BlockSyntax);
                sb.AppendLine(visitor._value);
            }

            return sb.ToString();
        }

        private StructureDefinition GetRequiredStructureType(List<StructureDefinition> structures, TypeReference type)
        {
            StructureDefinition result = structures.SingleOrDefault(sd => sd.Name == type.Name);
            if (result == null)
            {
                if (!TryDiscoverStructure(type.Name))
                {
                    throw new InvalidOperationException("Type referred by was not discovered: " + type.Name);
                }
            }

            return result;
        }

        private bool TryDiscoverStructure(string name)
        {
            INamedTypeSymbol type = Model.Compilation.GetTypeByMetadataName(name);
            SyntaxNode declaringSyntax = type.OriginalDefinition.DeclaringSyntaxReferences[0].GetSyntax();
            if (declaringSyntax is StructDeclarationSyntax sds)
            {
                if (ShaderSyntaxWalker.TryGetStructDefinition(Model, sds, out StructureDefinition sd))
                {
                    Structures.Add(sd);
                    return true;
                }
            }

            return false;
        }

        private StructureDefinition CreateOutputStructure(StructureDefinition sd)
        {
            StructureDefinition clone = new StructureDefinition(sd.Name + OutSemanticsSuffix, sd.Fields);
            Structures.Add(clone);
            return clone;
        }

        private struct HlslSemanticTracker
        {
            public int Position;
            public int TexCoord;
            public int Normal;
            public int Tangent;
            public int Color;
        }
    }
}
