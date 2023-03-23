// <copyright file="Patcher.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod
{
    using System;
    using System.Reflection;
    using AlgernonCommons;
    using AlgernonCommons.Patching;
    using HarmonyLib;

    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public sealed class Patcher : PatcherBase
    {
        /// <summary>
        /// Applies patches to Find It for prefab selection management.
        /// </summary>
        public void PatchFindIt()
        {
            try
            {
                // Check for enabled Find It assembly (don't try to patch disabled assembly, because that just breaks).
                if (AssemblyUtils.GetEnabledAssembly("FindIt") is Assembly findItAsm)
                {
                    Type findItType = findItAsm.GetType("FindIt.FindIt");
                    if (findItType != null)
                    {
                        Logging.Message("Find It found; patching");
                        Harmony harmonyInstance = new Harmony(HarmonyID);
                        MethodInfo targetMethod = AccessTools.Method(findItType, "SelectPrefab");
                        MethodInfo prefix = AccessTools.Method(typeof(PanelPatches), nameof(PanelPatches.Prefix));
                        MethodInfo postfix = AccessTools.Method(typeof(PanelPatches), nameof(PanelPatches.Postfix));

                        harmonyInstance.Patch(targetMethod, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception patching Find It");
            }
        }
    }
}