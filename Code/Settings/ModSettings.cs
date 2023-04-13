// <copyright file="ModSettings.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod
{
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons.UI;
    using AlgernonCommons.XML;

    /// <summary>
    /// Global mod settings.
    /// </summary>
    [XmlRoot("LineTool")]
    public class ModSettings : SettingsXMLBase
    {
        /// <summary>
        /// Settings file name.
        /// </summary>
        [XmlIgnore]
        private static readonly string SettingsFileName = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "LineTool.xml");

        /// <summary>
        /// Gets or sets the tool options panel's last saved X position.
        /// </summary>
        public float PanelXPosition { get => StandalonePanelManager<ToolOptionsPanel>.LastSavedXPosition; set => StandalonePanelManager<ToolOptionsPanel>.LastSavedXPosition = value; }

        /// <summary>
        /// Gets or sets the tool options panel's last saved Y position.
        /// </summary>
        public float PanelYPosition { get => StandalonePanelManager<ToolOptionsPanel>.LastSavedYPosition; set => StandalonePanelManager<ToolOptionsPanel>.LastSavedYPosition = value; }

        /// <summary>
        /// Loads settings from file.
        /// </summary>
        internal static void Load() => XMLFileUtils.Load<ModSettings>(SettingsFileName);

        /// <summary>
        /// Saves settings to file.
        /// </summary>
        internal static void Save() => XMLFileUtils.Save<ModSettings>(SettingsFileName);
    }
}