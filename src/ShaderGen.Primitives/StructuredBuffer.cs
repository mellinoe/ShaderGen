namespace ShaderGen
{
    public class StructuredBuffer<T> where T : struct
    {
        public T this[int index] => throw new ShaderBuiltinException();
    }
}
