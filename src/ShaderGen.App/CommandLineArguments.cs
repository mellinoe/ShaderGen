using System.CommandLine;

namespace ShaderGen.App {
    public class CommandLineArguments {
        public CommandLineArguments(string[] args) {
            ProcessArguments(args);
        }

        protected void ProcessArguments(string[] args) {
            for (var i = 0; i < args.Length; i++)
            {
                args[i] = args[i].Replace("\\\\", "\\");
            }
            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.DefineOption("ref", ref ReferenceItemsResponsePath, true, "The semicolon-separated list of references to compile against.");
                syntax.DefineOption("src", ref CompileItemsResponsePath, true, "The semicolon-separated list of source files to compile.");
                syntax.DefineOption("out", ref OutputPath, true, "The output path for the generated shaders.");
                syntax.DefineOption("genlist", ref GenListFilePath, true, "The output file to store the list of generated files.");
                syntax.DefineOption("listall", ref ListAllFiles, false, "Forces all generated files to be listed in the list file. By default, only bytecode files will be listed and not the original shader code.");
                syntax.DefineOption("processor", ref ProcessorPath, false, "The path of an assembly containing IShaderSetProcessor types to be used to post-process GeneratedShaderSet objects.");
                syntax.DefineOption("processorargs", ref ProcessorArgs, false, "Custom information passed to IShaderSetProcessor.");
            });

            ReferenceItemsResponsePath = NormalizePath(ReferenceItemsResponsePath);
            CompileItemsResponsePath = NormalizePath(CompileItemsResponsePath);
            OutputPath = NormalizePath(OutputPath);
            GenListFilePath = NormalizePath(GenListFilePath);
            ProcessorPath = NormalizePath(ProcessorPath);
        }
        
        private static string NormalizePath(string path) {
            return path?.Trim();
        }

        public string ReferenceItemsResponsePath;
        public string CompileItemsResponsePath;
        public string OutputPath;
        public string GenListFilePath;
        public bool ListAllFiles;
        public string ProcessorPath;
        public string ProcessorArgs;
    }
}