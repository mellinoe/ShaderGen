using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class Enums
    {
        public struct MaterialConstantsType
        {
            public LightingType Lighting;
            public MaterialType Material;
        }

        public MaterialConstantsType MaterialConstants;

        [FragmentShader]
        public Vector4 FS()
        {
            var result = DoMaterialStuff(MaterialConstants.Material);
            var result2 = DoMaterialStuff(MaterialType.Metal);

            var materialType = MaterialType.Plastic;
            materialType = MaterialType.Plastic;
            DoMaterialStuff(materialType);

            switch (MaterialConstants.Lighting)
            {
                case LightingType.Lambert:
                    return Vector4.Zero;

                case LightingType.Phong:
                    return Vector4.Zero;

                case LightingType.BlinnPhong:
                default:
                    return Vector4.Zero;
            }
        }

        private Vector4 DoMaterialStuff(MaterialType materialType)
        {
            if (materialType == MaterialType.Leather)
            {
                return Vector4.One;
            }
            else
            {
                return Vector4.Zero;
            }
        }

        public enum LightingType
        {
            Lambert,
            Phong,
            BlinnPhong
        }

        public enum MaterialType : uint
        {
            Plastic,
            Leather,
            Metal
        }
    }
}
