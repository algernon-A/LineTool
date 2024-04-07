// <copyright file="CircleMode.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Circle placement mode.
    /// </summary>
    public class CircleMode : ToolMode
    {
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
            if (m_validStart && drawGuides)
            {
                // Simple straight line overlay to show centre and current radius/angle of circle.
                Segment3 segment = new Segment3(m_startPos, position);
                overlay.DrawSegment(cameraInfo, color, segment, 2f, DashLength, -1024f, 1024f, false, false);
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

            // Calculate spacing.
            float circumference = magnitude * Mathf.PI * 2f;
            float numPoints = Mathf.Floor(circumference / spacing);
            float increment = (Mathf.PI * 2f) / numPoints;
            float startAngle = Mathf.Atan2(difference.z, difference.x);
            float finalRotation = rotation;

            // Create points.
            toolController.BeginColliding(out ulong[] collidingSegments, out ulong[] collidingBuildings);
            for (float i = startAngle; i < startAngle + (Mathf.PI * 2f); i += increment)
            {
                float xPos = magnitude * Mathf.Cos(i);
                float yPos = magnitude * Mathf.Sin(i);
                Vector3 thisPoint = new Vector3(m_startPos.x + xPos, m_startPos.y, m_startPos.z + yPos);

                // Calculate non-absolute rotation, if applicable.
                switch (rotationMode)
                {
                    case RotationMode.Relative:
                        finalRotation = Mathf.Atan2(yPos, xPos) - (Mathf.PI / 2f) + rotation;
                        break;

                    case RotationMode.FenceAlignedZ:
                        finalRotation = Mathf.Atan2(yPos, xPos);
                        break;

                    case RotationMode.FenceAlignedX:
                        finalRotation = Mathf.Atan2(yPos, xPos) - (Mathf.PI / 2f);
                        break;
                }

                // Get terrain height.
                thisPoint.y = terrainManager.SampleDetailHeight(thisPoint, out float _, out float _);

                // Add point to list.
                pointList.Add(new PointData { Position = thisPoint, Rotation = finalRotation, Colliding = CheckCollision(prefab, thisPoint, collidingSegments, collidingBuildings) });
            }

            toolController.EndColliding();
        }

        /// <summary>
        /// Performs actions after items are placed on the current line, setting up for the next line to be set.
        /// </summary>
        /// <param name="location">Click world location.</param>
        public override void ItemsPlaced(Vector3 location)
        {
            // Empty, to retain original start position (centre of circle).
        }
    }
}
