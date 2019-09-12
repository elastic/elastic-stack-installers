using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using static Bullseye.Targets;
using ElastiBuild.Options;

namespace ElastiBuild.Commands
{ 
    [Verb("show", HelpText = "Show available build targets")]
    public class ShowCommand : IElastiBuildCommand
    {
        [Usage(ApplicationAlias = GlobalOptions.AppAlias)]
        public static IEnumerable<Example> Examples => new List<Example>()
        {
            new Example(Environment.NewLine +
                "Show available build targets",
                new ShowCommand())
        };

        public Task RunAsync(BuildContext ctx_)
        {
            Program.CreateBuildTargeTree();
            Console.WriteLine("Available build TARGETS:" + Environment.NewLine);
            return RunTargetsAndExitAsync("-t".Split());
        }
    }
}
