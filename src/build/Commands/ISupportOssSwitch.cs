using CommandLine;

namespace ElastiBuild.Commands
{
    public interface ISupportOssSwitch
    {
        [Option("oss", Default = false, HelpText = "Show OSS packages")]
        bool ShowOss { get; set; }
    }
}
