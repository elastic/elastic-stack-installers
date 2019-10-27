using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Elastic.Installer;

namespace ElastiBuild.Commands
{
    [Verb("clean", HelpText = "Clean downloaded .zip, temporary and output files")]
    public class CleanCommand
        : IElastiBuildCommand
    {
        public Task RunAsync(BuildContext ctx)
        {
            Directory.Delete(ctx.BinDir, true);

            // TODO: Add support for Targets

            return Task.CompletedTask;
        }
    }
}
