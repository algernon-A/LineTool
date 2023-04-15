// <copyright file="BuildingToolPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod
{
    using AlgernonCommons.UI;
    using HarmonyLib;

    /// <summary>
    /// Harmony patches for BuildingTool to toggle line tool UI depending upon selected prefab.
    /// </summary>
    [HarmonyPatch(typeof(BuildingTool))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
    internal static class BuildingToolPatches
    {
        /// <summary>
        /// Harmony prefix patch to BuildingTool.OnToolUpdate to hide the line tool UI if an intersection building is selected.
        /// </summary>
        /// <param name="__instance">BuildingTool instance.</param>
        [HarmonyPatch("OnToolUpdate")]
        [HarmonyPrefix]
        internal static void OnToolUpdatePrefix(BuildingTool __instance)
        {
            // Toogle line tool UI visibility - hide if an intersection (or null) is selected, otherwise show.
            if (!__instance.m_prefab || __instance.m_prefab.m_buildingAI is IntersectionAI)
            {
                StandalonePanelManager<ToolModePanel>.Panel?.Hide();
            }
            else
            {
                StandalonePanelManager<ToolModePanel>.Create();
            }
        }
    }
}
