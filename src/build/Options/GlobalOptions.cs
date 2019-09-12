using CommandLine;

namespace ElastiBuild.Options
{
    public class GlobalOptions
    {
        public const string AppAlias = "./build";

        [Option("help", HelpText = "Shows this help screen as well as help for specific commands")]
        public bool IsHelp { get; set; }

        //[Option("build-root", Hidden = true, HelpText = "Directory with .buildroot file")]
        //public string BuildRoot { get; set; }
    }
}
