using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using static Bullseye.Targets;

namespace ElastiBuild
{
    partial class Program
    {
        public static void CreateBuildTargeTree()
        {
            Target("all",
                DependsOn(
                    nameof(BuildTarget.Winlogbeat)
                ));

            Target(nameof(BuildTarget.Winlogbeat), 
                () => { });
        }

        //async Task BuildTaskSetup(BuildContext ctx_)
        //{
        //    await Task.Yield();

        //    Target("all",
        //        DependsOn(
        //            nameof(BuildTarget.Winlogbeat)
        //            ));

        //    Target(nameof(BuildTarget.Winlogbeat),
        //        DependsOn(
        //            nameof(BuildTarget.Resolve)),
        //        BuildTarget.Winlogbeat.Create(ctx_).Build);

        //    Target(nameof(BuildTarget.Resolve),
        //        BuildTarget.Resolve.Create(ctx_).Build);
        //}
    }
}
