using CommandLine;

namespace ElastiBuild.Commands
{
    namespace Resources
    {
        public static class ISupportContainerId
        {
            public static string ContainerId =>
                "Container Id. One of: "
                + "Branch, eg. 6.8, 7.x; "
                + "Version, eg. 6.8.3-SNAPSHOT, 7.4.0; "
                + "Alias, eg. 6.8, 7.x-SNAPSHOT; ";
        }
    }

    public interface ISupportContainerId
    {
        // TODO: cid latest
        [Option("cid", Default = null, HelpText = nameof(ContainerId), ResourceType = typeof(Resources.ISupportContainerId))]
        string ContainerId { get; set; }
    }

    public interface ISupportRequiredContainerId
    {
        // TODO: cid latest
        [Option("cid", Required = true, HelpText = nameof(ContainerId), ResourceType = typeof(Resources.ISupportContainerId))]
        string ContainerId { get; set; }
    }
}
