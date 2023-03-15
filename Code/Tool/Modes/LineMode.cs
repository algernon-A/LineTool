﻿// <copyright file="LineMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool.Modes
{
    using System.Collections.Generic;
    using ColossalFramework;
    using ColossalFramework.Math;
    using UnityEngine;
    using static Tool;

    /// <summary>
    /// Straight-line placement mode.
    /// </summary>
    public class LineMode : ToolMode
    {
        /// <summary>
        /// Renders the overlay for this tool mode.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        /// <param name="toolManager">ToolManager instance.</param>
        /// <param name="overlay">Overlay effect instance.</param>
        /// <param name="mousePosition">Current mouse position.</param>
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, ToolManager toolManager, OverlayEffect overlay, Vector3 mousePosition)
        {
            // Don't render anything if no valid initial point.
            if (m_validStart)
            {
                // Simple straight line overlay.
                Segment3 segment = new Segment3(m_startPos, mousePosition);
                overlay.DrawSegment(cameraInfo, Color.magenta, segment, 2f, DashLength, -1024f, 1024f, false, false);
                ++toolManager.m_drawCallData.m_overlayCalls;
            }
        }

        /// <summary>
        /// Calculates the points to use based on this mode.
        /// </summary>
        /// <param name="currentPos">Selection current position.</param>
        /// <param name="spacing">Spacing setting.</param>
        /// <param name="pointList">List of points to populate.</param>
        /// <param name="rotationMode">Rotation calculation mode.</param>
        public override void CalculatePoints(Vector3 currentPos, float spacing, List<PointData> pointList, RotationMode rotationMode)
        {
            // Don't do anything if we don't have a valid start point.
            if (!m_validStart)
            {
                return;
            }

            // Local reference.
            TerrainManager terrainManager = Singleton<TerrainManager>.instance;

            // Calculate line vector.
            Vector3 difference = currentPos - m_startPos;
            float magnitude = difference.magnitude;

            // Create points.
            float currentDistance = 0f;
            while (currentDistance < magnitude)
            {
                // Interpolate position.
                float lerpFactor = currentDistance / magnitude;
                Vector3 thisPoint = Vector3.Lerp(m_startPos, currentPos, lerpFactor);

                // Get terrain height.
                thisPoint.y = terrainManager.SampleDetailHeight(thisPoint, out float _, out float _);

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = 0f });
                currentDistance += spacing;
            }
        }
    }
}