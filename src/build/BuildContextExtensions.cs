using System;
using Elastic.Installer;

namespace ElastiBuild
{
    public static class BuildContextExtensions
    {
        public static ArtifactPackage GetArtifactPackage(this BuildContext ctx)
        {
            if (ctx.Items.TryGetValue(nameof(ArtifactPackage), out var obj) && obj is ArtifactPackage ap)
                return ap;

            throw new Exception(
                $"Unable to get {nameof(ArtifactPackage)} instance from " +
                $"{nameof(BuildContext)}.{nameof(BuildContext.Items)}");
        }

        public static void UseArtifactPackage(this BuildContext ctx, ArtifactPackage ap)
        {
            ctx.Items.Add(nameof(ArtifactPackage), ap);
        }
    }
}
