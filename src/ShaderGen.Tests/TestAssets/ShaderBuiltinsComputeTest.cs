using System.Numerics;
using System.Runtime.InteropServices;
using ShaderGen;
using TestShaders;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    /// <summary>
    /// Shader to test built in methods.
    /// </summary>
    public class ShaderBuiltinsComputeTest
    {
        /// <summary>
        /// The number of methods.
        /// </summary>
        public const uint Methods = 147;

        [ResourceSet(0)] public RWStructuredBuffer<ComputeShaderParameters> InOutParameters;

        [ComputeShader(1, 1, 1)]
        public void CS()
        {
            DoCS(DispatchThreadID);
        }

        /*
         * TODO Issue #67 - WORKAROUND until DispatchThreadID is removed and the parameter style implemented
         */
        public void DoCS(UInt3 dispatchThreadID)
        {
            int index = (int)dispatchThreadID.X;

            // ReSharper disable once RedundantCast - WORKAROUND for #75
            if (index >= Methods)
            {
                return;
            }

            ComputeShaderParameters parameters = InOutParameters[index];
            switch (index)
            {
                // Abs
                case 0:
                    parameters.OutFloat = Abs(parameters.P1Float);
                    break;
                case 1:
                    parameters.OutVector2 = Abs(parameters.P1Vector2);
                    break;
                case 2:
                    parameters.OutVector3 = Abs(parameters.P1Vector3);
                    break;
                case 3:
                    parameters.OutVector4 = Abs(parameters.P1Vector4);
                    break;

                // Acos
                case 4:
                    parameters.OutFloat = Acos(parameters.P1Float);
                    break;
                case 5:
                    parameters.OutVector2 = Acos(parameters.P1Vector2);
                    break;
                case 6:
                    parameters.OutVector3 = Acos(parameters.P1Vector3);
                    break;
                case 7:
                    parameters.OutVector4 = Acos(parameters.P1Vector4);
                    break;

                // Acosh
                case 8:
                    parameters.OutFloat = Acosh(parameters.P1Float);
                    break;
                case 9:
                    parameters.OutVector2 = Acosh(parameters.P1Vector2);
                    break;
                case 10:
                    parameters.OutVector3 = Acosh(parameters.P1Vector3);
                    break;
                case 11:
                    parameters.OutVector4 = Acosh(parameters.P1Vector4);
                    break;

                // Asin
                case 12:
                    parameters.OutFloat = Asin(parameters.P1Float);
                    break;
                case 13:
                    parameters.OutVector2 = Asin(parameters.P1Vector2);
                    break;
                case 14:
                    parameters.OutVector3 = Asin(parameters.P1Vector3);
                    break;
                case 15:
                    parameters.OutVector4 = Asin(parameters.P1Vector4);
                    break;

                // Asinh
                case 16:
                    parameters.OutFloat = Asinh(parameters.P1Float);
                    break;
                case 17:
                    parameters.OutVector2 = Asinh(parameters.P1Vector2);
                    break;
                case 18:
                    parameters.OutVector3 = Asinh(parameters.P1Vector3);
                    break;
                case 19:
                    parameters.OutVector4 = Asinh(parameters.P1Vector4);
                    break;

                // Atan
                case 20:
                    parameters.OutFloat = Atan(parameters.P1Float);
                    break;
                case 21:
                    parameters.OutFloat = Atan(parameters.P1Float, parameters.P2Float);
                    break;
                case 22:
                    parameters.OutVector2 = Atan(parameters.P1Vector2);
                    break;
                case 23:
                    parameters.OutVector2 = Atan(parameters.P1Vector2, parameters.P2Vector2);
                    break;
                case 24:
                    parameters.OutVector3 = Atan(parameters.P1Vector3);
                    break;
                case 25:
                    parameters.OutVector3 = Atan(parameters.P1Vector3, parameters.P2Vector3);
                    break;
                case 26:
                    parameters.OutVector4 = Atan(parameters.P1Vector4);
                    break;
                case 27:
                    parameters.OutVector4 = Atan(parameters.P1Vector4, parameters.P2Vector4);
                    break;

                // Atanh
                case 28:
                    parameters.OutFloat = Atanh(parameters.P1Float);
                    break;
                case 29:
                    parameters.OutVector2 = Atanh(parameters.P1Vector2);
                    break;
                case 30:
                    parameters.OutVector3 = Atanh(parameters.P1Vector3);
                    break;
                case 31:
                    parameters.OutVector4 = Atanh(parameters.P1Vector4);
                    break;

                // Cbrt
                case 32:
                    parameters.OutFloat = Cbrt(parameters.P1Float);
                    break;
                case 33:
                    parameters.OutVector2 = Cbrt(parameters.P1Vector2);
                    break;
                case 34:
                    parameters.OutVector3 = Cbrt(parameters.P1Vector3);
                    break;
                case 35:
                    parameters.OutVector4 = Cbrt(parameters.P1Vector4);
                    break;

                // Ceiling
                case 36:
                    parameters.OutFloat = Ceiling(parameters.P1Float);
                    break;
                case 37:
                    parameters.OutVector2 = Ceiling(parameters.P1Vector2);
                    break;
                case 38:
                    parameters.OutVector3 = Ceiling(parameters.P1Vector3);
                    break;
                case 39:
                    parameters.OutVector4 = Ceiling(parameters.P1Vector4);
                    break;

                // Clamp
                case 40:
                    parameters.OutFloat = Clamp(parameters.P1Float, parameters.P2Float, parameters.P3Float);
                    break;
                case 41:
                    parameters.OutVector2 = Clamp(parameters.P1Vector2, parameters.P2Vector2, parameters.P1Vector3.XY());
                    break;
                case 42:
                    parameters.OutVector2 = Clamp(parameters.P1Vector2, parameters.P1Float, parameters.P2Float);
                    break;
                case 43:
                    parameters.OutVector3 = Clamp(parameters.P1Vector3, parameters.P2Vector3, parameters.P1Vector4.XYZ());
                    break;
                case 44:
                    parameters.OutVector3 = Clamp(parameters.P1Vector3, parameters.P1Float, parameters.P2Float);
                    break;
                case 45:
                    parameters.OutVector4 = Clamp(parameters.P1Vector4, parameters.P2Vector4, new Vector4(parameters.P1Vector3, parameters.P1Float));
                    break;
                case 46:
                    parameters.OutVector4 = Clamp(parameters.P1Vector4, parameters.P1Float, parameters.P2Float);
                    break;

                // Cos
                case 47:
                    parameters.OutFloat = Cos(parameters.P1Float);
                    break;
                case 48:
                    parameters.OutVector2 = Cos(parameters.P1Vector2);
                    break;
                case 49:
                    parameters.OutVector3 = Cos(parameters.P1Vector3);
                    break;
                case 50:
                    parameters.OutVector4 = Cos(parameters.P1Vector4);
                    break;

                // Coshh
                case 51:
                    parameters.OutFloat = Cosh(parameters.P1Float);
                    break;
                case 52:
                    parameters.OutVector2 = Cosh(parameters.P1Vector2);
                    break;
                case 53:
                    parameters.OutVector3 = Cosh(parameters.P1Vector3);
                    break;
                case 54:
                    parameters.OutVector4 = Cosh(parameters.P1Vector4);
                    break;

                // Exp
                case 55:
                    parameters.OutFloat = Exp(parameters.P1Float);
                    break;
                case 56:
                    parameters.OutVector2 = Exp(parameters.P1Vector2);
                    break;
                case 57:
                    parameters.OutVector3 = Exp(parameters.P1Vector3);
                    break;
                case 58:
                    parameters.OutVector4 = Exp(parameters.P1Vector4);
                    break;

                // Floor
                case 59:
                    parameters.OutFloat = Floor(parameters.P1Float);
                    break;
                case 60:
                    parameters.OutVector2 = Floor(parameters.P1Vector2);
                    break;
                case 61:
                    parameters.OutVector3 = Floor(parameters.P1Vector3);
                    break;
                case 62:
                    parameters.OutVector4 = Floor(parameters.P1Vector4);
                    break;

                // Frac
                case 63:
                    parameters.OutFloat = Frac(parameters.P1Float);
                    break;
                case 64:
                    parameters.OutVector2 = Frac(parameters.P1Vector2);
                    break;
                case 65:
                    parameters.OutVector3 = Frac(parameters.P1Vector3);
                    break;
                case 66:
                    parameters.OutVector4 = Frac(parameters.P1Vector4);
                    break;

                // Lerp
                case 67:
                    parameters.OutFloat = Lerp(parameters.P1Float, parameters.P2Float, parameters.P3Float);
                    break;
                case 68:
                    parameters.OutVector2 = Lerp(parameters.P1Vector2, parameters.P2Vector2, parameters.P1Vector3.XY());
                    break;
                case 69:
                    parameters.OutVector2 = Lerp(parameters.P1Vector2, parameters.P2Vector2, parameters.P2Float);
                    break;
                case 70:
                    parameters.OutVector3 = Lerp(parameters.P1Vector3, parameters.P2Vector3, parameters.P1Vector4.XYZ());
                    break;
                case 71:
                    parameters.OutVector3 = Lerp(parameters.P1Vector3, parameters.P2Vector3, parameters.P2Float);
                    break;
                case 72:
                    parameters.OutVector4 = Lerp(parameters.P1Vector4, parameters.P2Vector4, new Vector4(parameters.P1Vector3, parameters.P1Float));
                    break;
                case 73:
                    parameters.OutVector4 = Lerp(parameters.P1Vector4, parameters.P2Vector4, parameters.P2Float);
                    break;

                // Log
                case 74:
                    parameters.OutFloat = Log(parameters.P1Float);
                    break;
                case 75:
                    parameters.OutFloat = Log(parameters.P1Float, parameters.P2Float);
                    break;
                case 76:
                    parameters.OutVector2 = Log(parameters.P1Vector2);
                    break;
                case 77:
                    parameters.OutVector2 = Log(parameters.P1Vector2, parameters.P2Vector2);
                    break;
                case 78:
                    parameters.OutVector2 = Log(parameters.P1Vector2, parameters.P1Float);
                    break;
                case 79:
                    parameters.OutVector3 = Log(parameters.P1Vector3);
                    break;
                case 80:
                    parameters.OutVector3 = Log(parameters.P1Vector3, parameters.P2Vector3);
                    break;
                case 81:
                    parameters.OutVector3 = Log(parameters.P1Vector3, parameters.P1Float);
                    break;
                case 82:
                    parameters.OutVector4 = Log(parameters.P1Vector4);
                    break;
                case 83:
                    parameters.OutVector4 = Log(parameters.P1Vector4, parameters.P2Vector4);
                    break;
                case 84:
                    parameters.OutVector4 = Log(parameters.P1Vector4, parameters.P1Float);
                    break;

                // Log2
                case 85:
                    parameters.OutFloat = Log2(parameters.P1Float);
                    break;
                case 86:
                    parameters.OutVector2 = Log2(parameters.P1Vector2);
                    break;
                case 87:
                    parameters.OutVector3 = Log2(parameters.P1Vector3);
                    break;
                case 88:
                    parameters.OutVector4 = Log2(parameters.P1Vector4);
                    break;

                // Log10
                case 89:
                    parameters.OutFloat = Log10(parameters.P1Float);
                    break;
                case 90:
                    parameters.OutVector2 = Log10(parameters.P1Vector2);
                    break;
                case 91:
                    parameters.OutVector3 = Log10(parameters.P1Vector3);
                    break;
                case 92:
                    parameters.OutVector4 = Log10(parameters.P1Vector4);
                    break;

                // Max:
                case 93:
                    parameters.OutFloat = Max(parameters.P1Float, parameters.P2Float);
                    break;
                case 94:
                    parameters.OutVector2 = Max(parameters.P1Vector2, parameters.P2Vector2);
                    break;
                case 95:
                    parameters.OutVector2 = Max(parameters.P1Vector2, parameters.P2Vector2);
                    break;
                case 96:
                    parameters.OutVector3 = Max(parameters.P1Vector3, parameters.P2Vector3);
                    break;
                case 97:
                    parameters.OutVector3 = Max(parameters.P1Vector3, parameters.P2Vector3);
                    break;
                case 98:
                    parameters.OutVector4 = Max(parameters.P1Vector4, parameters.P2Vector4);
                    break;
                case 99:
                    parameters.OutVector4 = Max(parameters.P1Vector4, parameters.P2Vector4);
                    break;

                // Mod:
                case 100:
                    parameters.OutFloat = Mod(parameters.P1Float, parameters.P2Float);
                    break;
                case 101:
                    parameters.OutVector2 = Mod(parameters.P1Vector2, parameters.P2Vector2);
                    break;
                case 102:
                    parameters.OutVector2 = Mod(parameters.P1Vector2, parameters.P2Vector2);
                    break;
                case 103:
                    parameters.OutVector3 = Mod(parameters.P1Vector3, parameters.P2Vector3);
                    break;
                case 104:
                    parameters.OutVector3 = Mod(parameters.P1Vector3, parameters.P2Vector3);
                    break;
                case 105:
                    parameters.OutVector4 = Mod(parameters.P1Vector4, parameters.P2Vector4);
                    break;
                case 106:
                    parameters.OutVector4 = Mod(parameters.P1Vector4, parameters.P2Vector4);
                    break;

                // Mul:
                case 107:
                    parameters.OutVector4 = Mul(parameters.P1Matrix, parameters.P1Vector4);
                    break;

                // Pow
                case 108:
                    parameters.OutFloat = Pow(parameters.P1Float, parameters.P2Float);
                    break;
                case 109:
                    parameters.OutVector2 = Pow(parameters.P1Vector2, parameters.P2Vector2);
                    break;
                case 110:
                    parameters.OutVector3 = Pow(parameters.P1Vector3, parameters.P2Vector3);
                    break;
                case 111:
                    parameters.OutVector4 = Pow(parameters.P1Vector4, parameters.P2Vector4);
                    break;

                // Round
                case 112:
                    parameters.OutFloat = Round(parameters.P1Float);
                    break;
                case 113:
                    parameters.OutVector2 = Round(parameters.P1Vector2);
                    break;
                case 114:
                    parameters.OutVector3 = Round(parameters.P1Vector3);
                    break;
                case 115:
                    parameters.OutVector4 = Round(parameters.P1Vector4);
                    break;

                // Saturate
                case 116:
                    parameters.OutFloat = Saturate(parameters.P1Float);
                    break;
                case 117:
                    parameters.OutVector2 = Saturate(parameters.P1Vector2);
                    break;
                case 118:
                    parameters.OutVector3 = Saturate(parameters.P1Vector3);
                    break;
                case 119:
                    parameters.OutVector4 = Saturate(parameters.P1Vector4);
                    break;

                // Sinh
                case 120:
                    parameters.OutFloat = Sinh(parameters.P1Float);
                    break;
                case 121:
                    parameters.OutVector2 = Sinh(parameters.P1Vector2);
                    break;
                case 122:
                    parameters.OutVector3 = Sinh(parameters.P1Vector3);
                    break;
                case 123:
                    parameters.OutVector4 = Sinh(parameters.P1Vector4);
                    break;

                // Sqrt
                case 124:
                    parameters.OutFloat = Sqrt(parameters.P1Float);
                    break;
                case 125:
                    parameters.OutVector2 = Sqrt(parameters.P1Vector2);
                    break;
                case 126:
                    parameters.OutVector3 = Sqrt(parameters.P1Vector3);
                    break;
                case 127:
                    parameters.OutVector4 = Sqrt(parameters.P1Vector4);
                    break;

                // SmoothStep
                case 128:
                    parameters.OutFloat = SmoothStep(parameters.P1Float, parameters.P2Float, parameters.P3Float);
                    break;
                case 129:
                    parameters.OutVector2 = SmoothStep(parameters.P1Vector2, parameters.P2Vector2, parameters.P1Vector3.XY());
                    break;
                case 130:
                    parameters.OutVector2 = SmoothStep(parameters.P1Float, parameters.P2Float, parameters.P1Vector2);
                    break;
                case 131:
                    parameters.OutVector3 = SmoothStep(parameters.P1Vector3, parameters.P2Vector3, parameters.P1Vector4.XYZ());
                    break;
                case 132:
                    parameters.OutVector3 = SmoothStep(parameters.P1Float, parameters.P2Float, parameters.P1Vector3);
                    break;
                case 133:
                    parameters.OutVector4 = SmoothStep(parameters.P1Vector4, parameters.P2Vector4, new Vector4(parameters.P1Vector3, parameters.P1Float));
                    break;
                case 134:
                    parameters.OutVector4 = SmoothStep(parameters.P1Float, parameters.P2Float, parameters.P1Vector4);
                    break;

                // Tan
                case 135:
                    parameters.OutFloat = Tan(parameters.P1Float);
                    break;
                case 136:
                    parameters.OutVector2 = Tan(parameters.P1Vector2);
                    break;
                case 137:
                    parameters.OutVector3 = Tan(parameters.P1Vector3);
                    break;
                case 138:
                    parameters.OutVector4 = Tan(parameters.P1Vector4);
                    break;

                // Tanh
                case 139:
                    parameters.OutFloat = Tanh(parameters.P1Float);
                    break;
                case 140:
                    parameters.OutVector2 = Tanh(parameters.P1Vector2);
                    break;
                case 141:
                    parameters.OutVector3 = Tanh(parameters.P1Vector3);
                    break;
                case 142:
                    parameters.OutVector4 = Tanh(parameters.P1Vector4);
                    break;

                // Truncate
                case 143:
                    parameters.OutFloat = Truncate(parameters.P1Float);
                    break;
                case 144:
                    parameters.OutVector2 = Truncate(parameters.P1Vector2);
                    break;
                case 145:
                    parameters.OutVector3 = Truncate(parameters.P1Vector3);
                    break;
                case 146:
                    parameters.OutVector4 = Truncate(parameters.P1Vector4);
                    break;
            }

            InOutParameters[index] = parameters;
        }
    }
}