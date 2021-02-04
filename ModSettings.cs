// Originally written by algernon for Find It 2.
// Modified by sway
using System.Xml.Serialization;
using System.Collections.Generic;

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
        internal static List<SkippedEntry> skippedVehicleEntries = new List<SkippedEntry>();
        internal static List<SkippedEntry> skippedTreeEntries = new List<SkippedEntry>();
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

        [XmlArray("SkippedVehicleEntries")]
        [XmlArrayItem("SkippedEntry")]
        public List<SkippedEntry> SkippedVehicleEntries { get => Settings.skippedVehicleEntries; set => Settings.skippedVehicleEntries = value; }

        [XmlArray("SkippedTreeEntries")]
        [XmlArrayItem("SkippedEntry")]
        public List<SkippedEntry> SkippedTreeEntries { get => Settings.skippedTreeEntries; set => Settings.skippedTreeEntries = value; }

    }

    public class SkippedEntry
    {
        [XmlAttribute("Name")]
        public string name = "";

        [XmlAttribute("Skipped")]
        public bool skipped = false;

        public SkippedEntry() { }

        public SkippedEntry(string newName) { name = newName; }
    }
}