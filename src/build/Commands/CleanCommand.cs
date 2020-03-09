using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;

namespace ElastiBuild.Commands
{
    [Verb("clean", HelpText = "Clean downloaded .zip, temporary and output files")]
    public class CleanCommand
        : IElastiBuildCommand
    //, ISupportTargets
    {
        public IEnumerable<string> Targets { get; set; }

        [Option("all", Default = false, HelpText = "Clean entire bin directory: downloaded products and built msi packages")]
        public bool WxsOnly { get; set; }

        public async Task RunAsync()
        {
            var ctx = BuildContext.Default;

            var bt = new Bullseye.Targets();

            if (WxsOnly)
            {
                bt.Add("Clean#all", () =>
                {
                    try
                    { Directory.Delete(ctx.BinDir, true); }
                    catch { };
                });

                bt.Add("Clean", Bullseye.Targets.DependsOn("Clean#all"));
            }
            else
            {
                bt.Add("Clean#CompilerDir", () =>
                {
                    try
                    { Directory.Delete(ctx.CompilerDir, true); }
                    catch { };
                });

                bt.Add("Clean#OutDir", () =>
                {
                    try
                    { Directory.Delete(ctx.OutDir, true); }
                    catch { };
                });

                bt.Add("Clean",
                    Bullseye.Targets.DependsOn("Clean#CompilerDir", "Clean#OutDir"));
            }

            try
            {
                await bt.RunWithoutExitingAsync("Clean".Split());
            }
            catch
            {
                // We swallow exceptions here, BullsEye prints them
                // TODO: use overload "messageOnly"
            }

            // TODO: Add support for Targets
        }
    }
}
