// <copyright file="LineMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod.Modes
{
    using System.Collections.Generic;
    using ColossalFramework;
    using ColossalFramework.Math;
    using UnityEngine;
    using static LineTool;

    /// <summary>
    /// Straight-line placement mode.
    /// </summary>
    public class LineMode : ToolMode
    {
        // Calculated Bezier.
        private Bezier3 _thisBezier;

        /// <summary>
        /// Renders the overlay for this tool mode.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        /// <param name="toolManager">ToolManager instance.</param>
        /// <param name="overlay">Overlay effect instance.</param>
        /// <param name="color">Color to use.</param>
        /// <param name="position">Current end position.</param>
        /// <param name="drawGuides">Indicates whether to draw guide lines.</param>
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, ToolManager toolManager, OverlayEffect overlay, Color color, Vector3 position, bool drawGuides)
        {
            // Don't render anything if no valid initial point.
            if (m_validStart)
            {
                // Calculate Bezier data.
                Vector3 startDirection = position - m_startPos;
                startDirection = VectorUtils.NormalizeXZ(startDirection, out float distance);
                Vector3 endDirection = -startDirection;
                distance *= 0.15f;
                Vector3 middlePos1 = m_startPos + (startDirection * distance);
                Vector3 middlePos2 = position + (endDirection * distance);

                // Draw Bezier.
                _thisBezier = new Bezier3(m_startPos, middlePos1, middlePos2, position);
                overlay.DrawBezier(cameraInfo, color, _thisBezier, 2f, 0f, 0f, -1024f, 1024f, false, false);
                ++toolManager.m_drawCallData.m_overlayCalls;
            }
        }

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
        public override void CalculatePoints(ToolController toolController, PrefabInfo prefab, Vector3 currentPos, float spacing, float rotation, List<PointData> pointList, RotationMode rotationMode)
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

            // Handle rotation mode.
            float finalRotation = rotation;
            switch (rotationMode)
            {
                // Align prefab X-axis to line direction.
                case RotationMode.Relative:
                    finalRotation += Mathf.Atan2(difference.z, difference.x);
                    break;

                case RotationMode.FenceAlignedX:
                    finalRotation = Mathf.Atan2(difference.z, difference.x);
                    break;

                // Align prefab Y-axis to line direction.
                case RotationMode.FenceAlignedZ:
                    // Offset 90 degrees.
                    finalRotation = Mathf.Atan2(difference.z, difference.x) - (Mathf.PI / 2f);
                    break;
            }

            // Determine start position.
            float currentDistance = 0f;

            // Offset start position for fence mode.
            bool fenceMode = rotationMode == RotationMode.FenceAlignedX | rotationMode == RotationMode.FenceAlignedZ;
            if (fenceMode)
            {
                currentDistance = spacing / 2f;
            }

            // Calculate ending magnitude (for fence mode, final position is brought in by half-length).
            float endMagnitude = magnitude - currentDistance;

            // Create points.
            toolController.BeginColliding(out ulong[] collidingSegments, out ulong[] collidingBuildings);
            while (currentDistance < endMagnitude)
            {
                // Interpolate position.
                float lerpFactor = currentDistance / magnitude;
                Vector3 thisPoint = Vector3.Lerp(m_startPos, currentPos, lerpFactor);

                // Get terrain height.
                thisPoint.y = terrainManager.SampleDetailHeight(thisPoint, out float _, out float _);

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = finalRotation, Colliding = CheckCollision(prefab, thisPoint, collidingSegments, collidingBuildings) });
                currentDistance += spacing;

                // Add final point for fence mode.
                if (fenceMode && currentDistance >= endMagnitude)
                {
                    lerpFactor = endMagnitude / magnitude;
                    thisPoint = Vector3.Lerp(m_startPos, currentPos, lerpFactor);

                    pointList.Add(new PointData { Position = thisPoint, Rotation = finalRotation, Colliding = CheckCollision(prefab, thisPoint, collidingSegments, collidingBuildings) });
                }
            }

            toolController.EndColliding();
        }
    }
}
