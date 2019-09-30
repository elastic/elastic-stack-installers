using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace ElastiBuild.Commands
{
    public interface IElastiBuildCommand
    {
        Task RunAsync(BuildContext ctx_);
    }

    public interface ISupportTargets
    {
        [Value(0, Required = true, MetaName = "TARGETS", HelpText = "[TARGET [TARGET [...]]]")]
        IEnumerable<string> Targets { get; set; }
    }

    public interface ISupportContainerId
    {
        // TODO: arid latest
        [Option("cid",
            HelpText = "Container Id. One of: "
            + "Branch, eg. 6.8, 7.x; "
            + "Version, eg. 6.8.3-SNAPSHOT, 7.4.0; "
            + "Alias, eg. 6.8, 7.x-SNAPSHOT; "
        )]
        string ContainerId { get; set; }
    }

    public interface ISupportOssChoice
    {
        [Option("oss", Default = false, HelpText = "Show OSS artifacts")]
        bool ShowOss { get; set; }
    }

    public enum eBitness
    {
        both,
        x86,
        x64
    }

    public interface ISupportBitnessChoice
    {
        [Option("bitness", Default = eBitness.x64, HelpText = "Show artifacts of specific bitness: x86, x64"),]
        eBitness Bitness { get; set; }
    }
}
