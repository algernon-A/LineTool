﻿// <copyright file="CurveMode.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Curved line placement mode.
    /// </summary>
    public class CurveMode : ToolMode
    {
        // Elbow point and bezier.
        private Vector3 _elbowPoint;
        private Bezier3 _thisBezier;

        // Validity flags.
        private bool _validElbow = false;
        private bool _validBezier = false;

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _validElbow = false;
            _validBezier = false;
        }

        /// <summary>
        /// Handles a mouse click.
        /// </summary>
        /// <param name="location">Click world location.</param>
        /// <returns>True if items are to be placed as a result of this click, false otherwise.</returns>
        public override bool HandleClick(Vector3 location)
        {
            // If no valid initial point, record this as the first point.
            if (!m_validStart)
            {
                m_startPos = location;
                m_validStart = true;
                return false;
            }

            // Othwerwise, if no valid elbow point, record this as the elbow point.
            if (!_validElbow)
            {
                _elbowPoint = location;
                _validElbow = true;
                return false;
            }

            // If we got here, then we're placing.
            // Calculate new start and elbow points based on second leg.
            Vector3 difference = location - _elbowPoint;
            m_startPos = location;
            _elbowPoint = location + difference;

            // Place the items on the curve.
            return true;
        }

        /// <summary>
        /// Calculates the points to use based on this mode.
        /// </summary>
        /// <param name="currentPos">Selection current position.</param>
        /// <param name="spacing">Spacing setting.</param>
        /// <param name="rotation">Rotation setting.</param>
        /// <param name="pointList">List of points to populate.</param>
        /// <param name="rotationMode">Rotation calculation mode.</param>
        public override void CalculatePoints(Vector3 currentPos, float spacing, float rotation, List<PointData> pointList, RotationMode rotationMode)
        {
            // Don't do anything if we don't have valid start and elbow points.
            if (!(m_validStart & _validElbow))
            {
                return;
            }

            // Calulate angles.
            Vector3 direction1 = _elbowPoint - m_startPos;
            direction1.Normalize();
            Vector3 direction2 = _elbowPoint - currentPos;
            direction2.Normalize();

            // Create bezier.
            NetSegment.CalculateMiddlePoints(m_startPos, direction1, currentPos, direction2, false, false, out Vector3 middlePos1, out Vector3 middlePos2);
            _thisBezier = new Bezier3(m_startPos, middlePos1, middlePos2, currentPos);
            _validBezier = true;

            // Local reference.
            TerrainManager terrainManager = Singleton<TerrainManager>.instance;

            // Calculate points along bezier.
            float tFactor = 0f;
            if (rotationMode == RotationMode.FenceAlignedX || rotationMode == RotationMode.FenceAlignedZ)
            {
                // Fence mode.
                while (tFactor <= 1.0f)
                {
                    // Get start and endpoints of this fence segment.
                    Vector3 startPoint = _thisBezier.Position(tFactor);
                    tFactor = BezierStep(tFactor, spacing);
                    Vector3 endPoint = _thisBezier.Position(tFactor);

                    // Calculate rotation angle.
                    Vector3 difference = endPoint - startPoint;
                    float finalRotation = Mathf.Atan2(difference.z, difference.x);

                    // Calculate midpoint (prop placement point) and get terrain height.
                    Vector3 midPoint = new Vector3(endPoint.x - (difference.x / 2f), 0f, endPoint.z - (difference.z / 2f));
                    midPoint.y = terrainManager.SampleDetailHeight(midPoint, out float _, out float _);

                    // Add point to list.
                    pointList.Add(new PointData { Position = midPoint, Rotation = finalRotation });
                }
            }
            else
            {
                // Non-fence mode.
                while (tFactor <= 1.0f)
                {
                    Vector3 thisPoint = _thisBezier.Position(tFactor);

                    // Get terrain height.
                    thisPoint.y = terrainManager.SampleDetailHeight(thisPoint, out float _, out float _);

                    // Calculate rotation.
                    float finalRotation = rotation;
                    if (rotationMode == RotationMode.Relative)
                    {
                        // Get start and endpoints of this fence segment.
                        Vector3 startPoint = _thisBezier.Position(tFactor);
                        tFactor = BezierStep(tFactor, spacing);
                        Vector3 endPoint = _thisBezier.Position(tFactor);

                        // Calculate rotation angle.
                        Vector3 difference = endPoint - startPoint;
                        finalRotation += Mathf.Atan2(difference.z, difference.x);
                    }

                    // Add point to list.
                    pointList.Add(new PointData { Position = thisPoint, Rotation = finalRotation });

                    // Get next point.
                    tFactor = BezierStep(tFactor, spacing);
                }
            }
        }

        /// <summary>
        /// Renders the overlay for this tool mode.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        /// <param name="toolManager">ToolManager instance.</param>
        /// <param name="overlay">Overlay effect instance.</param>
        /// <param name="mousePosition">Current mouse position.</param>
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, ToolManager toolManager, OverlayEffect overlay, Vector3 mousePosition)
        {
            // No overlay to render if there isn't a valid starting point.
            if (!m_validStart)
            {
                return;
            }

            // Draw line guides.
            if (!_validElbow)
            {
                // No elbow point yet - just draw initial line.
                Segment3 segment = new Segment3(m_startPos, mousePosition);
                overlay.DrawSegment(cameraInfo, Color.magenta, segment, 2f, DashLength, -1024f, 1024f, false, false);
                ++toolManager.m_drawCallData.m_overlayCalls;
            }
            else
            {
                // Valid elbow - draw both lines.
                Segment3 segment = new Segment3(m_startPos, _elbowPoint);
                Segment3 segment2 = new Segment3(_elbowPoint, mousePosition);
                overlay.DrawSegment(cameraInfo, Color.magenta, segment, segment2, 2f, DashLength, -1024f, 1024f, false, false);
                ++toolManager.m_drawCallData.m_overlayCalls;
            }

            // Draw bezier overlay if we have a valid bezier to draw.
            if (_validBezier)
            {
                overlay.DrawBezier(cameraInfo, Color.magenta, _thisBezier, 2f, 0f, 0f, -1024f, 1024f, false, false);
                ++toolManager.m_drawCallData.m_overlayCalls;
            }
        }

        /// <summary>
        /// Steps along a bezier calculating the target t factor for the given starting t factor and the current spacing setting.
        /// Code based on Alterran's PropLineTool (StepDistanceCurve, Utilities/PLTMath.cs).
        /// </summary>
        /// <param name="tStart">Starting t factor.</param>
        /// <param name="spacing">Spacing setting.</param>
        /// <returns>Target t factor.</returns>
        private float BezierStep(float tStart, float spacing)
        {
            const float Tolerance = 0.001f;
            const float ToleranceSquared = Tolerance * Tolerance;

            float tEnd = _thisBezier.Travel(tStart, spacing);
            float usedDistance = CubicBezierArcLengthXZGauss04(tStart, tEnd);

            // Twelve iteration maximum for performance and to prevent infinite loops.
            for (int i = 0; i < 12; ++i)
            {
                // Stop looping if the remaining distance is less than tolerance.
                float remainingDistance = spacing - usedDistance;
                if (remainingDistance * remainingDistance < ToleranceSquared)
                {
                    break;
                }

                usedDistance = CubicBezierArcLengthXZGauss04(tStart, tEnd);
                tEnd += (spacing - usedDistance) / CubicSpeedXZ(tEnd);
            }

            return tEnd;
        }

        /// <summary>
        /// From Alterann's PropLineTool (CubicSpeedXZ, Utilities/PLTMath.cs).
        /// Returns the integrand of the arc length function for a cubic bezier curve, constrained to the XZ-plane at a specific t.
        /// </summary>
        /// <param name="t"> t factor.</param>
        /// <returns>Integrand of arc length.</returns>
        private float CubicSpeedXZ(float t)
        {
            // Pythagorean theorem.
            Vector3 tangent = _thisBezier.Tangent(t);
            float derivXsqr = tangent.x * tangent.x;
            float derivZsqr = tangent.z * tangent.z;

            return Mathf.Sqrt(derivXsqr + derivZsqr);
        }

        /// <summary>
        /// From Alterann's PropLineTool (CubicBezierArcLengthXZGauss04, Utilities/PLTMath.cs).
        /// Returns the XZ arclength of a cubic bezier curve between two t factors.
        /// Uses Gauss–Legendre Quadrature with n = 4.
        /// </summary>
        /// <param name="t1">Starting t factor.</param>
        /// <param name="t2">Ending t factor.</param>
        /// <returns>XZ arc length.</returns>
        private float CubicBezierArcLengthXZGauss04(float t1, float t2)
        {
            float linearAdj = (t2 - t1) / 2f;

            // Constants are from Gauss-Lengendre quadrature rules for n = 4.
            float p1 = CubicSpeedXZGaussPoint(0.3399810435848563f, 0.6521451548625461f, t1, t2);
            float p2 = CubicSpeedXZGaussPoint(-0.3399810435848563f, 0.6521451548625461f, t1, t2);
            float p3 = CubicSpeedXZGaussPoint(0.8611363115940526f, 0.3478548451374538f, t1, t2);
            float p4 = CubicSpeedXZGaussPoint(-0.8611363115940526f, 0.3478548451374538f, t1, t2);

            return linearAdj * (p1 + p2 + p3 + p4);
        }

        /// <summary>
        /// From Alterann's PropLineTool (CubicSpeedXZGaussPoint, Utilities/PLTMath.cs).
        /// </summary>
        /// <param name="x_i">X i.</param>
        /// <param name="w_i">W i.</param>
        /// <param name="a">a.</param>
        /// <param name="b">b.</param>
        /// <returns>Cubic speed.</returns>
        private float CubicSpeedXZGaussPoint(float x_i, float w_i, float a, float b)
        {
            float linearAdj = (b - a) / 2f;
            float constantAdj = (a + b) / 2f;
            return w_i * CubicSpeedXZ((linearAdj * x_i) + constantAdj);
        }
    }
}
