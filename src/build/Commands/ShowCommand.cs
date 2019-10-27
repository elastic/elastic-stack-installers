using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Elastic.Installer;

namespace ElastiBuild.Commands
{
    [Verb("show", HelpText = "Show products that we can build")]
    public class ShowCommand : IElastiBuildCommand
    {
        public Task RunAsync(BuildContext ctx)
        {
            foreach (var target in ctx.Config.TargetNames)
                Console.WriteLine(target);

            return Task.CompletedTask;
        }

        [Usage(ApplicationAlias = MagicStrings.AppAlias)]
        public static IEnumerable<Example> Examples => new List<Example>()
        {
            new Example(Environment.NewLine +
                "Show products that we can build",
                new ShowCommand())
        };
    }
}
