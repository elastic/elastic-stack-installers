using CommandLine;

namespace ElastiBuild.Commands
{
    namespace Resources
    {
        public static class ISupportContainerId
        {
            public static string ContainerId =>
                "Container Id. One of: "
                + "Branch, eg. 7.6, 7.x; "
                + "Version, eg. 7.6.0-SNAPSHOT, 7.6.1; "
                + "Alias, eg. 7.6, 7.x-SNAPSHOT; ";
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
