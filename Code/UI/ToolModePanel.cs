// <copyright file="ToolModePanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using LineToolMod.Modes;
    using UnityEngine;

    /// <summary>
    /// Mode selection buttons panel for the line tool.
    /// </summary>
    internal class ToolModePanel : StandalonePanelBase
    {
        // Mode button size.
        private const float ButtonSize = 36f;

        // Panel components.
        private UIMultiStateButton _optionsPanelToggle;

        // Tool selection indicies.
        private enum ModeIndexes : int
        {
            None = -1,
            Single,
            Line,
            Curve,
            Freeform,
            Circle,
            NumModes,
        }

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        public override float PanelWidth => ButtonSize * ((int)ModeIndexes.NumModes + 2);

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        public override float PanelHeight => ButtonSize;

        /// <summary>
        /// Gets a value indicating whether the panel's previous position should be remembered after closing.
        /// </summary>
        public override bool RememberPosition => false;

        /// <summary>
        /// Called by Unity before the first frame.
        /// Used to perform setup.
        /// </summary>
        public override void Start()
        {
            base.Start();

            // Add mode tabstrip.
            float tabStripWidth = ButtonSize * (int)ModeIndexes.NumModes;
            UITabstrip controlTabStrip = AddUIComponent<UITabstrip>();
            controlTabStrip.relativePosition = new Vector2(-Margin, 0f);
            controlTabStrip.width = tabStripWidth;
            controlTabStrip.height = ButtonSize;
            controlTabStrip.padding.right = 0;

            // Get button template.
            UIButton buttonTemplate = GameObject.Find("ToolMode").GetComponent<UITabstrip>().GetComponentInChildren<UIButton>();

            // Add buttons.
            AddTabTextButton(controlTabStrip, buttonTemplate, "Single", "POINT", "•", 1.5f, 0, 1, 4, 0);
            AddTabSpriteButton(controlTabStrip, buttonTemplate, "Straight", "STRAIGHT_LINE");
            AddTabSpriteButton(controlTabStrip, buttonTemplate, "Curved", "CURVE");
            AddTabSpriteButton(controlTabStrip, buttonTemplate, "Freeform", "FREEFORM");
            AddTabTextButton(controlTabStrip, buttonTemplate, "Circle", "CIRCLE", "○", 3.0f, -2, 1, -13, 0);

            // Fence mode toggle.
            UITextureAtlas toggleAtlas = UITextures.CreateSpriteAtlas("LineToolToggles", 1024, "PLT");
            UIMultiStateButton fenceModeToggle = AddToggleButton(this, "FenceMode", toggleAtlas, "PLT_MultiState", "PLT_FenceMode");
            fenceModeToggle.relativePosition = controlTabStrip.relativePosition + new Vector3(tabStripWidth, 0f);
            fenceModeToggle.tooltip = Translations.Translate("FENCEMODE");
            fenceModeToggle.activeStateIndex = LineTool.Instance.FenceMode ? 1 : 0;
            fenceModeToggle.eventActiveStateIndexChanged += (c, state) =>
            {
                LineTool.Instance.FenceMode = state != 0;
            };

            // Options panel toggle.
            _optionsPanelToggle = AddToggleButton(this, "Options", toggleAtlas, "PLT_MultiState", "PLT_ToggleCP");
            _optionsPanelToggle.relativePosition = fenceModeToggle.relativePosition + new Vector3(ButtonSize, 0f);
            _optionsPanelToggle.tooltip = Translations.Translate("LINE_OPTIONS");
            _optionsPanelToggle.eventActiveStateIndexChanged += (c, state) =>
            {
                if (state == 0)
                {
                    StandalonePanelManager<ToolOptionsPanel>.Panel?.Close();
                }
                else
                {
                    StandalonePanelManager<ToolOptionsPanel>.Create();
                    StandalonePanelManager<ToolOptionsPanel>.Panel.EventClose += OptionsPanelClosed;
                }
            };

            // Event handler.
            controlTabStrip.eventSelectedIndexChanged += TabIndexChanged;

            // Make sure tool options panel is closed when this is closed.
            EventClose += () => StandalonePanelManager<ToolOptionsPanel>.Panel?.Close();
        }

        /// <summary>
        /// Gets the panel's default position.
        /// </summary>
        protected override void ApplyDefaultPosition()
        {
            // Set position.
            UIComponent optionsBar = GameObject.Find("OptionsBar").GetComponent<UIComponent>();
            absolutePosition = optionsBar.absolutePosition;
        }

        /// <summary>
        /// Updates the options panel button state when the options panel is closed.
        /// </summary>
        private void OptionsPanelClosed() => _optionsPanelToggle.activeStateIndex = 0;

        /// <summary>
        /// Appends a tab button with text to a tabstrip.
        /// </summary>
        /// <param name="tabstrip">Tabstrip to append to.</param>
        /// <param name="template">Button template.</param>
        /// <param name="name">Button name.</param>
        /// <param name="tooltipKey">Tooltop key.</param>
        /// <param name="displayText">Text to display.</param>
        /// <param name="textScale">Text scale.</param>
        /// <param name="leftPadding">Text padding (left).</param>
        /// <param name="rightPadding">Text padding (right).</param>
        /// <param name="topPadding">Text padding (top).</param>
        /// <param name="bottomPadding">Text padding (button).</param>
        private void AddTabTextButton(UITabstrip tabstrip, UIButton template, string name, string tooltipKey, string displayText, float textScale, int leftPadding, int rightPadding, int topPadding, int bottomPadding)
        {
            // Add basic button.
            UIButton newButton = AddTabButton(tabstrip, template, name, tooltipKey);

            // Clear sprites.
            newButton.normalFgSprite = string.Empty;
            newButton.focusedFgSprite = string.Empty;
            newButton.hoveredFgSprite = string.Empty;
            newButton.pressedFgSprite = string.Empty;
            newButton.disabledFgSprite = string.Empty;

            // Set text.
            newButton.text = displayText;
            newButton.textScale = textScale;

            // Set text padding.
            newButton.textPadding.left = leftPadding;
            newButton.textPadding.right = rightPadding;
            newButton.textPadding.top = topPadding;
            newButton.textPadding.bottom = bottomPadding;

            // Set text colour.
            newButton.textColor = new Color32(119, 124, 126, 255);
            newButton.hoveredTextColor = new Color32(110, 113, 114, 255);
            newButton.pressedTextColor = new Color32(172, 175, 176, 255);
            newButton.focusedTextColor = new Color32(187, 224, 235, 255);
            newButton.disabledTextColor = new Color32(66, 69, 70, 255);
        }

        /// <summary>
        /// Appends a tab button with sprite to a tabstrip.
        /// </summary>
        /// <param name="tabstrip">Tabstrip to append to.</param>
        /// <param name="template">Button template.</param>
        /// <param name="name">Button name.</param>
        /// <param name="tooltipKey">Tooltop key.</param>
        private void AddTabSpriteButton(UITabstrip tabstrip, UIButton template, string name, string tooltipKey)
        {
            // Add basic button.
            UIButton newButton = AddTabButton(tabstrip, template, name, tooltipKey);

            // Set sprites.
            string spriteBaseName = "RoadOption" + name;
            newButton.normalFgSprite = spriteBaseName;
            newButton.focusedFgSprite = spriteBaseName + "Focused";
            newButton.hoveredFgSprite = spriteBaseName + "Hovered";
            newButton.pressedFgSprite = spriteBaseName + "Pressed";
            newButton.disabledFgSprite = spriteBaseName + "Disabled";
        }

        /// <summary>
        /// Appends a tab button to a tabstrip.
        /// </summary>
        /// <param name="tabstrip">Tabstrip to append to.</param>
        /// <param name="template">Button template.</param>
        /// <param name="name">Button name.</param>
        /// <param name="tooltipKey">Tooltop key.</param>
        /// <returns>New UIButton.</returns>
        private UIButton AddTabButton(UITabstrip tabstrip, UIButton template, string name, string tooltipKey)
        {
            // Basic setup.
            UIButton newButton = tabstrip.AddTab(name, template, false);
            newButton.name = name;
            newButton.autoSize = false;
            newButton.height = ButtonSize;
            newButton.width = ButtonSize;
            newButton.tooltip = Translations.Translate(tooltipKey);

            return newButton;
        }

        /// <summary>
        /// Control tab index changed event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="index">Selected index.</param>
        private void TabIndexChanged(UIComponent c, int index)
        {
            // Set current tool and/or mode based on new index.
            switch ((ModeIndexes)index)
            {
                case ModeIndexes.Single:
                    ToolsModifierControl.toolController.CurrentTool = LineTool.Instance.BaseTool;
                    StandalonePanelManager<ToolOptionsPanel>.Panel?.Close();
                    _optionsPanelToggle.Hide();
                    break;

                case ModeIndexes.Line:
                    ToolsModifierControl.toolController.CurrentTool = LineTool.Instance;
                    LineTool.Instance.CurrentMode = new LineMode();
                    OpenOptionsPanel();
                    break;

                case ModeIndexes.Curve:
                    ToolsModifierControl.toolController.CurrentTool = LineTool.Instance;
                    LineTool.Instance.CurrentMode = new CurveMode();
                    OpenOptionsPanel();
                    break;

                case ModeIndexes.Freeform:
                    ToolsModifierControl.toolController.CurrentTool = LineTool.Instance;
                    LineTool.Instance.CurrentMode = new FreeformMode();
                    OpenOptionsPanel();
                    break;

                case ModeIndexes.Circle:
                    ToolsModifierControl.toolController.CurrentTool = LineTool.Instance;
                    LineTool.Instance.CurrentMode = new CircleMode();
                    OpenOptionsPanel();
                    break;
            }
        }

        /// <summary>
        /// Opens the options panel.
        /// </summary>
        private void OpenOptionsPanel()
        {
            _optionsPanelToggle.Show();
            _optionsPanelToggle.activeStateIndex = 1;
            StandalonePanelManager<ToolOptionsPanel>.Create();
        }

        /// <summary>
        /// Adds a multi-state toggle button to the specified UIComponent.
        /// </summary>
        /// <param name="parent">Parent UIComponent.</param>
        /// <param name="name">Button name.</param>
        /// <param name="atlas">Button atlas.</param>
        /// <param name="backgroundPrefix">Background sprite common prefix (will be appended with "Zero" and "One" for the two states).</param>
        /// <param name="foregroundPrefix">Foreground sprite common prefix (will be appended with "Zero" and "One" for the two states).</param>
        /// <returns>New UIMultiStateButton.</returns>
        private UIMultiStateButton AddToggleButton(UIComponent parent, string name, UITextureAtlas atlas, string backgroundPrefix, string foregroundPrefix)
        {
            // Create button.
            UIMultiStateButton newButton = parent.AddUIComponent<UIMultiStateButton>();
            newButton.name = name;
            newButton.atlas = atlas;

            // Get sprite sets.
            UIMultiStateButton.SpriteSetState fgSpriteSetState = newButton.foregroundSprites;
            UIMultiStateButton.SpriteSetState bgSpriteSetState = newButton.backgroundSprites;

            // Calculate set names.
            string bgPrefixZero = backgroundPrefix + "Zero";
            string fgPrefixZero = foregroundPrefix + "Zero";
            string bgPrefixOne = backgroundPrefix + "One";
            string fgPrefixOne = foregroundPrefix + "One";

            // State 0 background.
            UIMultiStateButton.SpriteSet bgSpriteSetZero = bgSpriteSetState[0];
            bgSpriteSetZero.normal = bgPrefixZero;
            bgSpriteSetZero.focused = bgPrefixZero + "Focused";
            bgSpriteSetZero.hovered = bgPrefixZero + "Hovered";
            bgSpriteSetZero.pressed = bgPrefixZero + "Pressed";
            bgSpriteSetZero.disabled = bgPrefixZero + "Disabled";

            // State 0 foreground.
            UIMultiStateButton.SpriteSet fgSpriteSetZero = fgSpriteSetState[0];
            fgSpriteSetZero.normal = fgPrefixZero;
            fgSpriteSetZero.focused = fgPrefixZero + "Focused";
            fgSpriteSetZero.hovered = fgPrefixZero + "Hovered";
            fgSpriteSetZero.pressed = fgPrefixZero + "Pressed";
            fgSpriteSetZero.disabled = fgPrefixZero + "Disabled";

            // Add state 1.
            fgSpriteSetState.AddState();
            bgSpriteSetState.AddState();

            // State 1 background.
            UIMultiStateButton.SpriteSet bgSpriteSetOne = bgSpriteSetState[1];
            bgSpriteSetOne.normal = bgPrefixOne;
            bgSpriteSetOne.focused = bgPrefixOne + "Focused";
            bgSpriteSetOne.hovered = bgPrefixOne + "Hovered";
            bgSpriteSetOne.pressed = bgPrefixOne + "Pressed";
            bgSpriteSetOne.disabled = bgPrefixOne + "Disabled";

            // State 1 foreground.
            UIMultiStateButton.SpriteSet fgSpriteSetOne = fgSpriteSetState[1];
            fgSpriteSetOne.normal = fgPrefixOne;
            fgSpriteSetOne.focused = fgPrefixOne + "Focused";
            fgSpriteSetOne.hovered = fgPrefixOne + "Hovered";
            fgSpriteSetOne.pressed = fgPrefixOne + "Pressed";
            fgSpriteSetOne.disabled = fgPrefixOne + "Disabled";

            // Set initial state.
            newButton.state = UIMultiStateButton.ButtonState.Normal;
            newButton.activeStateIndex = 0;

            // Size and appearance.
            newButton.autoSize = false;
            newButton.width = ButtonSize;
            newButton.height = ButtonSize;
            newButton.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            newButton.spritePadding = new RectOffset(0, 0, 0, 0);
            newButton.playAudioEvents = true;

            // Enforce defaults.
            newButton.canFocus = false;
            newButton.enabled = true;
            newButton.isInteractive = true;
            newButton.isVisible = true;

            return newButton;
        }
    }
}
