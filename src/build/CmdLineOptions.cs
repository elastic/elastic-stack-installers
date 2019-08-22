using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandLine;

namespace ElastiBuild
{
    public interface IBullseyeOptions
    {
        [Option('t', "list-tree", HelpText = "List dependency tree")]
        bool ListTree { get; set; }
    }

    public class BullseyeOptions : IBullseyeOptions
    {
        public bool ListTree { get; set; }
    }

    public class CmdLineOptions : BullseyeOptions
    {
        [Value(0, Required = false)]
        public IEnumerable<string> BuildTargets { get; set; }

        [Option("build-root", Hidden = true)]
        public string BuildRoot { get; set; }

        [Option("skip-tests")]
        public bool SkipTests { get; set; }

        [Option("cert-file")]
        public string CertFilename { get; set; }

        [Option("cert-pass")]
        public string CertPassword { get; set; }

        public static CmdLineOptions Default { get; } = new CmdLineOptions();
    }
}
