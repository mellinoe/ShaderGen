using System.Collections.Generic;
using System.Numerics;

namespace ShaderGen.Hlsl
{
    public static class HlslKnownIdentifiers
    {
        private static Dictionary<string, Dictionary<string, string>> s_mappings = GetMappings();

        private static Dictionary<string, Dictionary<string, string>> GetMappings()
        {
            Dictionary<string, Dictionary<string, string>> ret = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> builtinMappings = new Dictionary<string, string>()
            {
                { nameof(ShaderBuiltins.E), "2.71828182845905" },
                { nameof(ShaderBuiltins.PI), "3.14159265358979" }
            };
            ret.Add("ShaderGen.ShaderBuiltins", builtinMappings);

            Dictionary<string, string> v2Mappings = new Dictionary<string, string>()
            {
                { nameof(Vector2.X), "x" },
                { nameof(Vector2.Y), "y" },
            };
            ret.Add("System.Numerics.Vector2", v2Mappings);

            Dictionary<string, string> v3Mappings = new Dictionary<string, string>()
            {
                { nameof(Vector3.X), "x" },
                { nameof(Vector3.Y), "y" },
                { nameof(Vector3.Z), "z" },
            };
            ret.Add("System.Numerics.Vector3", v3Mappings);

            Dictionary<string, string> v4Mappings = new Dictionary<string, string>()
            {
                { nameof(Vector4.X), "x" },
                { nameof(Vector4.Y), "y" },
                { nameof(Vector4.Z), "z" },
                { nameof(Vector4.W), "w" },
            };
            ret.Add("System.Numerics.Vector4", v4Mappings);

            Dictionary<string, string> m4x4Mappings = new Dictionary<string, string>()
            {
                { nameof(Matrix4x4.M11), "[0][0]" },
                { nameof(Matrix4x4.M12), "[0][1]" },
                { nameof(Matrix4x4.M13), "[0][2]" },
                { nameof(Matrix4x4.M14), "[0][3]" },
                { nameof(Matrix4x4.M21), "[1][0]" },
                { nameof(Matrix4x4.M22), "[1][1]" },
                { nameof(Matrix4x4.M23), "[1][2]" },
                { nameof(Matrix4x4.M24), "[1][3]" },
                { nameof(Matrix4x4.M31), "[2][0]" },
                { nameof(Matrix4x4.M32), "[2][1]" },
                { nameof(Matrix4x4.M33), "[2][2]" },
                { nameof(Matrix4x4.M34), "[2][3]" },
                { nameof(Matrix4x4.M41), "[3][0]" },
                { nameof(Matrix4x4.M42), "[3][1]" },
                { nameof(Matrix4x4.M43), "[3][2]" },
                { nameof(Matrix4x4.M44), "[3][3]" },
            };
            ret.Add("System.Numerics.Matrix4x4", m4x4Mappings);

            Dictionary<string, string> uint2Mappings = new Dictionary<string, string>()
            {
                { nameof(UInt2.X), "x" },
                { nameof(UInt2.Y), "y" },
            };
            ret.Add("ShaderGen.UInt2", uint2Mappings);

            Dictionary<string, string> uint3Mappings = new Dictionary<string, string>()
            {
                { nameof(UInt3.X), "x" },
                { nameof(UInt3.Y), "y" },
                { nameof(UInt3.Z), "z" },
            };
            ret.Add("ShaderGen.UInt3", uint3Mappings);

            Dictionary<string, string> uint4Mappings = new Dictionary<string, string>()
            {
                { nameof(UInt4.X), "x" },
                { nameof(UInt4.Y), "y" },
                { nameof(UInt4.Z), "z" },
                { nameof(UInt4.W), "w" },
            };
            ret.Add("ShaderGen.UInt4", uint4Mappings);

            Dictionary<string, string> int2Mappings = new Dictionary<string, string>()
            {
                { nameof(Int2.X), "x" },
                { nameof(Int2.Y), "y" },
            };
            ret.Add("ShaderGen.Int2", int2Mappings);

            Dictionary<string, string> int3Mappings = new Dictionary<string, string>()
            {
                { nameof(Int3.X), "x" },
                { nameof(Int3.Y), "y" },
                { nameof(Int3.Z), "z" },
            };
            ret.Add("ShaderGen.Int3", int3Mappings);

            Dictionary<string, string> int4Mappings = new Dictionary<string, string>()
            {
                { nameof(Int4.X), "x" },
                { nameof(Int4.Y), "y" },
                { nameof(Int4.Z), "z" },
                { nameof(Int4.W), "w" }
            };
            ret.Add("ShaderGen.Int4", int4Mappings);

            Dictionary<string, string> mathfMappings = new Dictionary<string, string>()
            {
                // TODO Note MathF is not included in .Net Standard
                { "E", "2.71828182845905" },
                { "PI", "3.14159265358979" }
            };
            ret.Add("System.MathF", mathfMappings);

            return ret;
        }

        public static string GetMappedIdentifier(string type, string identifier)
        {
            if (s_mappings.TryGetValue(type, out var dict))
            {
                if (dict.TryGetValue(identifier, out string mappedValue))
                {
                    return mappedValue;
                }
            }

            return identifier;
        }
    }
}
