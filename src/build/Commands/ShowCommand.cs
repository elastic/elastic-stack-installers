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
        public async Task RunAsync()
        {
            foreach (var target in BuildContext.Default.Config.ProductNames)
                Console.WriteLine(target);

            await Task.CompletedTask;
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
