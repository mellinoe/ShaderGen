using ShaderGen;
using System.Numerics;

namespace TestShaders
{
    public class BuiltInVariables
    {
        [VertexShader]
        SystemPosition4 VS()
        {
            uint vertexID = ShaderBuiltins.VertexID;
            uint instanceID = ShaderBuiltins.InstanceID;

            SystemPosition4 output;
            output.Position = new Vector4(vertexID, instanceID, 0, 1);
            return output;
        }
    }
}
