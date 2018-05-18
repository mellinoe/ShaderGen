namespace ShaderGen.Tests.Tools
{
    public class ToolResult
    {
        public readonly int ExitCode;
        public readonly string StdOut;
        public readonly string StdError;

        public ToolResult(int exitCode, string stdOut, string stdError)
        {
            ExitCode = exitCode;
            StdOut = stdOut;
            StdError = stdError;
        }
    }
}
