using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Elastic.Installer;

namespace ElastiBuild.Commands
{
    [Verb("show", HelpText = "Show products that we can build")]
    public class ShowCommand
        : IElastiBuildCommand
        , ISupportVerboseSwitch
    {
        public bool VerboseSwitch { get; set; }

        public async Task RunAsync()
        {
            foreach (var kvProduct in BuildContext.Default.Config.Products)
            {
                Console.WriteLine(kvProduct.Key + (VerboseSwitch ? ":" : string.Empty));

                if (!VerboseSwitch)
                    continue;

                var offset = "  ";
                var item = kvProduct.Value;

                Console.WriteLine(offset + nameof(item.PublishedName) + ": " + item.PublishedName);
                Console.WriteLine(offset + nameof(item.Description) + ": " + item.Description);
                Console.WriteLine(offset + nameof(item.PublishedUrl) + ": " + item.PublishedUrl);
                Console.WriteLine(offset + nameof(item.IsWindowsService) + ": " + item.IsWindowsService);

                Console.WriteLine();
            }

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
