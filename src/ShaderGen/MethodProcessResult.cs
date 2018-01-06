using System.Collections.Generic;

namespace ShaderGen
{
    public class MethodProcessResult
    {
        public string FullText { get; }
        public HashSet<ResourceDefinition> ResourcesUsed { get; set; }

        public MethodProcessResult(string fullText, HashSet<ResourceDefinition> resourcesUsed)
        {
            FullText = fullText;
            ResourcesUsed = resourcesUsed;
        }
    }
}
