using System;

namespace ShaderGen
{
    public class ResourceSetAttribute : Attribute
    {
        public int Set { get; }

        public ResourceSetAttribute(int set)
        {
            Set = set;
        }
    }
}
