using CommandLine;

namespace ElastiBuild.Commands
{
    namespace Resources
    {
        public static class ISupportArchitecture
        {
            public static string Architecture =>
                "Architecture. x86_64 or arm64. Default is x86_64";
        }
    }

    public interface ISupportArchitecture
    {
        [Option("arch", Default = "x86_64", HelpText = nameof(Architecture), ResourceType = typeof(Resources.ISupportArchitecture))]
        string Architecture { get; set; }
    }
}
