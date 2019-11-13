using System.Collections.Generic;
using System.Threading.Tasks;
using CommandLine;

namespace ElastiBuild.Commands
{
    public interface IElastiBuildCommand
    {
        Task RunAsync();
    }

    public interface ISupportTargets
    {
        [Value(0, MetaName = "PRODUCT", HelpText = "[PRODUCT [PRODUCT [...]]]")]
        IEnumerable<string> Targets { get; set; }
    }

    public interface ISupportRequiredTargets
    {
        [Value(0, Required = true, MetaName = "PRODUCT", HelpText = "[PRODUCT [PRODUCT [...]]]")]
        IEnumerable<string> Targets { get; set; }
    }

    public interface ISupportContainerId
    {
        // TODO: cid latest
        [Option("cid",
            HelpText = "Container Id. One of: "
            + "Branch, eg. 6.8, 7.x; "
            + "Version, eg. 6.8.3-SNAPSHOT, 7.4.0; "
            + "Alias, eg. 6.8, 7.x-SNAPSHOT; "
        )]
        string ContainerId { get; set; }
    }

    public interface ISupportRequiredContainerId
    {
        // TODO: cid latest
        [Option("cid", Required = true,
            HelpText = "Container Id. One of: "
            + "Branch, eg. 6.8, 7.x; "
            + "Version, eg. 6.8.3-SNAPSHOT, 7.4.0; "
            + "Alias, eg. 6.8, 7.x-SNAPSHOT; "
        )]
        string ContainerId { get; set; }
    }

    public interface ISupportOssSwitch
    {
        [Option("oss", Default = false, HelpText = "Show OSS packages")]
        bool ShowOss { get; set; }
    }

    public interface ISupportWxsOnlySwitch
    {
        [Option("wxs-only", HelpText = "Only generate .wxs file, skip building .msi")]
        bool WxsOnly { get; set; }
    }

    public enum eBitness
    {
        both,
        x86,
        x64
    }

    public interface ISupportBitnessChoice
    {
        [Option("bitness", Default = eBitness.x64, HelpText = "Show packages of specific bitness: x86, x64, both")]
        eBitness Bitness { get; set; }
    }
}
