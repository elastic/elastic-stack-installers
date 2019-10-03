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
        static async Task Main(string[] args_)
        {
            await new Program().Run(args_);
        }

        async Task Run(string[] args_)
        {
#if DEBUG
            //args_ = "build --cid 7.x winlogbeat --bitness x64".Split();
            //Console.WriteLine("ARGS: " + string.Join(",", args_));
#endif

            var commands = typeof(Program).Assembly
                .GetTypes()
                .Where(x => x.GetCustomAttributes(typeof(VerbAttribute), true).Length > 0)
                .ToArray();

            Action<ParserSettings> parserCfg = cfg =>
            {
                cfg.AutoHelp = true;
                cfg.CaseSensitive = false;
                cfg.AutoVersion = false;
                cfg.IgnoreUnknownArguments = false;
                cfg.HelpWriter = null;
            };

            using var parser = new Parser(parserCfg);
            var result = parser.ParseArguments(args_, commands);

            var ctx = BuildContext.Create();

            await result.MapResult(
                async (IElastiBuildCommand cmd) => await cmd.RunAsync(ctx),
                async (errs) => await HandleErrorsAndShowHelp(result, commands));
        }

        Task HandleErrorsAndShowHelp(ParserResult<object> result_, Type[] commands_)
        {
            SentenceBuilder.Factory = () => new TweakedSentenceBuilder();

            using var parser = new Parser(cfg =>
            {
                cfg.IgnoreUnknownArguments = true;
                cfg.AutoHelp = false;
                cfg.AutoVersion = false;
                cfg.HelpWriter = null;
            });

            var result = parser.ParseArguments<GlobalOptions>("".Split(' '));

            HelpText htGlobals = new HelpText("ElastiBuild v1.0.0", "Copyright (c) 2019, Elastic.co")
            {
                AdditionalNewLineAfterOption = false,
                AutoHelp = false,
                AutoVersion = false,
                AddDashesToOption = true
            };

            htGlobals.AddPreOptionsLine(Environment.NewLine + "Global Flags:");
            htGlobals.AddOptions(result);
            Console.WriteLine(htGlobals.ToString());

            bool isGlobalHelp = result_.TypeInfo.Current == typeof(NullInstance);

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
                htVerbs.AddVerbs(commands_);

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
                htOptions.AddOptions(result_);

                text = htOptions.ToString();
                if (text.Length > 0)
                {
                    var cmdName = result_
                        .TypeInfo.Current
                        .GetCustomAttributes(typeof(VerbAttribute), true)
                        .Cast<VerbAttribute>()
                        .FirstOrDefault()
                        ?.Name ?? string.Empty;

                    Console.WriteLine(
                        $"{cmdName.ToUpper()} Flags:" + text);
                }

                text = HelpText.RenderUsageText(result_);
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
                    result_,
                    err => tsb.FormatError(err),
                    mex => tsb.FormatMutuallyExclusiveSetErrors(mex),
                    2);

            if (text.Length > 0)
                Console.WriteLine("Error(s):" + Environment.NewLine + text);

            return Task.CompletedTask;
        }
    }
}
