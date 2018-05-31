using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ShaderGen
{
    public static class TypeSizeCache
    {
        private static readonly IReadOnlyDictionary<string, int> s_knownSizes = new Dictionary<string, int>()
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
        };

        private static readonly IReadOnlyDictionary<string, int> s_shaderAlignments = new Dictionary<string, int>()
        {
            { "System.Numerics.Vector2", 8 },
            { "System.Numerics.Vector3", 16 },
            { "System.Numerics.Vector4", 16 },
            { "System.Numerics.Matrix4x4", 16 },
            { "ShaderGen.UInt2", 8 },
            { "ShaderGen.UInt3", 16 },
            { "ShaderGen.UInt4", 16 },
            { "ShaderGen.Int2", 8 },
            { "ShaderGen.Int3", 16 },
            { "ShaderGen.Int4", 16 },
        };

        private static readonly ConcurrentDictionary<ITypeSymbol, AlignmentInfo> s_cachedSizes = new ConcurrentDictionary<ITypeSymbol, AlignmentInfo>();

        public static AlignmentInfo Get(ITypeSymbol symbol)
        {
            Debug.Assert(symbol.Kind != SymbolKind.ArrayType);
            return s_cachedSizes.TryGetValue(symbol, out AlignmentInfo alignmentInfo)
                ? alignmentInfo
                : Analyze(symbol);
        }

        private static AlignmentInfo Analyze(ITypeSymbol typeSymbol)
        {
            // Check if we already know this type
            if (s_cachedSizes.TryGetValue(typeSymbol, out AlignmentInfo alignmentInfo))
            {
                return alignmentInfo;
            }

            string symbolFullName = typeSymbol.GetFullMetadataName();

            // Get any specific shader alignment
            int? specificShaderAlignment = s_shaderAlignments.TryGetValue(symbolFullName, out int sa)
                ? (int?)sa
                : null;

            // Check if this in our list of known sizes
            if (s_knownSizes.TryGetValue(symbolFullName, out int knownSize))
            {
                alignmentInfo = new AlignmentInfo(knownSize, knownSize, knownSize, specificShaderAlignment ?? knownSize);
                s_cachedSizes.TryAdd(typeSymbol, alignmentInfo);
                return alignmentInfo;
            }

            // Check if enum
            if (typeSymbol.TypeKind == TypeKind.Enum)
            {
                string enumBaseType = ((INamedTypeSymbol)typeSymbol).EnumUnderlyingType.GetFullMetadataName();
                if (!s_knownSizes.TryGetValue(enumBaseType, out int enumSize))
                {
                    throw new ShaderGenerationException($"Unknown enum base type: {enumBaseType}");
                }

                alignmentInfo = new AlignmentInfo(enumSize, enumSize, enumSize, specificShaderAlignment ?? enumSize);
                s_cachedSizes.TryAdd(typeSymbol, alignmentInfo);
                return alignmentInfo;
            }

            // NOTE This check only works for known types accessible to ShaderGen, but it will pick up most non-blittable types.
            if (BlittableHelper.IsBlittable(symbolFullName) == false)
            {
                throw new ShaderGenerationException($"Cannot use the {symbolFullName} type in a shader as it is not a blittable type.");
            }

            // Unknown type, get the instance fields.
            ITypeSymbol[] fields = typeSymbol.GetMembers()
                .Where(symb => symb.Kind == SymbolKind.Field && !symb.IsStatic)
                .Select(symb => ((IFieldSymbol)symb).Type)
                .ToArray();

            if (fields.Length == 0)
            {
                throw new ShaderGenerationException($"No fields on type {symbolFullName}, cannot assess size of structure.");
            }

            int csharpSize = 0;
            int shaderSize = 0;
            int csharpAlignment = 0;
            int shaderAlignment = 0;

            // Calculate size of struct from its fields alignment infos
            foreach (ITypeSymbol fieldType in fields)
            {
                // Determine if type is blittable
                alignmentInfo = Analyze(fieldType);
                csharpAlignment = Math.Max(csharpAlignment, alignmentInfo.CSharpAlignment);
                csharpSize += alignmentInfo.CSharpSize + csharpSize % alignmentInfo.CSharpAlignment;
                shaderAlignment = Math.Max(shaderAlignment, alignmentInfo.ShaderAlignment);
                shaderSize += alignmentInfo.ShaderSize + shaderSize % alignmentInfo.ShaderAlignment;
            }

            // Return new alignment info after adding into cache.
            alignmentInfo = new AlignmentInfo(csharpSize, shaderSize, csharpAlignment, specificShaderAlignment ?? shaderAlignment);
            s_cachedSizes.TryAdd(typeSymbol, alignmentInfo);
            return alignmentInfo;
        }
    }
}
