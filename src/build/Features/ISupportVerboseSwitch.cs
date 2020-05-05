using CommandLine;

namespace ElastiBuild.Commands
{
    public interface ISupportVerboseSwitch
    {
        [Option("verbose", Default = false, HelpText = "Try to produce more verbose output.")]
        bool VerboseSwitch { get; set; }
    }
}
