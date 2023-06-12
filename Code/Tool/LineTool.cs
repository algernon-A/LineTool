// <copyright file="LineTool.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod
{
    using System.Collections;
    using System.Collections.Generic;
    using AlgernonCommons;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.Math;
    using ColossalFramework.UI;
    using HarmonyLib;
    using LineToolMod.Modes;
    using UnityEngine;
    using TreeInstance = global::TreeInstance;

    /// <summary>
    /// The line tool itslef.
    /// </summary>
    public class LineTool : DefaultTool
    {
        private readonly List<PointData> _propPoints = new List<PointData>();
        private PrefabInfo _selectedPrefab;
        private Randomizer _randomizer = default;
        private ToolMode _currentMode = new LineMode();
        private bool _fenceMode = false;

        // Locking.
        private bool _locked = false;
        private Vector3 _lockedPosition;

        // Stepping data.
        private Vector3 _endPos;
        private float _originalRotation;
        private bool _validEndPos = false;
        private bool _stepMode = false;
        private int _stepIndex = 0;

        // Building height offset modifer.
        private float _heightOffset = 0.001f;

        // Building completed delegate.
        private BuildingCompletedDelegate _buildingCompleted;

        /// <summary>
        /// Delegate to CommonBuildingAI.BuildingCompleted (open delegate).
        /// </summary>
        private delegate void BuildingCompletedDelegate(CommonBuildingAI instance, ushort buildingID, ref Building buildingData);

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
        public ToolMode CurrentMode
        {
            get => _currentMode;

            set
            {
                _currentMode = value;

                // Reset status on mode change.
                _locked = false;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether fence mode is active.
        /// </summary>
        public bool FenceMode
        {
            get => _fenceMode;

            set
            {
                // Don't do anything if no change.
                if (value != _fenceMode)
                {
                    _fenceMode = value;

                    // Set initial spacing if fence mode has just been activated.
                    if (value)
                    {
                        SetFenceSpacing();
                    }
                }
            }
        }

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
                    Stepping = false;
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

            set
            {
                _selectedPrefab = value;

                // Reset calculated spacing if we're in fence mode.
                if (FenceMode)
                {
                    SetFenceSpacing();
                }
            }
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
        /// Gets a value indicating whether stepping is active.
        /// </summary>
        internal bool Stepping
        {
            get => _validEndPos;

            private set
            {
                // Don't do anything if no change.
                if (_validEndPos != value)
                {
                    _validEndPos = value;

                    // Update panel button states.
                    StandalonePanelManager<ToolOptionsPanel>.Panel?.UpdateButtonStates();

                    // Reset step index.
                    _stepIndex = 0;
                }
            }
        }

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
            // Don't do anything if the end position is locked.
            if (_validEndPos)
            {
                return;
            }

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
                m_ignoreTreeFlags = TreeInstance.Flags.All,
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

            // Calculate points if not locked, or no errors and no valid end position.
            if (_locked || (errors == ToolErrors.None && !_validEndPos))
            {
                // Make threadsafe.
                lock (_propPoints)
                {
                    // Clear list.
                    _propPoints.Clear();

                    // Set default rotation.
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
                                rotationMode = RotationMode.FenceAlignedX;
                            }
                            else
                            {
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
                                rotationMode = RotationMode.FenceAlignedX;
                            }
                            else
                            {
                                rotationMode = RotationMode.FenceAlignedZ;
                            }
                        }
                    }

                    // Calculate points.
                    CurrentMode?.CalculatePoints(m_toolController, _selectedPrefab, _locked ? _lockedPosition : m_accuratePosition, Spacing, Rotation, _propPoints, rotationMode);
                }
            }
        }

        /// <summary>
        /// Called by the game when tool geometry is to be rendered.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo)
        {
            // Don't preview prefabs when in step mode.
            if (!_stepMode)
            {
                if (SelectedPrefab is PropInfo propInfo)
                {
                    Randomizer randomizer = default;

                    // Preview props.
                    lock (_propPoints)
                    {
                        foreach (PointData point in _propPoints)
                        {
                            // Skip blocked points.
                            if (point.Colliding)
                            {
                                continue;
                            }

                            // Based on game code from PropTool.
                            ushort seed = Singleton<PropManager>.instance.m_props.NextFreeItem(ref randomizer);
                            Randomizer propRandomizer = new Randomizer(seed);
                            float scale = propInfo.m_minScale + (propRandomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f);
                            Color color = propInfo.GetColor(ref propRandomizer);
                            InstanceID id = default;
                            if (propInfo.m_requireWaterMap)
                            {
                                Singleton<TerrainManager>.instance.GetHeightMapping(point.Position, out Texture heightMap, out Vector4 heightMapping, out Vector4 surfaceMapping);
                                Singleton<TerrainManager>.instance.GetWaterMapping(point.Position, out Texture waterMap, out Vector4 waterHeightMapping, out Vector4 waterSurfaceMapping);
                                PropInstance.RenderInstance(cameraInfo, propInfo, id, point.Position, scale, point.Rotation, color, RenderManager.DefaultColorLocation, active: true, heightMap, heightMapping, surfaceMapping, waterMap, waterHeightMapping, waterSurfaceMapping);
                            }
                            else if (propInfo.m_requireHeightMap)
                            {
                                Singleton<TerrainManager>.instance.GetHeightMapping(point.Position, out Texture heightMap, out Vector4 heightMapping, out Vector4 surfaceMapping);
                                PropInstance.RenderInstance(cameraInfo, propInfo, id, point.Position, scale, point.Rotation, color, RenderManager.DefaultColorLocation, active: true, heightMap, heightMapping, surfaceMapping);
                            }
                            else
                            {
                                PropInstance.RenderInstance(cameraInfo, propInfo, id, point.Position, scale, point.Rotation, color, RenderManager.DefaultColorLocation, active: true);
                            }
                        }
                    }
                }
                else if (SelectedPrefab is TreeInfo treeInfo)
                {
                    Randomizer randomizer = default;

                    // Preview trees.
                    lock (_propPoints)
                    {
                        foreach (PointData point in _propPoints)
                        {
                            // Skip blocked points.
                            if (point.Colliding)
                            {
                                continue;
                            }

                            // Based on game code from TreeTool
                            uint seed = Singleton<TreeManager>.instance.m_trees.NextFreeItem(ref randomizer);
                            Randomizer treeRandomizer = new Randomizer(seed);
                            float scale = treeInfo.m_minScale + (treeRandomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f);
                            float brightness = treeInfo.m_minBrightness + (treeRandomizer.Int32(10000u) * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f);
                            TreeInstance.RenderInstance(null, treeInfo, point.Position, scale, brightness, RenderManager.DefaultColorLocation, disableRuined: false);
                        }
                    }
                }
                else if (SelectedPrefab is BuildingInfo buildingInfo)
                {
                    // Preview buildings.
                    lock (_propPoints)
                    {
                        foreach (PointData point in _propPoints)
                        {
                            // Skip blocked points.
                            if (point.Colliding)
                            {
                                continue;
                            }

                            // Based on game code from BuildingTool.
                            Building data = default;
                            data.m_position = point.Position;
                            data.m_angle = point.Rotation;
                            m_toolController.RenderCollidingNotifications(cameraInfo, 0, 0);
                            float elevation = point.Position.y;
                            Color color = buildingInfo.m_buildingAI.GetColor(0, ref data, Singleton<InfoManager>.instance.CurrentMode, Singleton<InfoManager>.instance.CurrentSubMode);
                            buildingInfo.m_buildingAI.RenderBuildGeometry(cameraInfo, point.Position, point.Rotation, elevation);
                            BuildingTool.RenderGeometry(cameraInfo, buildingInfo, 0, point.Position, point.Rotation, radius: true, color);
                            if (buildingInfo.m_subBuildings != null && buildingInfo.m_subBuildings.Length != 0)
                            {
                                Matrix4x4 matrix4x = default;
                                matrix4x.SetTRS(point.Position, Quaternion.AngleAxis(point.Rotation * 57.29578f, Vector3.down), Vector3.one);
                                for (int i = 0; i < buildingInfo.m_subBuildings.Length; i++)
                                {
                                    BuildingInfo renderInfo = buildingInfo.m_subBuildings[i].m_buildingInfo;
                                    Vector3 position = matrix4x.MultiplyPoint(buildingInfo.m_subBuildings[i].m_position);
                                    float angle = (buildingInfo.m_subBuildings[i].m_angle * (Mathf.PI / 180f)) + point.Rotation;
                                    Segment3 connectionSegment = default;
                                    renderInfo.m_buildingAI.CheckBuildPositionMainThread(0, ref position, ref angle, 0f, elevation, ref connectionSegment, out var _, out var _);
                                    renderInfo.m_buildingAI.RenderBuildGeometry(cameraInfo, position, angle, elevation);
                                    BuildingTool.RenderGeometry(cameraInfo, renderInfo, 0, position, angle, radius: true, color);
                                }
                            }
                        }
                    }
                }
            }

            base.RenderGeometry(cameraInfo);
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

            // Render the overlay based on locking status.
            if (_locked)
            {
                CurrentMode.RenderOverlay(cameraInfo, toolManager, overlay, Color.green, _lockedPosition, false);
            }
            else
            {
                // Not locked - render either the saved track if applicable, or a new track based on the current position.
                CurrentMode.RenderOverlay(cameraInfo, toolManager, overlay, Color.magenta, _validEndPos ? _endPos : m_accuratePosition, true);
            }

            // Point overlays.
            lock (_propPoints)
            {
                // Active point if stepping.
                if (_validEndPos)
                {
                    overlay.DrawCircle(cameraInfo, Color.green, _propPoints[_stepIndex].Position, 7f, -1024f, 1024f, false, false);
                    ++toolManager.m_drawCallData.m_overlayCalls;
                }

                // Remaining points.
                foreach (PointData point in _propPoints)
                {
                    // Only render where we're not previewing prefabs.
                    if (_stepMode | point.Colliding)
                    {
                        overlay.DrawCircle(cameraInfo, point.Colliding ? Color.red : Color.magenta, point.Position, 5f, -1024f, 1024f, false, false);
                        ++toolManager.m_drawCallData.m_overlayCalls;
                    }
                }
            }
        }

        /// <summary>
        /// Sets  spacing to the selected prefab's length.
        /// </summary>
        public void SetToLength()
        {
            if (SelectedPrefab is PropInfo prop)
            {
                Spacing = prop.m_mesh.bounds.extents.z * 2f;
            }
            else if (SelectedPrefab is TreeInfo tree)
            {
                Spacing = tree.m_mesh.bounds.extents.z * 2f;
            }
            else if (SelectedPrefab is BuildingInfo building)
            {
                Spacing = building.GetLength() * 8f;
            }

            // Update options panel.
            StandalonePanelManager<ToolOptionsPanel>.Panel?.RefreshSpacing();
        }

        /// <summary>
        /// Sets spacing to the selected prefab's width.
        /// </summary>
        public void SetToWidth()
        {
            if (SelectedPrefab is PropInfo prop)
            {
                Spacing = prop.m_mesh.bounds.extents.x * 2f;
            }
            else if (SelectedPrefab is TreeInfo tree)
            {
                Spacing = tree.m_mesh.bounds.extents.x * 2f;
            }
            else if (SelectedPrefab is BuildingInfo building)
            {
                Spacing = building.GetWidth() * 8f;
            }

            // Update options panel.
            StandalonePanelManager<ToolOptionsPanel>.Panel?.RefreshSpacing();
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

            // Skip this point and move to next.
            Skip();
        }

        /// <summary>
        /// Skips the next pint in sequence.
        /// </summary>
        public void Skip()
        {
            // Only step if data is valid.
            if (!_validEndPos || !StepMode)
            {
                return;
            }

            // Increment index.
            ++_stepIndex;

            // Check for completion of stepping.
            lock (_propPoints)
            {
                if (_stepIndex == _propPoints.Count)
                {
                    // Reached the end of this line; stop stepping.
                    Stepping = false;

                    // Mode placement post-processing.
                    CurrentMode.ItemsPlaced(_endPos);
                }
            }
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

            // Set the BuildingCompleted delegate if we haven't already.
            if (_buildingCompleted == null)
            {
                _buildingCompleted = AccessTools.MethodDelegate<BuildingCompletedDelegate>(AccessTools.Method(typeof(CommonBuildingAI), "BuildingCompleted"));
                if (_buildingCompleted == null)
                {
                    Logging.Error("unable to get delegate for CommonBuildingAI.BuildingCompleted");
                }
            }
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
            base.OnToolGUI(e);

            // Prcessed 'enter' key to exit locking..
            bool placing = false;
            if (_locked && e.type == EventType.KeyUp)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    e.Use();
                    _locked = false;

                    // Initiate placement.
                    InitiatePlacement();

                    return;
                }
            }

            // Don't do anything if inside UI.
            if (m_toolController.IsInsideUI)
            {
                return;
            }

            // Check for escape key.
            if (e.type == EventType.keyDown && e.keyCode == KeyCode.Escape)
            {
                // Escape key pressed - disable tool.
                e.Use();
                ToolsModifierControl.SetTool<DefaultTool>();
            }

            // Check for mousedown events with button zero.
            if (placing || e.type == EventType.MouseDown)
            {
                // Got one; use the event.
                UIInput.MouseUsed();

                if (e.button == 1)
                {
                    // Right-click; clear selection.
                    CurrentMode.Reset();
                    Stepping = false;
                    return;
                }

                // Don't do anything if there are any errors other than failed raycast.
                if (m_selectErrors != ToolErrors.None && m_selectErrors != ToolErrors.RaycastFailed)
                {
                    return;
                }

                if (e.button == 0)
                {
                    if (_locked)
                    {
                        _locked = false;
                    }

                    // Only perform actions when not locked and there's a valid selected prefab.
                    if (!_locked && _selectedPrefab != null)
                    {
                        // Handle click via current mode.
                        if (CurrentMode.HandleClick(m_accuratePosition))
                        {
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                _locked = true;
                                _lockedPosition = m_accuratePosition;
                                return;
                            }

                            // Initiate placement.
                            InitiatePlacement();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Intitiates item placement.
        /// </summary>
        private void InitiatePlacement()
        {
            // Stepping?
            if (StepMode)
            {
                // Step mode - save end point.
                Stepping = true;
                _endPos = m_accuratePosition;
                _originalRotation = Rotation;
            }
            else
            {
                // Not step mode - place all items.
                lock (_propPoints)
                {
                    PointData[] points = new PointData[_propPoints.Count];
                    _propPoints.CopyTo(points);
                    PrefabInfo selectedPrefab = _selectedPrefab;
                    Singleton<SimulationManager>.instance.AddAction(CreateItems(points, selectedPrefab));
                }

                // Mode placement post-processing.
                CurrentMode.ItemsPlaced(m_accuratePosition);
            }
        }

        /// <summary>
        /// Action method to create new items on the map.
        /// </summary>
        /// <param name="points">Target point array.</param>
        /// <param name="prefab">Prefab to place.</param>
        /// <returns>Action IEnumerator yield.</returns>
        private IEnumerator CreateItems(PointData[] points, PrefabInfo prefab)
        {
            if (prefab is PropInfo prop)
            {
                // Props - create one at each point.
                foreach (PointData point in points)
                {
                    if (!point.Colliding)
                    {
                        CreateProp(prop, point.Position, point.Rotation);
                    }
                }
            }
            else if (prefab is TreeInfo tree)
            {
                // Trees - create one at each point.
                foreach (PointData point in points)
                {
                    if (!point.Colliding)
                    {
                        CreateTree(tree, point.Position);
                    }
                }
            }
            else if (prefab is BuildingInfo building)
            {
                // Buildings - create one at each point.
                foreach (PointData point in points)
                {
                    if (!point.Colliding)
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

                    // Skip any colliding items.
                    if (!point.Colliding)
                    {
                        // Check any rotation delta.
                        float rotationDelta = Rotation - _originalRotation;

                        if (_selectedPrefab is PropInfo prop)
                        {
                            // Prop.
                            CreateProp(prop, point.Position, point.Rotation + rotationDelta);
                        }
                        else if (_selectedPrefab is TreeInfo tree)
                        {
                            // Tree.
                            CreateTree(tree, point.Position);
                        }
                        else if (_selectedPrefab is BuildingInfo building)
                        {
                            // Building.
                            CreateBuilding(building, point.Position, point.Rotation + rotationDelta);
                        }
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
            ushort buildingID = 0;

            // Check construction cost.
            bool isAffordable = true;
            if ((Singleton<ToolManager>.instance.m_properties.m_mode & ItemClass.Availability.Game) != 0)
            {
                int constructionCost = buildingPrefab.GetConstructionCost();
                isAffordable = constructionCost == 0 || constructionCost == Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Construction, constructionCost, buildingPrefab.m_class);
            }

            // Apply height adjustment.
            Vector3 adjustedPosition = position;
            adjustedPosition.y += _heightOffset;
            _heightOffset *= -1f;

            if (isAffordable)
            {
                bool buildingPlaced = false;

                if (buildingPrefab.m_buildingAI.WorksAsNet())
                {
                    Building data = default;
                    data.m_buildIndex = Singleton<SimulationManager>.instance.m_currentBuildIndex;
                    data.m_position = adjustedPosition;
                    data.m_angle = angle;
                    data.Width = buildingPrefab.m_cellWidth;
                    data.Length = buildingPrefab.m_cellLength;
                    BuildingDecoration.LoadPaths(buildingPrefab, 0, ref data, adjustedPosition.y);
                    if (Mathf.Abs(adjustedPosition.y) < 1f)
                    {
                        BuildingDecoration.LoadProps(buildingPrefab, 0, ref data);
                    }

                    Singleton<SimulationManager>.instance.m_currentBuildIndex++;
                    buildingPlaced = true;
                }
                else if (Singleton<BuildingManager>.instance.CreateBuilding(out buildingID, ref Singleton<SimulationManager>.instance.m_randomizer, buildingPrefab, adjustedPosition, angle, 0, Singleton<SimulationManager>.instance.m_currentBuildIndex))
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

                    BuildingTool.DispatchPlacementEffect(buildingPrefab, 0, adjustedPosition, angle, buildingPrefab.m_cellWidth, buildingPrefab.m_cellLength, bulldozing: false, collapsed: false);

                    // Instant construction.
                    // Check that we have a valid building ID.
                    if (buildingID != 0)
                    {
                        // Get building AI.
                        PrivateBuildingAI buildingAI = buildingPrefab.m_buildingAI as PrivateBuildingAI;

                        // Only interested in private building AI.
                        if (buildingAI != null)
                        {
                            // Check to see if construction time is greater than zero.
                            if (buildingAI.m_constructionTime > 0)
                            {
                                // Complete construction.
                                Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_frame0.m_constructState = byte.MaxValue;
                                _buildingCompleted.Invoke(buildingAI, buildingID, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID]);

                                // Have to do this manually as CommonBuildingAI.BuildingCompleted won't if construction time isn't zero.
                                Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, updateGroup: true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets fence mode spacing to the relevant dimension size.
        /// </summary>
        private void SetFenceSpacing()
        {
            if (SelectedPrefab is PropInfo prop)
            {
                // Prop fence mode.
                float xSize = prop.m_mesh.bounds.extents.x * 2f;
                float zSize = prop.m_mesh.bounds.extents.z * 2f;

                if (xSize > zSize)
                {
                    SetToWidth();
                }
                else
                {
                    SetToLength();
                }
            }
            else if (SelectedPrefab is BuildingInfo building)
            {
                // Building fence mode.
                float xSize = building.m_mesh.bounds.extents.x * 2f;
                float zSize = building.m_mesh.bounds.extents.z * 2f;

                if (xSize > zSize)
                {
                    SetToWidth();
                }
                else
                {
                    SetToLength();
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

            /// <summary>
            /// Collision state.
            /// </summary>
            public bool Colliding;
        }
    }
}
