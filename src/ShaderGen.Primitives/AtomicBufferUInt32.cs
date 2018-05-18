namespace ShaderGen
{
    public class AtomicBufferUInt32
    {
        public uint this[int index] => throw new ShaderBuiltinException();
        public uint this[uint index] => throw new ShaderBuiltinException();
    }
}
