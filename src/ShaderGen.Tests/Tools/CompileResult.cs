using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace ShaderGen.Tests.Tools
{
    /// <summary>
    /// The result of a compilation tool execution.
    /// </summary>
    public class CompileResult
    {
        /// <summary>
        /// The shader code passed to the tool.
        /// </summary>
        public readonly string Code;

        /// <summary>
        /// The exit code
        /// </summary>
        public readonly int ExitCode;

        /// <summary>
        /// The standard out.
        /// </summary>
        public readonly string StdOut;

        /// <summary>
        /// The standard error.
        /// </summary>
        public readonly string StdError;

        /// <summary>
        /// The compiled output (if any); otherwise <see langword="null"/>.
        /// </summary>
        public readonly byte[] CompiledOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompileResult" /> class.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="exitCode">The exit code.</param>
        /// <param name="stdOut">The standard out.</param>
        /// <param name="stdError">The standard error.</param>
        /// <param name="outputBytes">The output bytes.</param>
        public CompileResult(string code, int exitCode, string stdOut, string stdError, byte[] outputBytes)
        {
            Code = code ?? string.Empty;
            ExitCode = exitCode;
            StdOut = stdOut;
            StdError = stdError;
            CompiledOutput = outputBytes;
        }

        /// <summary>
        /// Gets a value indicating whether this instance has an error.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has an error; otherwise, <c>false</c>.
        /// </value>
        public bool HasError => ExitCode != 0;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            /*
             * Build informative error message
             */
            StringBuilder builder = new StringBuilder()
                .Append($"Compilation ")
                .Append(HasError ? $"failed with exit code {ExitCode}." : "suceeded.");

            if (!string.IsNullOrWhiteSpace(StdOut))
                builder.AppendLine().AppendLine("Output: ").Append(StdOut);
            if (!string.IsNullOrWhiteSpace(StdError))
                builder.AppendLine().AppendLine("Error: ").Append(StdError);

            builder.AppendLine().AppendLine("Shader Code:");
            int lines = 0;
            foreach (string line in Code.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None))
            {
                lines++;
                if (lines > 999)
                {
                    builder.AppendLine("... (line count exceeded)");
                    continue;
                }

                builder.Append($"{lines,3}: ").AppendLine(line);
            }

            return builder.ToString();
        }
    }
}
