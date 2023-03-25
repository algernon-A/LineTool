// <copyright file="LineTool.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod
{
    using System.Collections;
    using System.Collections.Generic;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.Math;
    using ColossalFramework.UI;
    using LineToolMod.Modes;
    using UnityEngine;

    /// <summary>
    /// The line tool itslef.
    /// </summary>
    public class LineTool : DefaultTool
    {
        private readonly List<PointData> _propPoints = new List<PointData>();
        private PrefabInfo _selectedPrefab;
        private Randomizer _randomizer = default;

        // Stepping data.
        private Vector3 _endPos;
        private bool _validEndPos = false;
        private bool _stepMode = false;
        private int _stepIndex = 0;

        /// <summary>
        /// Rotation calculation mode enum.
        /// </summary>
        public enum RotationMode
        {
            /// <summary>
            /// Fixed rotation.
            /// </summary>
            Fixed,

            /// <summary>
            /// Relative rotation.
            /// </summary>
            Relative,

            /// <summary>
            /// Random rotation.
            /// </summary>
            Random,

            /// <summary>
            /// Fence mode, aligned to X-axis.
            /// </summary>
            FenceAlignedX,

            /// <summary>
            /// Fence mode, aligned to Z-axis.
            /// </summary>
            FenceAlignedZ,
        }

        /// <summary>
        /// Gets or sets the line spacing.
        /// </summary>
        public float Spacing { get; set; } = 10f;

        /// <summary>
        /// Gets or sets the rotation setting.
        /// </summary>
        public float Rotation { get; set; } = 0f;

        /// <summary>
        /// Gets or sets the current tool mode.
        /// </summary>
        public ToolMode CurrentMode { get; set; } = new LineMode();

        /// <summary>
        /// Gets or sets a value indicating whether fence mode is active.
        /// </summary>
        public bool FenceMode { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether step mode is active.
        /// </summary>
        public bool StepMode
        {
            get => _stepMode;

            set
            {
                _stepMode = value;

                // Clear flags if mode is being disabled.
                if (!value)
                {
                    _validEndPos = false;
                    _stepIndex = 0;
                }
            }
        }

        /// <summary>
        /// Gets or sets the base tool for this activation of the line tool.
        /// </summary>
        public ToolBase BaseTool { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether rotation is relative or absolute.
        /// </summary>
        public bool RelativeRotation { get; set; } = true;

        /// <summary>
        /// Gets or sets the selected prefab to place.
        /// </summary>
        public PrefabInfo SelectedPrefab
        {
            get => _selectedPrefab;

            set => _selectedPrefab = value;
        }

        /// <summary>
        /// Gets the active instance reference.
        /// </summary>
        internal static LineTool Instance => ToolsModifierControl.toolController?.gameObject?.GetComponent<LineTool>();

        /// <summary>
        /// Gets a value indicating whether the RON tool is currently active (true) or inactive (false).
        /// </summary>
        internal static bool IsActiveTool => Instance != null && ToolsModifierControl.toolController.CurrentTool == Instance;

        /// <summary>
        /// Sets vehicle ingore flags to ignore all vehicles.
        /// </summary>
        /// <returns>Vehicle flags ignoring all vehicles.</returns>
        public override Vehicle.Flags GetVehicleIgnoreFlags() =>
            Vehicle.Flags.LeftHandDrive
            | Vehicle.Flags.Created
            | Vehicle.Flags.Deleted
            | Vehicle.Flags.Spawned
            | Vehicle.Flags.Inverted
            | Vehicle.Flags.TransferToTarget
            | Vehicle.Flags.TransferToSource
            | Vehicle.Flags.Emergency1
            | Vehicle.Flags.Emergency2
            | Vehicle.Flags.WaitingPath
            | Vehicle.Flags.Stopped
            | Vehicle.Flags.Leaving
            | Vehicle.Flags.Arriving
            | Vehicle.Flags.Reversed
            | Vehicle.Flags.TakingOff
            | Vehicle.Flags.Flying
            | Vehicle.Flags.Landing
            | Vehicle.Flags.WaitingSpace
            | Vehicle.Flags.WaitingCargo
            | Vehicle.Flags.GoingBack
            | Vehicle.Flags.WaitingTarget
            | Vehicle.Flags.Importing
            | Vehicle.Flags.Exporting
            | Vehicle.Flags.Parking
            | Vehicle.Flags.CustomName
            | Vehicle.Flags.OnGravel
            | Vehicle.Flags.WaitingLoading
            | Vehicle.Flags.Congestion
            | Vehicle.Flags.DummyTraffic
            | Vehicle.Flags.Underground
            | Vehicle.Flags.Transition
            | Vehicle.Flags.InsideBuilding;

        /// <summary>
        /// Called by the game every simulation step.
        /// Performs raycasting to select hovered instance.
        /// </summary>
        public override void SimulationStep()
        {
            // Get base mouse ray.
            Ray mouseRay = m_mouseRay;

            // Get raycast input.
            RaycastInput input = new RaycastInput(mouseRay, m_mouseRayLength)
            {
                m_rayRight = m_rayRight,
                m_netService = GetService(),
                m_buildingService = GetService(),
                m_propService = GetService(),
                m_treeService = GetService(),
                m_districtNameOnly = true,
                m_ignoreTerrain = false,
                m_ignoreNodeFlags = NetNode.Flags.All,
                m_ignoreSegmentFlags = GetSegmentIgnoreFlags(out input.m_segmentNameOnly),
                m_ignoreBuildingFlags = Building.Flags.None,
                m_ignoreTreeFlags = global::TreeInstance.Flags.All,
                m_ignorePropFlags = PropInstance.Flags.All,
                m_ignoreVehicleFlags = GetVehicleIgnoreFlags(),
                m_ignoreParkedVehicleFlags = VehicleParked.Flags.All,
                m_ignoreCitizenFlags = CitizenInstance.Flags.All,
                m_ignoreTransportFlags = TransportLine.Flags.All,
                m_ignoreDistrictFlags = District.Flags.All,
                m_ignoreParkFlags = DistrictPark.Flags.All,
                m_ignoreDisasterFlags = DisasterData.Flags.All,
                m_transportTypes = 0,
            };

            ToolErrors errors = ToolErrors.None;
            RaycastOutput output;

            // Is the base mouse ray valid?
            if (m_mouseRayValid)
            {
                // Yes - raycast.
                if (RayCast(input, out output))
                {
                    // Set base tool accurate position.
                    m_accuratePosition = output.m_hitPos;
                }
                else
                {
                    // Raycast failed.
                    errors = ToolErrors.RaycastFailed;
                }
            }
            else
            {
                // No valid mouse ray.
                output = default;
                errors = ToolErrors.RaycastFailed;
            }

            // Set mouse position and record errors.
            m_mousePosition = output.m_hitPos;
            m_selectErrors = errors;

            // Calculate points if no errors and no valid end position.
            if (errors == ToolErrors.None && !_validEndPos)
            {
                // Make threadsafe.
                lock (_propPoints)
                {
                    // Clear list.
                    _propPoints.Clear();

                    // Set default spacing and rotation.
                    float spacing = Spacing;
                    RotationMode rotationMode = RelativeRotation ? RotationMode.Relative : RotationMode.Fixed;

                    // Fence mode calculations (overrides spacing and rotation mode).
                    if (FenceMode)
                    {
                        if (SelectedPrefab is PropInfo prop)
                        {
                            // Prop fence mode.
                            float xSize = prop.m_mesh.bounds.extents.x * 2f;
                            float zSize = prop.m_mesh.bounds.extents.z * 2f;

                            if (xSize > zSize)
                            {
                                spacing = xSize;
                                rotationMode = RotationMode.FenceAlignedX;
                            }
                            else
                            {
                                spacing = zSize;
                                rotationMode = RotationMode.FenceAlignedZ;
                            }
                        }
                        else if (SelectedPrefab is BuildingInfo building)
                        {
                            // Building fence mode.
                            float xSize = building.m_mesh.bounds.extents.x * 2f;
                            float zSize = building.m_mesh.bounds.extents.z * 2f;

                            if (xSize > zSize)
                            {
                                spacing = xSize;
                                rotationMode = RotationMode.FenceAlignedX;
                            }
                            else
                            {
                                spacing = zSize;
                                rotationMode = RotationMode.FenceAlignedZ;
                            }
                        }
                    }

                    // Calculate points.
                    CurrentMode?.CalculatePoints(m_accuratePosition, spacing, Rotation, _propPoints, rotationMode);
                }
            }
        }

        /// <summary>
        /// Called by game when overlay is to be rendered.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            // Local references.
            ToolManager toolManager = Singleton<ToolManager>.instance;
            OverlayEffect overlay = Singleton<RenderManager>.instance.OverlayEffect;

            // Render either the saved track if applicable, or a new track based on the current position.
            CurrentMode.RenderOverlay(cameraInfo, toolManager, overlay, _validEndPos ? _endPos : m_accuratePosition);

            // Point overlays.
            lock (_propPoints)
            {
                foreach (PointData point in _propPoints)
                {
                    overlay.DrawCircle(cameraInfo, Color.magenta, point.Position, 5f, -1024f, 1024f, false, false);
                    ++toolManager.m_drawCallData.m_overlayCalls;
                }
            }
        }

        /// <summary>
        /// Adds the next item in sequence.
        /// </summary>
        public void Step()
        {
            // Only step if data is valid.
            if (!_validEndPos || !StepMode)
            {
                return;
            }

            // Place item at this point.
            int pointIndex = _stepIndex;
            Singleton<SimulationManager>.instance.AddAction(CreateItem(pointIndex));

            // Increment index.
            ++_stepIndex;
        }

        /// <summary>
        /// Toggles the current tool to/from the line tool.
        /// </summary>
        internal static void ToggleTool()
        {
            // Activate tool if it isn't already; if already active, deactivate it by selecting the default tool instead.
            if (!IsActiveTool)
            {
                // Activate tool.
                ToolsModifierControl.toolController.CurrentTool = Instance;
            }
            else
            {
                // Activate default tool.
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        /// <summary>
        /// Initialise the tool.
        /// Called by unity when the tool is created.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // Load cursor.
            m_cursor = UITextures.LoadCursor("LT-Cursor.png");
        }

        /// <summary>
        /// Called by game when tool is enabled.
        /// </summary>
        protected override void OnEnable()
        {
            // Call base even before loaded checks to properly initialize tool.
            base.OnEnable();

            // Make sure that game is loaded before activating tool.
            if (!Loading.IsLoaded)
            {
                // Loading not complete - deactivate tool by seting default tool.
                ToolsModifierControl.SetTool<DefaultTool>();
                return;
            }
        }

        /// <summary>
        /// Unity late update handling.
        /// Called by game every late update.
        /// </summary>
        protected override void OnToolLateUpdate()
        {
            base.OnToolLateUpdate();

            // Force the info mode to none.
            ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.None);
        }

        /// <summary>
        /// Tool GUI event processing.
        /// Called by game every GUI update.
        /// </summary>
        /// <param name="e">Event.</param>
        protected override void OnToolGUI(Event e)
        {
            // Check for escape key.
            if (e.type == EventType.keyDown && e.keyCode == KeyCode.Escape)
            {
                // Escape key pressed - disable tool.
                e.Use();
                ToolsModifierControl.SetTool<DefaultTool>();
            }

            // Don't do anything if mouse is inside UI or if there are any errors other than failed raycast.
            if (m_toolController.IsInsideUI || (m_selectErrors != ToolErrors.None && m_selectErrors != ToolErrors.RaycastFailed))
            {
                return;
            }

            // Check for mousedown events with button zero.
            if (e.type == EventType.MouseDown && _selectedPrefab != null)
            {
                // Got one; use the event.
                UIInput.MouseUsed();

                if (e.button == 0)
                {
                    // Handle click via current mode.
                    if (CurrentMode.HandleClick(m_accuratePosition))
                    {
                        // Stepping?
                        if (StepMode)
                        {
                            // Step mode - save end point.
                            _validEndPos = true;
                            _endPos = m_accuratePosition;
                            _stepIndex = 0;
                        }
                        else
                        {
                            // Not step mode - place all items.
                            Singleton<SimulationManager>.instance.AddAction(CreateItems());

                            // Mode placement post-processing.
                            CurrentMode.ItemsPlaced(m_accuratePosition);
                        }
                    }
                }
                else if (e.button == 1)
                {
                    // Right-click; clear selection.
                    CurrentMode.Reset();
                    _validEndPos = false;
                    _stepIndex = 0;
                }
            }
        }

        /// <summary>
        /// Action method to create new items on the map.
        /// </summary>
        /// <returns>Action IEnumerator yield.</returns>
        private IEnumerator CreateItems()
        {
            // Make threadsafe.
            lock (_propPoints)
            {
                if (_selectedPrefab is PropInfo prop)
                {
                    // Props - create one at each point.
                    foreach (PointData point in _propPoints)
                    {
                        CreateProp(prop, point.Position, point.Rotation);
                    }
                }
                else if (_selectedPrefab is TreeInfo tree)
                {
                    // Trees - create one at each point.
                    foreach (PointData point in _propPoints)
                    {
                        CreateTree(tree, point.Position);
                    }
                }
                else if (_selectedPrefab is BuildingInfo building)
                {
                    // Buildings - create one at each point.
                    foreach (PointData point in _propPoints)
                    {
                        CreateBuilding(building, point.Position, point.Rotation);
                    }
                }
            }

            yield return 0;
        }

        /// <summary>
        /// Action method to create a new single item on the map.
        /// </summary>
        /// <param name="pointIndex">Index number of this point.</param>
        /// <returns>Action IEnumerator yield.</returns>
        private IEnumerator CreateItem(int pointIndex)
        {
            // Make threadsafe.
            lock (_propPoints)
            {
                if (pointIndex < _propPoints.Count)
                {
                    PointData point = _propPoints[pointIndex];

                    if (_selectedPrefab is PropInfo prop)
                    {
                        // Prop.
                        CreateProp(prop, point.Position, point.Rotation);
                    }
                    else if (_selectedPrefab is TreeInfo tree)
                    {
                        // Tree.
                        CreateTree(tree, point.Position);
                    }
                    else if (_selectedPrefab is BuildingInfo building)
                    {
                        // Building.
                        CreateBuilding(building, point.Position, point.Rotation);
                    }
                }
            }

            yield return 0;
        }

        /// <summary>
        /// Creates a prop instance.
        /// Based on game code.
        /// </summary>
        /// <param name="prop">Prop prefab.</param>
        /// <param name="position">Postion.</param>
        /// <param name="rotation">Prop rotation (in degrees).</param>
        private void CreateProp(PropInfo prop, Vector3 position, float rotation)
        {
            // Check construction cost.
            bool isAffordable = true;
            if ((Singleton<ToolManager>.instance.m_properties.m_mode & ItemClass.Availability.Game) != 0)
            {
                int constructionCost = prop.GetConstructionCost();
                isAffordable = constructionCost == 0 || constructionCost == Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Construction, constructionCost, prop.m_class);
            }

            // Create the prop.
            if (isAffordable && Singleton<PropManager>.instance.CreateProp(out ushort _, ref _randomizer, prop, position, rotation, true))
            {
                PropTool.DispatchPlacementEffect(position, false);
            }
        }

        /// <summary>
        /// Creates a tree instance.
        /// Based on game code.
        /// </summary>
        /// <param name="tree">Tree prefab.</param>
        /// <param name="position">Postion.</param>
        private void CreateTree(TreeInfo tree, Vector3 position)
        {
            // Check construction cost.
            bool isAffordable = true;
            if ((Singleton<ToolManager>.instance.m_properties.m_mode & ItemClass.Availability.Game) != 0)
            {
                int constructionCost = tree.GetConstructionCost();
                isAffordable = constructionCost == 0 || constructionCost == Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Construction, constructionCost, tree.m_class);
            }

            // Create the tree.
            if (isAffordable && Singleton<TreeManager>.instance.CreateTree(out uint _, ref _randomizer, tree, position, true))
            {
                TreeTool.DispatchPlacementEffect(position, false);
            }
        }

        /// <summary>
        /// Creates a building instance.
        /// Based on game code.
        /// </summary>
        /// <param name="building">Building prefab.</param>
        /// <param name="position">Building position.</param>
        /// <param name="angle">Bulding angle.</param>
        private void CreateBuilding(BuildingInfo building, Vector3 position, float angle)
        {
            // Effective prefab (may be overwritten).
            BuildingInfo buildingPrefab = building;

            // Check construction cost.
            bool isAffordable = true;
            if ((Singleton<ToolManager>.instance.m_properties.m_mode & ItemClass.Availability.Game) != 0)
            {
                int constructionCost = buildingPrefab.GetConstructionCost();
                isAffordable = constructionCost == 0 || constructionCost == Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Construction, constructionCost, buildingPrefab.m_class);
            }

            if (isAffordable)
            {
                bool buildingPlaced = false;

                if (buildingPrefab.m_buildingAI.WorksAsNet())
                {
                    Building data = default;
                    data.m_buildIndex = Singleton<SimulationManager>.instance.m_currentBuildIndex;
                    data.m_position = position;
                    data.m_angle = angle;
                    data.Width = buildingPrefab.m_cellWidth;
                    data.Length = buildingPrefab.m_cellLength;
                    BuildingDecoration.LoadPaths(buildingPrefab, 0, ref data, position.y);
                    if (Mathf.Abs(position.y) < 1f)
                    {
                        BuildingDecoration.LoadProps(buildingPrefab, 0, ref data);
                    }

                    Singleton<SimulationManager>.instance.m_currentBuildIndex++;
                    buildingPlaced = true;
                }
                else if (Singleton<BuildingManager>.instance.CreateBuilding(out ushort buildingID, ref Singleton<SimulationManager>.instance.m_randomizer, buildingPrefab, position, angle, 0, Singleton<SimulationManager>.instance.m_currentBuildIndex))
                {
                    BuildingInfo placedInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;
                    if (placedInfo != null)
                    {
                        buildingPrefab = placedInfo;
                    }

                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_flags |= Building.Flags.FixedHeight;

                    Singleton<SimulationManager>.instance.m_currentBuildIndex++;
                    buildingPlaced = true;
                }

                // Handle building placement.
                if (buildingPlaced)
                {
                    buildingPrefab.m_notUsedGuide?.Disable();

                    buildingPrefab.m_buildingAI.PlacementSucceeded();
                    Singleton<GuideManager>.instance.m_notEnoughMoney.Deactivate();
                    int publicServiceIndex = ItemClass.GetPublicServiceIndex(buildingPrefab.m_class.m_service);
                    if (publicServiceIndex != -1)
                    {
                        Singleton<GuideManager>.instance.m_serviceNotUsed[publicServiceIndex].Disable();
                        Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex].Deactivate();
                        Singleton<CoverageManager>.instance.CoverageUpdated(buildingPrefab.m_class.m_service, buildingPrefab.m_class.m_subService, buildingPrefab.m_class.m_level);
                    }

                    BuildingTool.DispatchPlacementEffect(buildingPrefab, 0, position, angle, buildingPrefab.m_cellWidth, buildingPrefab.m_cellLength, bulldozing: false, collapsed: false);
                }
            }
        }

        /// <summary>
        /// Data struct for calculated point.
        /// </summary>
        public struct PointData
        {
            /// <summary>
            /// Point location.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// Point rotation.
            /// </summary>
            public float Rotation;
        }
    }
}
