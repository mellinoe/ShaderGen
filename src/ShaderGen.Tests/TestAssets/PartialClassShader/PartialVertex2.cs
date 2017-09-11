using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public partial class PartialVertex
    {
        [Resource]
        public Texture2DResource Fourth;

        [Resource]
        public Matrix4x4 Fifth;
    }
}
