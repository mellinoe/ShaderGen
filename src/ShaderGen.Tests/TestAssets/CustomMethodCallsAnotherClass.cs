using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public static class AnotherClass
    {
        public static float CustomAbs(float v)
        {
            return Abs(v);
        }
    }
}
