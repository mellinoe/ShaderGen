namespace ShaderGen
{
    public class RWStructuredBuffer<T> where T : struct
    {
        public ref T this[int index]
        {
            get => throw new ShaderBuiltinException();
        }

        public ref T this[uint index]
        {
            get => throw new ShaderBuiltinException();
        }
    }
}
