using System;

namespace ShaderGen.Tests.Attributes
{
    /// <summary>
    /// Enumeration of tools.
    /// </summary>
    [Flags]
    public enum Tool : byte
    {
        /// <summary>
        /// No tools are required.
        /// </summary>
        None = 0,
        /// <summary>
        /// The metal tools are required.
        /// </summary>
        Metal = 1,
        /// <summary>
        /// The GLS language validator is required.
        /// </summary>
        GlsLangValidator = 2,
        /// <summary>
        /// The Direct 3D shader compiler is required.
        /// </summary>
        Fxc = 4,
        /// <summary>
        /// All tools are required.
        /// </summary>
        All = Metal | GlsLangValidator | Fxc
    }
}