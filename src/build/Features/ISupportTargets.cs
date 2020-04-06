using System.Collections.Generic;
using CommandLine;

namespace ElastiBuild.Commands
{
    namespace Resources
    {
        public static class ISupportTargets
        {
            public static string Targets => "[PRODUCT [PRODUCT [...]]]";
        }
    }

    public interface ISupportTargets
    {
        [Value(0, MetaName = "PRODUCT", HelpText = nameof(Targets), ResourceType = typeof(Resources.ISupportTargets))]
        IEnumerable<string> Targets { get; set; }
    }

    public interface ISupportRequiredTargets
    {
        [Value(0, Required = true, MetaName = "PRODUCT", HelpText = nameof(Targets), ResourceType = typeof(Resources.ISupportTargets))]
        IEnumerable<string> Targets { get; set; }
    }
}
