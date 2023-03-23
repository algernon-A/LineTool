// <copyright file="FreeformMode.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod.Modes
{
    using UnityEngine;

    /// <summary>
    /// Curved line placement mode.
    /// </summary>
    public class FreeformMode : CurveMode
    {
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
            if (!m_validElbow)
            {
                m_elbowPoint = location;
                m_validElbow = true;
                return false;
            }

            // If we got here, then we're placing.
            // Calculate new start and elbow points based on second leg.
            Vector3 difference = location - m_elbowPoint;
            m_startPos = location;
            m_elbowPoint = location + difference;

            // Place the items on the curve.
            return true;
        }
    }
}
