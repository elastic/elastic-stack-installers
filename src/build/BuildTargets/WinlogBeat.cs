using System;
using System.IO;
using System.Threading.Tasks;

using static SimpleExec.Command;
using static Bullseye.Targets;

namespace ElastiBuild.BuildTarget
{
    // unused atm
    public class Winlogbeat : BuildTargetBase<Winlogbeat>
    {
        public async Task Build()
        {
            //await RunTargetsWithoutExitingAsync(nameof(ResolveArtifact), context: );

            // TODO: better command runner
            // TODO: check exit codes

            await Task.Yield();
        }
    }
}
