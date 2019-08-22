using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

using static Bullseye.Targets;

namespace ElastiBuild
{
    partial class Program
    {
        // NOTE: In BullsEye v3.0 target names case sensitivity will go away.
        //       At this time .ToLower() and .Select() calls shuld be removed.
        async Task BuildTaskSetup(CommandLine.Parser parser_, BuildContext ctx_)
        {
            Target(
                "default",
                DependsOn(nameof(BuildTarget.Help).ToLower()));

            Target(
                nameof(BuildTarget.Help).ToLower(),
                BuildTarget.Help.Create(ctx_).Build);

            Target(
                nameof(BuildTarget.Clean).ToLower(),
                BuildTarget.Clean.Create(ctx_).Build);

            Target(
                nameof(BuildTarget.ResolveArtifact).ToLower(),
                BuildTarget.ResolveArtifact.Create(ctx_).Build);

            Target(
                nameof(BuildTarget.WinlogBeat).ToLower(),
                DependsOn(
                    //nameof(BuildTarget.Clean).ToLower(),
                    nameof(BuildTarget.ResolveArtifact).ToLower()),
                BuildTarget.WinlogBeat.Create(ctx_).Build);

            Target(
                "all",
                DependsOn(
                    nameof(BuildTarget.WinlogBeat).ToLower()
                    ));

            IBullseyeOptions qq = ctx_.Options;
            var ti = qq.GetType().GetProperties().SelectMany(x => x.GetCustomAttributes(typeof(OptionAttribute), false));

            var opts = parser_.FormatCommandLine((IBullseyeOptions) ctx_.Options);
            var tgts = ctx_.Options.BuildTargets ?? Enumerable.Empty<string>();
            var args = tgts.Concat(opts.Split(' '));

            await RunTargetsAndExitAsync(args);
        }
    }
}
