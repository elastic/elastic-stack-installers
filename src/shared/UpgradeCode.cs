using System;
using SharpYaml.Serialization;

namespace Elastic.Installer
{
    public sealed class UpgradeCode
    {
        [YamlMember("x86")]
        public Guid x86 { get; set; }

        [YamlMember("x64")]
        public Guid x64 { get; set; }
    }
}
