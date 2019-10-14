using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Elastic.Installer;
using ElastiBuild.Options;

namespace ElastiBuild.Commands
{ 
    [Verb("show", HelpText = "Show available build targets")]
    public class ShowCommand : IElastiBuildCommand
    {
        [Usage(ApplicationAlias = MagicStrings.AppAlias)]
        public static IEnumerable<Example> Examples => new List<Example>()
        {
            new Example(Environment.NewLine +
                "Show available build targets",
                new ShowCommand())
        };

        public Task RunAsync(BuildContext ctx_)
        {
            foreach (var target in ctx_.Config.TargetNames)
                Console.WriteLine(target);

            return Task.CompletedTask;
        }
    }
}
