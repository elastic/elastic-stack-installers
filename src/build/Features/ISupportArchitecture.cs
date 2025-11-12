using CommandLine;

namespace ElastiBuild.Commands
{
    namespace Resources
    {
        public static class ISupportArchitecture
        {
            public static string Architecture =>
                "Architecture. x86_64 or arm64";
        }
    }

    public interface ISupportArchitecture
    {
        // TODO: arch latest
        [Option("arch", Default = "x86_64", HelpText = nameof(Architecture), ResourceType = typeof(Resources.ISupportArchitecture))]
        string Architecture { get; set; }
    }

/*
    public interface ISupportRequiredArchitecture
    {
        // TODO: arch latest
        [Option("arch", Required = true, HelpText = nameof(Architecture), ResourceType = typeof(Resources.ISupportArchitecture))]
        string Architecture { get; set; }
    }
*/
}
