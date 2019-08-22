using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ElastiBuild
{
    public class BuildContext
    {
        public BuildContext(CmdLineOptions opts_)
        {
            Options = opts_;
        }

        public CmdLineOptions Options { get; private set; }

        // TODO: remember these
        public string SrcDir => Path.Combine(Options.BuildRoot, "src");
        public string BinDir => Path.Combine(Options.BuildRoot, "bin");
        public string InDir => Path.Combine(BinDir, "in");
        public string OutDir => Path.Combine(BinDir, "out");
    }
}
