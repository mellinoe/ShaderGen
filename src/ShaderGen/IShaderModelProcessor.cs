namespace ShaderGen
{
    public interface IShaderModelProcessor
    {
        string UserArgs { get; set; }
        void ProcessShaderModel(ShaderModel model);
    }
}
