using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ResourceAttribute : Attribute
    {
    }
}
