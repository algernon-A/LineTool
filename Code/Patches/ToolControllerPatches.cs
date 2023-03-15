// <copyright file="ToolControllerPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using AlgernonCommons;
    using HarmonyLib;

    /// <summary>
    /// Harmony patches for the tool controller to track tool changes.
    /// </summary>
    [HarmonyPatch(typeof(ToolController))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
    public static class ToolControllerPatches
    {
        /// <summary>
        /// Harmony prefix patch to ToolController.SetTool to record selected prefab from previous tool when switching to LineTool..
        /// </summary>
        /// <param name="__instance">ToolController instance (from original instance call).</param>
        /// <param name="tool">Tool being assinged.</param>
        [HarmonyPatch("SetTool")]
        [HarmonyPrefix]
        public static void SetToolPrefix(ToolController __instance, ToolBase tool)
        {
            // Look for activation of linetool.
            if (tool is Tool lineTool)
            {
                ToolBase currentTool = __instance.CurrentTool;

                if (currentTool is BuildingTool buildingTool)
                {
                    Logging.KeyMessage("buildingTool active; selected prefab is ", buildingTool.m_prefab?.name);
                    lineTool.SelectedPrefab = buildingTool.m_prefab;
                }
                else if (currentTool is PropTool propTool)
                {
                    Logging.KeyMessage("propTool active; selected prefab is ", propTool.m_prefab?.name);
                    lineTool.SelectedPrefab = propTool.m_prefab;
                }
                else if (currentTool is TreeTool treeTool)
                {
                    Logging.KeyMessage("treeTool active; selected prefab is ", treeTool.m_prefab?.name);
                    lineTool.SelectedPrefab = treeTool.m_prefab;
                }
                else
                {
                    lineTool.SelectedPrefab = null;
                }
            }
        }
    }
}
