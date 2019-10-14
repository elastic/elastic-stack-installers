using CommandLine;

namespace ElastiBuild.Options
{
    public class GlobalOptions
    {
        [Option("help", HelpText = "Shows this help screen as well as help for specific commands")]
        public bool IsHelp { get; set; }
    }
}
