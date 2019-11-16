using CommandLine;

namespace ElastiBuild.Commands
{
    public interface ISupportWxsOnlySwitch
    {
        [Option("wxs-only", Default = false, HelpText = "Only generate .wxs file, skip building .msi")]
        bool WxsOnly { get; set; }
    }
}
