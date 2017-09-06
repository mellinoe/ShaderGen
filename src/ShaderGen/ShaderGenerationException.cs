using System;

namespace ShaderGen
{
    public class ShaderGenerationException : Exception
    {
        public ShaderGenerationException()
        {
        }

        public ShaderGenerationException(string message) : base(message)
        {
        }

        public ShaderGenerationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
