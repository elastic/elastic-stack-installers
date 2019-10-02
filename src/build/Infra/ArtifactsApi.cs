using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Elastic.Installer;
using ElastiBuild.Commands;

namespace ElastiBuild
{
    public class ArtifactsApi
    {
        public static Uri BaseAddress { get; } = new Uri("https://artifacts-api.elastic.co/v1/");

        public static async Task<IEnumerable<ArtifactContainer>> ListNamedContainers()
        {
            using (var http = new HttpClient()
            {
                BaseAddress = BaseAddress,
                Timeout = TimeSpan.FromMilliseconds(3000)
            })
            {
                var namedItems = new List<ArtifactContainer>();

                const string branches = "branches";
                using (var stm = await http.GetStreamAsync(branches))
                using (var sr = new StreamReader(stm))
                using (var jtr = new JsonTextReader(sr))
                {
                    var js = new JsonSerializer();
                    var data = js.Deserialize<JToken>(jtr);

                    foreach (var itm in data[branches] ?? new JArray())
                        namedItems.Add(new ArtifactContainer((string)itm, isBranch_: true));
                }

                const string versions = "versions", aliases = "aliases";
                using (var stm = await http.GetStreamAsync(versions))
                using (var sr = new StreamReader(stm))
                using (var jtr = new JsonTextReader(sr))
                {
                    var js = new JsonSerializer();
                    var data = js.Deserialize<JToken>(jtr);

                    foreach (var itm in data[versions] ?? new JArray())
                        namedItems.Add(new ArtifactContainer((string)itm, isVersion_: true));

                    foreach (var itm in data[aliases] ?? new JArray())
                        namedItems.Add(new ArtifactContainer((string)itm, isAlias_: true));
                }

                return namedItems;
            }
        }

        public static async Task<IEnumerable<ArtifactPackage>> FindArtifact(
            string target_, Action<ArtifactFilter> filterConfiguration_)
        {
            // TODO: validate filterConfiguraion

            var filter = new ArtifactFilter();
            filterConfiguration_?.Invoke(filter);

            using (var http = new HttpClient()
            {
                BaseAddress = BaseAddress,
                Timeout = TimeSpan.FromMilliseconds(3000)
            })
            {
                var query =
                    $"search/{filter.ContainerId}/{target_}"
                    + ",windows"
                    + (filter.ShowOss ? string.Empty : ",-oss")
                    + (filter.Bitness == eBitness.both
                        ? string.Empty
                        : (filter.Bitness == eBitness.x86
                            ? ",-x86_64" 
                            : string.Empty))
                    ;

                using (var stm = await http.GetStreamAsync(query))
                using (var sr = new StreamReader(stm))
                using (var jtr = new JsonTextReader(sr))
                {
                    var js = new JsonSerializer();
                    var data = js.Deserialize<JToken>(jtr);

                    var packages = new List<ArtifactPackage>();

                    foreach (JProperty itm in data["packages"] ?? new JArray())
                    {
                        if (filter.ShowOss && !itm.Name.Contains("oss"))
                            continue;

                        if (filter.Bitness == eBitness.x64 && (string)itm.Value["architecture"] != "x86_64")
                            continue;

                        var package = new ArtifactPackage(
                            itm.Name, 
                            (string) itm.Value["url"] ?? string.Empty);

                        packages.Add(package);
                    }

                    return packages;
                }
            }
        }

        public static async Task FetchArtifact(BuildContext ctx_, ArtifactPackage ap_)
        {
            // TODO: Proper check
            Debug.Assert(ap_.IsDownloadable);

            var fname = Path.Combine(ctx_.InDir, Path.GetFileName(ap_.Location));

            // TODO: support "force overwrite"
            if (File.Exists(fname))
                return;

            Directory.CreateDirectory(ctx_.InDir);

            using (var http = new HttpClient())
            {
                using (var stm = await http.GetStreamAsync(ap_.Location))
                using (var fs = File.OpenWrite(fname))
                {
                    // TODO: use ArrayPool

                    // Buffer size just shy of one that would get onto LOH
                    var bytes = new byte[81920];
                    int bytesRead = 0;

                    while (true)
                    {
                        if ((bytesRead = await stm.ReadAsync(bytes, 0, bytes.Length)) <= 0)
                            return;

                        await fs.WriteAsync(bytes, 0, bytesRead);
                    }
                }
            }
        }

        public static Task UnpackArtifact(BuildContext ctx_, ArtifactPackage ap_)
        {
            var unpackedDir = Path.Combine(
                ctx_.InDir, 
                Path.GetFileNameWithoutExtension(ap_.FileName));

            if (Directory.Exists(unpackedDir))
                return Task.CompletedTask;

            return Task.Run(() => 
                ZipFile.ExtractToDirectory(
                    Path.Combine(ctx_.InDir, Path.GetFileName(ap_.FileName)),
                    Path.Combine(ctx_.InDir),
                    overwriteFiles: true));
        }
    }
}
