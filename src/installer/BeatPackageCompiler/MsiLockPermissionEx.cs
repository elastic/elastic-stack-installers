using System;
using System.Xml.Linq;
using WixSharp;

namespace Elastic.PackageCompiler.Beats
{
    public class MsiLockPermissionEx : IGenericEntity
    {
        readonly string Sddl = string.Empty;
        readonly string Win64 = string.Empty;

        public MsiLockPermissionEx(string sddl, bool fWin64)
        {
            Sddl = sddl;
            Win64 = fWin64 ? "yes" : "no";
        }

        public void Process(ProcessingContext context)
        {
            var parentId = context.XParent.Attribute("Id").Value;

            var elt = new XElement("Component",
                new XAttribute("Id", "Component.MsiLockPermissionEx_" + (uint) parentId.GetHashCode32()),
                new XAttribute("Guid", Guid.NewGuid()),
                new XAttribute("KeyPath", "yes"),
                new XAttribute("Win64", Win64),

                new XElement("CreateFolder",
                    new XElement("PermissionEx", new XAttribute("Sddl", Sddl))),

                new XElement("RemoveFolder",
                    new XAttribute("Id", parentId),
                    new XAttribute("On", "uninstall"))
                );

            context.XParent.Add(elt);
        }
    }
}
