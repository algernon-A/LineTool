// <copyright file="ToolMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod.Modes
{
    using System.Collections.Generic;
    using UnityEngine;
    using static LineTool;

    /// <summary>
    /// Tool placement mode.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Protected fields")]
    public abstract class ToolMode
    {
        /// <summary>
        /// Overlay dashed line dash length.
        /// </summary>
        protected const float DashLength = 8f;

        /// <summary>
        /// Indicates whether a valid starting position has been recorded.
        /// </summary>
        protected bool m_validStart = false;

        /// <summary>
        /// Records the current selection start position.
        /// </summary>
        protected Vector3 m_startPos;

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public virtual void Reset()
        {
            m_validStart = false;
        }

        /// <summary>
        /// Handles a mouse click.
        /// </summary>
        /// <param name="location">Click world location.</param>
        /// <returns>True if items are to be placed as a result of this click, false otherwise.</returns>
        public virtual bool HandleClick(Vector3 location)
        {
            // If no valid start position is set, record it.
            if (!m_validStart)
            {
                m_validStart = true;
                m_startPos = location;

                // No placement at this stage (only the first click has been made).
                return false;
            }

            // Second click; we're placing items.
            return true;
        }

        /// <summary>
        /// Performs actions after items are placed on the current line, setting up for the next line to be set.
        /// </summary>
        /// <param name="location">Click world location.</param>
        public virtual void ItemsPlaced(Vector3 location)
        {
            // Update new starting location to the previous end point.
            m_startPos = location;
        }

        /// <summary>
        /// Renders the overlay for this tool mode, using the calculated point list.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        /// <param name="toolManager">ToolManager instance.</param>
        /// <param name="overlay">Overlay effect instance.</param>
        /// <param name="pointList">Current mouse position.</param>
        public virtual void RenderOverlay(RenderManager.CameraInfo cameraInfo, ToolManager toolManager, OverlayEffect overlay, List<PointData> pointList)
        {
        }

        /// <summary>
        /// Renders the overlay for this tool mode.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        /// <param name="toolManager">ToolManager instance.</param>
        /// <param name="overlay">Overlay effect instance.</param>
        /// <param name="color">Color to use.</param>
        /// <param name="position">Current end position.</param>
        /// <param name="drawGuides">Indicates whether to draw guide lines.</param>
        public abstract void RenderOverlay(RenderManager.CameraInfo cameraInfo, ToolManager toolManager, OverlayEffect overlay, Color color, Vector3 position, bool drawGuides);

        /// <summary>
        /// Calculates the points to use based on this mode.
        /// </summary>
        /// <param name="toolController">Tool controller reference.</param>
        /// <param name="prefab">Currently selected prefab.</param>
        /// <param name="currentPos">Selection current position.</param>
        /// <param name="spacing">Spacing setting.</param>
        /// <param name="rotation">Rotation setting.</param>
        /// <param name="pointList">List of points to populate.</param>
        /// <param name="rotationMode">Rotation calculation mode.</param>
        public abstract void CalculatePoints(ToolController toolController, PrefabInfo prefab, Vector3 currentPos, float spacing, float rotation, List<PointData> pointList, RotationMode rotationMode);

        /// <summary>
        /// Checks for any collision at the specified point.
        /// </summary>
        /// <param name="prefab">Selected prefab.</param>
        /// <param name="position">Position to check.</param>
        /// <param name="collidingSegments">Colliding segments array.</param>
        /// <param name="collidingBuildings">Colliding buildings array.</param>
        /// <returns><c>true</c> if the position has a collision for the selected prefab, <c>false</c> otherwise.</returns>
        protected bool CheckCollision(PrefabInfo prefab, Vector3 position, ulong[] collidingSegments, ulong[] collidingBuildings)
        {
            if (prefab is PropInfo prop)
            {
                return PropTool.CheckPlacementErrors(prop, position, false, 0, collidingSegments, collidingBuildings) != ToolBase.ToolErrors.None;
            }
            else if (prefab is TreeInfo tree)
            {
                return TreeTool.CheckPlacementErrors(tree, position, false, 0, collidingSegments, collidingBuildings) != ToolBase.ToolErrors.None;
            }

            // If we got here, no collision by default.
            return false;
        }
    }
}
