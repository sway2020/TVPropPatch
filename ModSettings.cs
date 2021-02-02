// Originally written by algernon for Find It 2.
// Modified by sway
using System.Xml.Serialization;

namespace TVPropPatch
{
    /// <summary>
    /// Class to hold global mod settings.
    /// </summary>
    [XmlRoot(ElementName = "TVPropPatch", Namespace = "", IsNullable = false)]
    internal static class Settings
    {
        internal static bool skipVanillaTrees = false;
        internal static bool skipVanillaVehicles = false;
    }

    /// <summary>
    /// Defines the XML settings file.
    /// </summary>
    [XmlRoot(ElementName = "TVPropPatch", Namespace = "", IsNullable = false)]
    public class XMLSettingsFile
    {
        [XmlElement("SkipVanillaTrees")]
        public bool SkipVanillaTrees { get => Settings.skipVanillaTrees; set => Settings.skipVanillaTrees = value; }

        [XmlElement("SkipVanillaVehicles")]
        public bool SkipVanillaVehicles { get => Settings.skipVanillaVehicles; set => Settings.skipVanillaVehicles = value; }

    }
}