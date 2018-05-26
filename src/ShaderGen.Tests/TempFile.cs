using System;
using System.IO;

namespace ShaderGen.Tests
{
    public class TempFile : IDisposable
    {
        public readonly string FilePath;

        public TempFile() : this(Path.GetTempFileName()) { }
        public TempFile(string path)
        {
            FilePath = path;
        }

        public static implicit operator string(TempFile tf) => tf.FilePath;

        public void Dispose()
        {
            File.Delete(FilePath);
        }
    }
}