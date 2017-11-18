using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

namespace TestShaders
{
    public class BuiltInVariables
    {
        [VertexShader]
        SystemPosition4 VS()
        {
            uint vertexID = ShaderBuiltins.VertexID;
            uint instanceID = InstanceID;

            SystemPosition4 output;
            output.Position = new Vector4(vertexID, instanceID, ShaderBuiltins.VertexID, 1);
            return output;
        }
    }
}
