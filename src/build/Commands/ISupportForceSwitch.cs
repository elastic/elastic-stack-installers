using CommandLine;

namespace ElastiBuild.Commands
{
    public interface ISupportForceSwitch
    {
        [Option("force", Default = false, HelpText = "Skip sanity checking where appropriate.")]
        bool ForceSwitch { get; set; }
    }
}
