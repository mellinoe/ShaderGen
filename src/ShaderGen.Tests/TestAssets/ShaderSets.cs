using ShaderGen;

[assembly: ShaderSet("VertexAndFragment", "TestShaders.VertexAndFragment.VS", "TestShaders.VertexAndFragment.FS")]
[assembly: ShaderSet("VertexOnly", "TestShaders.TestVertexShader.VS", null)]
[assembly: ShaderSet("FragmentOnly", null, "TestShaders.VertexAndFragment.FS")]
