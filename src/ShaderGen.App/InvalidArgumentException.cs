using System;

namespace ShaderGen.App {
    public class InvalidArgumentException : Exception {
        public InvalidArgumentException(string message) : base(message){}
    }
}