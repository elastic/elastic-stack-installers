using System;
using System.IO;
using System.Threading.Tasks;

using static SimpleExec.Command;

namespace ElastiBuild.BuildTarget
{
    public class WinlogBeat : BuildTargetBase<WinlogBeat>
    {
        public async Task Build()
        {
            await RunAsync(
                "dotnet", "msbuild \"" +
                Path.Combine(Context.SrcDir, "installer", "WinlogBeat") +
                "\" -nr:false -t:Build -p:Configuration=Release");

            await RunAsync(
                Path.Combine(Context.SrcDir, "installer", "WinlogBeat", "bin", "Release", "WinlogBeat-compiler.exe"),
                "--package-dir=winlogbeat-7.3.0-windows-x86_64", 
                Context.InDir);
        }
    }
}
