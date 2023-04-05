// <copyright file="PanelPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using AlgernonCommons;
    using HarmonyLib;

    /// <summary>
    /// Harmony patches for prefab selection panels to implement seamless prefab selection when line tool is active.
    /// </summary>
    [HarmonyPatch]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
    internal static class PanelPatches
    {
        /// <summary>
        /// Determines list of target methods to patch.
        /// </summary>
        /// <returns>List of target methods to patch.</returns>
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            // Vanilla game panels.
            yield return AccessTools.Method(typeof(BeautificationPanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(LandscapingPanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(MonumentsPanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(WondersPanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(EducationPanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(HealthcarePanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(FireDepartmentPanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(ElectricityPanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(WaterAndSewagePanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(GarbagePanel), "OnButtonClicked");
            yield return AccessTools.Method(typeof(PublicTransportPanel), "OnButtonClicked");

            // Natural resources brush (detours BeautificationGroupPanel).
            Type nrbType = Type.GetType("NaturalResourcesBrush.Detours.BeautificationPanelDetour,NaturalResourcesBrush");
            if (nrbType != null)
            {
                Logging.Message("Extra Landscaping Tools found; patching");
                yield return AccessTools.Method(nrbType, "OnButtonClicked");
            }
        }

        /// <summary>
        /// Harmony prefix patch to record if the LineTool is active before the base method is executed.
        /// </summary>
        /// <param name="__state">Passthrough to postifx; set to true if the LineTool is active when the target method is called.</param>
        internal static void Prefix(out bool __state)
        {
            __state = LineTool.IsActiveTool;
        }

        /// <summary>
        /// Harmony postifx patch to check to see if LineTool should be restored after the base method is executed.
        /// </summary>
        /// <param name="__state">Passthrough from prefix; set to true if the LineTool is active when the target method is called.</param>
        internal static void Postfix(bool __state)
        {
            if (__state)
            {
                // LineTool was active; check current tool.
                ToolBase newTool = ToolsModifierControl.toolController.CurrentTool;

                // Only restore LineTool if a supported base tool type was selected.
                if (newTool is PropTool || newTool is TreeTool || newTool is BuildingTool)
                {
                    ToolsModifierControl.toolController.CurrentTool = LineTool.Instance;
                }
            }
        }
    }
}
