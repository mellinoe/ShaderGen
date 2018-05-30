using System;

namespace ShaderGen.Tests.Tools
{
    /// <summary>
    /// The features that are available for the <see cref="ToolChain"/> on the current environment.
    /// </summary>
    /// <remarks><para>If a <see cref="ToolChain"/> does not 'require' the feature, it is considered 'availalbe', e.g. OpenGL compilation.
    /// In such a circumstance invoking the feature will do nothing.</para></remarks>
    [Flags]
    public enum ToolFeatures
    {

        /// <summary>
        /// The <see cref="ToolChain"/> can transpile C# to shader code.
        /// </summary>
        Transpilation = 1 << 0,

        /// <summary>
        /// The <see cref="ToolChain"/> can compile shader code.
        /// </summary>
        Compilation = 1 << 1,

        /// <summary>
        /// The <see cref="ToolChain"/> can create a headless graphics device.
        /// </summary>
        HeadlessGraphicsDevice = 1 << 2,

        /// <summary>
        /// The <see cref="ToolChain"/> can create a windowed graphics device.
        /// </summary>
        WindowedGraphicsDevice = 1 << 3,

        /*
         * Combination Flags
         */
        /// <summary>
        /// No features are available.
        /// </summary>
        None = 0,

        /// <summary>
        /// The <see cref="ToolChain"/> can transpile C# to shader and then compile it.
        /// </summary>
        ToCompiled = Transpilation | Compilation,


        /// <summary>
        /// The <see cref="ToolChain"/> can transpile C# to shader, compile it and then create a headless graphics device to run it.
        /// </summary>
        ToHeadless = ToCompiled | HeadlessGraphicsDevice,

        /// <summary>
        /// The <see cref="ToolChain"/> can transpile C# to shader, compile it and then create a windowed graphics device to run it.
        /// </summary>
        ToWindowed = ToCompiled | WindowedGraphicsDevice,

        /// <summary>
        /// All features are available.
        /// </summary>
        All = Transpilation | Compilation | HeadlessGraphicsDevice | WindowedGraphicsDevice

    }
}