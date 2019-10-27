using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Elastic.Installer;
using ElastiBuild.Commands;
using ElastiBuild.Infra;
using ElastiBuild.Options;

namespace ElastiBuild
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            await new Program().Run(args);
        }

        async Task Run(string[] args)
        {
#if DEBUG
            //args = "build --cid 7.x winlogbeat --bitness x64".Split();
            //Console.WriteLine("ARGS: " + string.Join(",", args));
#endif

            var commands = typeof(Program)
                .Assembly.GetTypes()
                .Where(x => x.GetCustomAttributes(typeof(VerbAttribute), inherit: true).Length > 0)
                .ToArray();

            using var parser = new Parser(cfg =>
            {
                cfg.AutoHelp = true;
                cfg.CaseSensitive = false;
                cfg.AutoVersion = false;
                cfg.IgnoreUnknownArguments = false;
                cfg.HelpWriter = null;
            });

            var ctx = new BuildContext();

            var result = parser.ParseArguments(args, commands);
            await result.MapResult(
                async (IElastiBuildCommand cmd) => await cmd.RunAsync(ctx),
                async (errs) => await HandleErrorsAndShowHelp(result, commands));
        }

        Task HandleErrorsAndShowHelp(ParserResult<object> parserResult, Type[] commands)
        {
            SentenceBuilder.Factory = () => new TweakedSentenceBuilder();

            using var parser = new Parser(cfg =>
            {
                cfg.IgnoreUnknownArguments = true;
                cfg.AutoHelp = false;
                cfg.AutoVersion = false;
                cfg.HelpWriter = null;
            });

            var result = parser.ParseArguments<GlobalOptions>(string.Empty.Split(' '));

            HelpText htGlobals = new HelpText("ElastiBuild v1.0.0", "Copyright (c) 2019, https://elastic.co")
            {
                AdditionalNewLineAfterOption = false,
                AutoHelp = false,
                AutoVersion = false,
                AddDashesToOption = true
            };

            htGlobals.AddPreOptionsLine(Environment.NewLine + "Global Flags:");
            htGlobals.AddOptions(result);
            Console.WriteLine(htGlobals.ToString());

            bool isGlobalHelp = parserResult.TypeInfo.Current == typeof(NullInstance);

            if (isGlobalHelp)
            {
                var htVerbs = new HelpText()
                {
                    AddDashesToOption = false,
                    AutoHelp = false,
                    AutoVersion = false,
                    AdditionalNewLineAfterOption = false,
                };

                htVerbs.AddPreOptionsLine("Available Commands:");
                htVerbs.AddVerbs(commands);

                Console.WriteLine(htVerbs.ToString());
            }

            var htOptions = new HelpText(string.Empty, string.Empty)
            {
                AddDashesToOption = true,
                AutoHelp = false,
                AutoVersion = false
            };

            string text = string.Empty;

            if (!isGlobalHelp)
            {
                htOptions.AddOptions(parserResult);

                text = htOptions.ToString();
                if (text.Length > 0)
                {
                    var cmdName = parserResult
                        .TypeInfo.Current
                        .GetCustomAttributes(typeof(VerbAttribute), true)
                        .Cast<VerbAttribute>()
                        .FirstOrDefault()
                        ?.Name ?? throw new Exception("Something went horribly wrong. Command name is empty.");

                    Console.WriteLine(
                        $"{cmdName.ToUpper()} Flags:" + text);
                }

                text = HelpText.RenderUsageText(parserResult);
                if (text.Length > 0)
                {
                    Console.WriteLine(
                        "Usage Examples:" +
                        Environment.NewLine +
                        text +
                        Environment.NewLine);
                }
            };

            var tsb = SentenceBuilder.Factory();
            text =
                HelpText.RenderParsingErrorsText(
                    parserResult,
                    err => tsb.FormatError(err),
                    mex => tsb.FormatMutuallyExclusiveSetErrors(mex),
                    2);

            if (text.Length > 0)
                Console.WriteLine("Error(s):" + Environment.NewLine + text);

            return Task.CompletedTask;
        }
    }
}
