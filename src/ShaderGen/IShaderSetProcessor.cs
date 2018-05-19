namespace ShaderGen
{
    public interface IShaderSetProcessor
    {
        string UserArgs { get; set; }
        void ProcessShaderSet(ShaderSetProcessorInput input);
    }
}
