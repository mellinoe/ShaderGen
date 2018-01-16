using System;
using System.Collections.Generic;

namespace ShaderGen
{
    internal static class ShaderPrimitiveTypes
    {
        private static readonly HashSet<string> s_primitiveTypes = new HashSet<string>()
        {
            "float",
            "System.Single",
            "int",
            "System.Int32",
            "System.Numerics.Vector2",
            "System.Numerics.Vector3",
            "System.Numerics.Vector4",
            "System.Numerics.Matrix4x4",
            "ShaderGen.UInt2",
            "ShaderGen.UInt3",
            "ShaderGen.UInt4",
            "ShaderGen.Int2",
            "ShaderGen.Int3",
            "ShaderGen.Int4",
            "ShaderGen.Byte2",
            "ShaderGen.Byte4",
            "ShaderGen.SByte2",
            "ShaderGen.SByte4",
            "ShaderGen.UShort2",
            "ShaderGen.UShort4",
            "ShaderGen.Short2",
            "ShaderGen.Short4",
        };

        public static bool IsPrimitiveType(string name)
        {
            return s_primitiveTypes.Contains(name);
        }
    }
}