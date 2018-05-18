namespace ShaderGen
{
    public class AtomicBufferInt32
    {
        public int this[int index] => throw new ShaderBuiltinException();
        public int this[uint index] => throw new ShaderBuiltinException();
    }
}
