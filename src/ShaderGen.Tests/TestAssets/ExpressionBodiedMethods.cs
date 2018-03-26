using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class ExpressionBodiedMethods
    {
        [FragmentShader]
        public Vector4 ExpressionBodyWithReturn()
            => new Vector4(0f, 0f, 0f, 1f);

        [FragmentShader]
        public void ExpressionBodyWithoutReturn()
            => new Vector4(0f, 0f, 0f, 1f);
    }
}
