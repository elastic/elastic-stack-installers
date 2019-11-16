using CommandLine;

namespace ElastiBuild.Commands
{
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
