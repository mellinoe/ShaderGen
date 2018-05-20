using System.Numerics;
using System.Runtime.InteropServices;

namespace TestShaders
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ComputeShaderParameters
    {
        public /*readonly*/ Matrix4x4 P1Matrix;
        public /*readonly*/ Matrix4x4 P2Matrix;

        public /*readonly*/ Vector4 P1Vector4;
        public /*readonly*/ Vector4 P2Vector4;
        public /*readonly*/ Vector4 P3Vector4;
        public /*readonly*/ Vector4 OutVector4;

        public /*readonly*/ Vector3 P1Vector3;
        public /*readonly*/ float P1Float;
        public /*readonly*/ Vector3 P2Vector3;
        public /*readonly*/ float P2Float;
        public /*readonly*/ Vector3 P3Vector3;
        public /*readonly*/ float P3Float;
        public /*readonly*/ Vector3 OutVector3;
        public /*readonly*/ float OutFloat;

        public /*readonly*/ Vector2 P1Vector2;
        public /*readonly*/ Vector2 P2Vector2;
        public /*readonly*/ Vector2 P3Vector2;
        public /*readonly*/ Vector2 OutVector2;


        public /*readonly*/ uint OutFloatSet;
        public /*readonly*/ uint OutVector2Set;
        public /*readonly*/ uint OutVector3Set;
        public /*readonly*/ uint OutVector4Set;

        /*
        public ComputeShaderParameters(
            float p1Float = default(float),
            float p2Float = default(float),
            float p3Float = default(float),
            Vector2 p1Vector2 = default(Vector2),
            Vector2 p2Vector2 = default(Vector2),
            Vector2 p3Vector2 = default(Vector2),
            Vector3 p1Vector3 = default(Vector3),
            Vector3 p2Vector3 = default(Vector3),
            Vector3 p3Vector3 = default(Vector3),
            Vector4 p1Vector4 = default(Vector4),
            Vector4 p2Vector4 = default(Vector4),
            Vector4 p3Vector4 = default(Vector4),
            Matrix4x4 p1Matrix = default(Matrix4x4),
            Matrix4x4 p2Matrix = default(Matrix4x4))
            : this(
                p1Float,
                p2Float,
                p3Float,
                p1Vector2,
                p2Vector2,
                p3Vector2,
                p1Vector3,
                p2Vector3,
                p3Vector3,
                p1Vector4,
                p2Vector4,
                p3Vector4,
                p1Matrix,
                p2Matrix,
                default(float),
                default(Vector2),
                default(Vector3),
                default(Vector4),
                false,
                false,
                false,
                false)
        {
        }

        private ComputeShaderParameters(
            ComputeShaderParameters parameters,
            float outFloat,
            Vector2 outVector2,
            Vector3 outVector3,
            Vector4 outVector4,
            bool outFloatSet,
            bool outVector2Set,
            bool outVector3Set,
            bool outVector4Set)
            : this(
                parameters.P1Float,
                parameters.P2Float,
                parameters.P3Float,
                parameters.P1Vector2,
                parameters.P2Vector2,
                parameters.P3Vector2,
                parameters.P1Vector3,
                parameters.P2Vector3,
                parameters.P3Vector3,
                parameters.P1Vector4,
                parameters.P2Vector4,
                parameters.P3Vector4,
                parameters.P1Matrix,
                parameters.P2Matrix,
                outFloat,
                outVector2,
                outVector3,
                outVector4,
                outFloatSet,
                outVector2Set,
                outVector3Set,
                outVector4Set)
        {
        }

        private ComputeShaderParameters(
            float p1Float,
            float p2Float,
            float p3Float,
            Vector2 p1Vector2,
            Vector2 p2Vector2,
            Vector2 p3Vector2,
            Vector3 p1Vector3,
            Vector3 p2Vector3,
            Vector3 p3Vector3,
            Vector4 p1Vector4,
            Vector4 p2Vector4,
            Vector4 p3Vector4,
            Matrix4x4 p1Matrix,
            Matrix4x4 p2Matrix,
            float outFloat,
            Vector2 outVector2,
            Vector3 outVector3,
            Vector4 outVector4,
            bool outFloatSet,
            bool outVector2Set,
            bool outVector3Set,
            bool outVector4Set)
        {
            P1Float = p1Float;
            P2Float = p2Float;
            P3Float = p3Float;

            P1Vector2 = p1Vector2;
            P2Vector2 = p2Vector2;
            P3Vector2 = p3Vector2;

            P1Vector3 = p1Vector3;
            P2Vector3 = p2Vector3;
            P3Vector3 = p3Vector3;

            P1Vector4 = p1Vector4;
            P2Vector4 = p2Vector4;
            P3Vector4 = p3Vector4;

            P1Matrix = p1Matrix;
            P2Matrix = p2Matrix;

            OutFloat = outFloat;
            OutVector2 = outVector2;
            OutVector3 = outVector3;
            OutVector4 = outVector4;

            OutFloatSet = outFloatSet;
            OutVector2Set = outVector2Set;
            OutVector3Set = outVector3Set;
            OutVector4Set = outVector4Set;
        }

        private ComputeShaderParameters(
            ComputeShaderParameters parameters,
            float outFloat,
            Vector2 outVector2,
            Vector3 outVector3,
            Vector4 outVector4,
            bool outFloatSet,
            bool outVector2Set,
            bool outVector3Set,
            bool outVector4Set)
        {
            P1Float = parameters.P1Float;
            P2Float = parameters.P2Float;
            P3Float = parameters.P3Float;

            P1Vector2 = parameters.P1Vector2;
            P2Vector2 = parameters.P2Vector2;
            P3Vector2 = parameters.P3Vector2;

            P1Vector3 = parameters.P1Vector3;
            P2Vector3 = parameters.P2Vector3;
            P3Vector3 = parameters.P3Vector3;

            P1Vector4 = parameters.P1Vector4;
            P2Vector4 = parameters.P2Vector4;
            P3Vector4 = parameters.P3Vector4;

            P1Matrix = parameters.P1Matrix;
            P2Matrix = parameters.P2Matrix;

            OutFloat = outFloat;
            OutVector2 = outVector2;
            OutVector3 = outVector3;
            OutVector4 = outVector4;

            OutFloatSet = outFloatSet;
            OutVector2Set = outVector2Set;
            OutVector3Set = outVector3Set;
            OutVector4Set = outVector4Set;
        }

        public ComputeShaderParameters Set(float outFloat)
            => new ComputeShaderParameters(this, outFloat, default(Vector2), default(Vector3), default(Vector4),
                true, false, false, false);

        /*
        public ComputeShaderParameters Set(Vector2 outVector2)
            => new ComputeShaderParameters(this, default(float), outVector2, default(Vector3), default(Vector4),
                false, true, false, false);

        public ComputeShaderParameters Set(Vector3 outVector3)
            => new ComputeShaderParameters(this, default(float), default(Vector2), outVector3, default(Vector4),
                false, false, true, false);

        public ComputeShaderParameters Set(Vector4 outVector4)
            => new ComputeShaderParameters(this, default(float), default(Vector2), default(Vector3), outVector4,
                false, false, false, true);
                */
    }
}