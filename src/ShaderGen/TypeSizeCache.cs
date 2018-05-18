using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ShaderGen
{
    public static class TypeSizeCache
    {
        private static readonly Dictionary<ITypeSymbol, AlignmentInfo> s_cachedSizes = new Dictionary<ITypeSymbol, AlignmentInfo>();
        private static readonly HashSet<ITypeSymbol> s_encounteredSymbols = new HashSet<ITypeSymbol>();
        private static readonly object s_lock = new object();

        public static AlignmentInfo Get(SemanticModel model, ITypeSymbol symbol)
        {
            Debug.Assert(symbol.Kind != SymbolKind.ArrayType); // Compute from element size manually.

            lock (s_lock)
            {
                if (!s_cachedSizes.TryGetValue(symbol, out AlignmentInfo ret))
                {
                    ret = Analyze(model, symbol);
                    s_cachedSizes.Add(symbol, ret);
                }

                return ret;
            }
        }

        private static AlignmentInfo Analyze(SemanticModel model, ITypeSymbol symbol)
        {
            int csharpSize = GetCSharpSize(model, symbol);
            int shaderSize = GetShaderSize(model, symbol);
            int csharpAlignment = GetCSharpAlignment(model, symbol);
            int shaderAlignment = GetShaderAlignment(model, symbol);
            return new AlignmentInfo(csharpSize, shaderSize, csharpAlignment, shaderAlignment);
        }

        private static int GetCSharpSize(SemanticModel model, ITypeSymbol symbol)
        {
            string symbolFullName = symbol.GetFullMetadataName();
            if (s_knownSizes.TryGetValue(symbolFullName, out int size))
            {
                return size;
            }

            if (symbol.TypeKind == TypeKind.Enum)
            {
                string enumBaseType = ((INamedTypeSymbol)symbol).EnumUnderlyingType.GetFullMetadataName();
                if (s_knownSizes.TryGetValue(enumBaseType, out int enumRet))
                {
                    return enumRet;
                }
                else
                {
                    throw new ShaderGenerationException($"Unknown enum base type: {enumBaseType}");
                }
            }

            ISymbol[] fields = symbol.GetMembers().Where(symb => symb.Kind == SymbolKind.Field).ToArray();
            if (fields.Length == 0)
            {
                throw new ShaderGenerationException($"No fields on type {symbolFullName}");
            }

            int totalSize = 0;
            foreach (ISymbol field in fields)
            {
                ITypeSymbol fieldType = ((IFieldSymbol)field).Type;
                int fieldAlignment = GetCSharpAlignment(model, fieldType);
                totalSize += totalSize % fieldAlignment;
                totalSize += GetCSharpSize(model, fieldType);
            }

            return totalSize;
        }

        private static int GetCSharpAlignment(SemanticModel model, ITypeSymbol symbol)
        {
            string symbolFullName = symbol.GetFullMetadataName();
            if (s_knownSizes.TryGetValue(symbolFullName, out int size))
            {
                return size;
            }

            if (symbol.TypeKind == TypeKind.Enum)
            {
                string enumBaseType = ((INamedTypeSymbol)symbol).EnumUnderlyingType.GetFullMetadataName();
                if (s_knownSizes.TryGetValue(enumBaseType, out int enumRet))
                {
                    return enumRet;
                }
                else
                {
                    throw new ShaderGenerationException($"Unknown enum base type: {enumBaseType}");
                }
            }

            ISymbol[] fields = symbol.GetMembers().Where(symb => symb.Kind == SymbolKind.Field).ToArray();
            if (fields.Length == 0)
            {
                throw new ShaderGenerationException($"No fields on type {symbolFullName}");
            }

            int maxFieldSize = 0;
            foreach (ISymbol field in fields)
            {
                ITypeSymbol fieldType = ((IFieldSymbol)field).Type;
                int fieldSize = GetCSharpAlignment(model, fieldType);
                maxFieldSize = Math.Max(maxFieldSize, fieldSize);
            }

            return maxFieldSize;
        }

        private static int GetShaderSize(SemanticModel model, ITypeSymbol symbol)
        {
            string symbolFullName = symbol.GetFullMetadataName();
            if (s_knownSizes.TryGetValue(symbolFullName, out int size))
            {
                return size;
            }

            if (symbol.TypeKind == TypeKind.Enum)
            {
                string enumBaseType = ((INamedTypeSymbol)symbol).EnumUnderlyingType.GetFullMetadataName();
                if (s_knownSizes.TryGetValue(enumBaseType, out int enumRet))
                {
                    return enumRet;
                }
                else
                {
                    throw new ShaderGenerationException($"Unknown enum base type: {enumBaseType}");
                }
            }

            ISymbol[] fields = symbol.GetMembers().Where(symb => symb.Kind == SymbolKind.Field).ToArray();
            if (fields.Length == 0)
            {
                throw new ShaderGenerationException($"No fields on type {symbolFullName}");
            }

            int totalSize = 0;
            foreach (ISymbol field in fields)
            {
                ITypeSymbol fieldType = ((IFieldSymbol)field).Type;
                int fieldAlignment = GetShaderAlignment(model, fieldType);
                totalSize += totalSize % fieldAlignment;
                totalSize += GetShaderSize(model, fieldType);
            }

            return totalSize;
        }

        private static int GetShaderAlignment(SemanticModel model, ITypeSymbol symbol)
        {
            string symbolFullName = symbol.GetFullMetadataName();
            if (s_shaderAlignments.TryGetValue(symbolFullName, out int specialAlignment))
            {
                return specialAlignment;
            }
            if (s_knownSizes.TryGetValue(symbolFullName, out int size))
            {
                return size;
            }

            if (symbol.TypeKind == TypeKind.Enum)
            {
                string enumBaseType = ((INamedTypeSymbol)symbol).EnumUnderlyingType.GetFullMetadataName();
                if (s_knownSizes.TryGetValue(enumBaseType, out int enumRet))
                {
                    return enumRet;
                }
                else
                {
                    throw new ShaderGenerationException($"Unknown enum base type: {enumBaseType}");
                }
            }

            ISymbol[] fields = symbol.GetMembers().Where(symb => symb.Kind == SymbolKind.Field).ToArray();
            if (fields.Length == 0)
            {
                throw new ShaderGenerationException($"No fields on type {symbolFullName}");
            }

            int maxFieldSize = 0;
            foreach (ISymbol field in fields)
            {
                ITypeSymbol fieldType = ((IFieldSymbol)field).Type;
                int fieldSize = GetShaderAlignment(model, fieldType);
                maxFieldSize = Math.Max(maxFieldSize, fieldSize);
            }

            return maxFieldSize;
        }

        private static readonly Dictionary<string, int> s_knownSizes = new Dictionary<string, int>()
        {
            { "System.Byte", 1 },
            { "System.SByte", 1 },
            { "System.UIn16", 2 },
            { "System.Int16", 2 },
            { "System.UInt32", 4 },
            { "System.Int32", 4 },
            { "System.UInt64", 4 },
            { "System.Int64", 4 },
            { "System.Single", 4 },
            { "System.Double", 4 },
        };

        private static readonly Dictionary<string, int> s_shaderAlignments = new Dictionary<string, int>()
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
    }

    public struct AlignmentInfo
    {
        public readonly int CSharpSize;
        public readonly int ShaderSize;
        public readonly int CSharpAlignment;
        public readonly int ShaderAlignment;

        public AlignmentInfo(int csharpSize, int shaderSize, int csharpAlignment, int shaderAlignment)
        {
            CSharpSize = csharpSize;
            ShaderSize = shaderSize;
            CSharpAlignment = csharpAlignment;
            ShaderAlignment = shaderAlignment;
        }
    }
}
