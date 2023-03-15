// <copyright file="ToolMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool.Modes
{
    using System.Collections.Generic;
    using UnityEngine;
    using static Tool;

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

            // Second click; place items.
            return true;
        }

        /// <summary>
        /// Renders the overlay for this tool mode.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        /// <param name="toolManager">ToolManager instance.</param>
        /// <param name="overlay">Overlay effect instance.</param>
        /// <param name="mousePosition">Current mouse position.</param>
        public abstract void RenderOverlay(RenderManager.CameraInfo cameraInfo, ToolManager toolManager, OverlayEffect overlay, Vector3 mousePosition);

        /// <summary>
        /// Calculates the points to use based on this mode.
        /// </summary>
        /// <param name="currentPos">Selection current position.</param>
        /// <param name="spacing">Spacing setting.</param>
        /// <param name="pointList">List of points to populate.</param>
        /// <param name="rotationMode">Rotation calculation mode.</param>
        public abstract void CalculatePoints(Vector3 currentPos, float spacing, List<PointData> pointList, RotationMode rotationMode);
    }
}
