using static ShaderGen.Builtins;

namespace TestShaders
{
    public static class AnotherClass
    {
        public static float CustomAbs(float v)
        {
            return HelperMethod(v);
        }

        public static float HelperMethod(float v)
        {
            return Abs(v);
        }
    }
}
