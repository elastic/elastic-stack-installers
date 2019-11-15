using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using Elastic.Installer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ElastiBuild.Infra
{
    public class ArtifactsApi
    {
        public static Uri BaseAddress { get; } = new Uri(MagicStrings.ArtifactsApi.BaseAddress);

        public static async Task<IEnumerable<ArtifactContainer>> ListNamedContainers()
        {
            using var http = new HttpClient()
            {
                BaseAddress = BaseAddress,
                Timeout = TimeSpan.FromMilliseconds(3000)
            };

            var namedItems = new List<ArtifactContainer>();

            using (var stm = await http.GetStreamAsync(MagicStrings.ArtifactsApi.Branches))
            using (var sr = new StreamReader(stm))
            using (var jtr = new JsonTextReader(sr))
            {
                var js = new JsonSerializer();
                var data = js.Deserialize<JToken>(jtr);

                foreach (var itm in data[MagicStrings.ArtifactsApi.Branches] ?? new JArray())
                    namedItems.Add(new ArtifactContainer((string) itm, isBranch: true));
            }

            using (var stm = await http.GetStreamAsync(MagicStrings.ArtifactsApi.Versions))
            using (var sr = new StreamReader(stm))
            using (var jtr = new JsonTextReader(sr))
            {
                var js = new JsonSerializer();
                var data = js.Deserialize<JToken>(jtr);

                foreach (var itm in data[MagicStrings.ArtifactsApi.Versions] ?? new JArray())
                    namedItems.Add(new ArtifactContainer((string) itm, isVersion: true));

                foreach (var itm in data[MagicStrings.ArtifactsApi.Aliases] ?? new JArray())
                    namedItems.Add(new ArtifactContainer((string) itm, isAlias: true));
            }

            return namedItems;
        }

        public static async Task<IEnumerable<ArtifactPackage>> FindArtifact(
            string target, Action<ArtifactFilter> filterConfiguration)
        {
            // TODO: validate filterConfiguraion

            var filter = new ArtifactFilter();
            filterConfiguration?.Invoke(filter);

            using var http = new HttpClient()
            {
                BaseAddress = BaseAddress,
                Timeout = TimeSpan.FromMilliseconds(3000)
            };

            var query = $"search/{filter.ContainerId}/{target}{filter.QueryString}";
            using var stm = await http.GetStreamAsync(query);

            using var sr = new StreamReader(stm);
            using var jtr = new JsonTextReader(sr);

            var js = new JsonSerializer();
            var data = js.Deserialize<JToken>(jtr);

            var packages = new List<ArtifactPackage>();

            foreach (JProperty itm in data["packages"] ?? new JArray())
            {
                if (filter.ShowOss && !itm.Name.Contains("oss"))
                    continue;

                if (filter.Bitness == eBitness.x64 &&
                    (string) itm.Value[MagicStrings.ArtifactsApi.Architecture] != MagicStrings.Arch.x86_64)
                {
                    continue;
                }

                var package = new ArtifactPackage(
                    itm.Name,
                    (string) itm.Value[MagicStrings.ArtifactsApi.Url] ?? string.Empty);

                packages.Add(package);
            }

            return packages;
        }

        public static async Task<(bool wasAlreadyPresent, string localPath)> FetchArtifact(BuildContext ctx, ArtifactPackage ap)
        {
            // TODO: Proper check
            if (!ap.IsDownloadable)
                throw new Exception($"{ap.FileName} is missing {nameof(ap.Url)}");

            var localPath = Path.Combine(ctx.InDir, Path.GetFileName(ap.Url));

            // TODO: support "force overwrite"
            if (File.Exists(localPath))
                return (true, localPath);

            Directory.CreateDirectory(ctx.InDir);

            using var http = new HttpClient();
            using var stm = await http.GetStreamAsync(ap.Url);
            using var fs = File.OpenWrite(localPath);

            // Buffer size just shy of one that would get onto LOH (hopefully ArrayPool will oblige...)
            var bytes = ArrayPool<byte>.Shared.Rent(81920);

            try
            {
                int bytesRead = 0;

                while (true)
                {
                    if ((bytesRead = await stm.ReadAsync(bytes, 0, bytes.Length)) <= 0)
                        break;

                    await fs.WriteAsync(bytes, 0, bytesRead);
                }
            }
            finally
            {
                if (bytes != null)
                    ArrayPool<byte>.Shared.Return(bytes);
            }

            return (false, localPath);
        }

        public static Task UnpackArtifact(BuildContext ctx, ArtifactPackage ap)
        {
            var unpackedDir = Path.Combine(
                ctx.InDir,
                Path.GetFileNameWithoutExtension(ap.FileName));

            if (Directory.Exists(unpackedDir))
                return Task.CompletedTask;

            return Task.Run(() =>
                ZipFile.ExtractToDirectory(
                    Path.Combine(ctx.InDir, Path.GetFileName(ap.FileName)),
                    Path.Combine(ctx.InDir),
                    overwriteFiles: true));
        }
    }
}
