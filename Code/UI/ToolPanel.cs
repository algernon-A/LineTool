// <copyright file="ToolPanel.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Control panel for the line tool.
    /// TODO: quick-and-dirty hack only for development, not production design.
    /// </summary>
    internal class ToolPanel : StandalonePanel
    {
        // Mode button size.
        private const float ButtonSize = 36f;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolPanel"/> class.
        /// </summary>
        public ToolPanel()
        {
            float currentY = 50f;

            // Basic controls.
            BOBSlider spacingSlider = AddBOBSlider(this, Margin, currentY, PanelWidth - Margin - Margin, "SPACING", 1f, 100f, 0.1f, "Spacing");
            spacingSlider.value = LineTool.Instance.Spacing;
            spacingSlider.eventValueChanged += (c, value) => LineTool.Instance.Spacing = value;
            currentY += 40f;

            UICheckBox fenceCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, currentY, Translations.Translate("FENCEMODE"));
            fenceCheck.isChecked = LineTool.Instance.FenceMode;
            fenceCheck.eventCheckChanged += (c, isChecked) => LineTool.Instance.FenceMode = isChecked;

            currentY += 25f;
            height = currentY;
            currentY += Margin;

            // Add control tab.
            UITabstrip controlTabStrip = AddUIComponent<UITabstrip>();
            controlTabStrip.relativePosition = new Vector2(0f, currentY);
            controlTabStrip.width = ButtonSize * (int)ModeIndexes.NumModes;
            controlTabStrip.height = 36f;
            controlTabStrip.padding.right = 0;

            // Get button template.
            UIButton buttonTemplate = GameObject.Find("ToolMode").GetComponent<UITabstrip>().GetComponentInChildren<UIButton>();

            // Add buttons.
            AddTabTextButton(controlTabStrip, buttonTemplate, "Single", "POINT", "•", 1.5f, 0, 1, 4, 0);
            AddTabSpriteButton(controlTabStrip, buttonTemplate, "Straight", "STRAIGHT_LINE");
            AddTabSpriteButton(controlTabStrip, buttonTemplate, "Curved", "CURVE");
            AddTabTextButton(controlTabStrip, buttonTemplate, "Circle", "CIRCLE", "○", 3.0f, -2, 1, -13, 0);

            // Event handler.
            controlTabStrip.eventSelectedIndexChanged += TabIndexChanged;

            // Set position.
            UIComponent optionsBar = GameObject.Find("OptionsBar").GetComponent<UIComponent>();
            absolutePosition = optionsBar.absolutePosition - new Vector3(0f, currentY);
        }

        // Tool selection indicies.
        private enum ModeIndexes : int
        {
            None = -1,
            Single,
            Line,
            Curve,
            Circle,
            NumModes,
        }

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        public override float PanelWidth => 200f;

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        public override float PanelHeight => 200f;

        /// <summary>
        /// Gets the panel's title.
        /// </summary>
        protected override string PanelTitle => Translations.Translate("MOD_NAME");

        /// <summary>
        /// Adds a BOB slider to the specified component.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="width">Slider width.</param>
        /// <param name="labelKey">Text label translation key.</param>
        /// <param name="minValue">Minimum displayed value.</param>
        /// <param name="maxValue">Maximum displayed value.</param>
        /// <param name="stepSize">Minimum slider step size.</param>
        /// <param name="name">Slider name.</param>
        /// <returns>New BOBSlider.</returns>
        protected BOBSlider AddBOBSlider(UIComponent parent, float xPos, float yPos, float width, string labelKey, float minValue, float maxValue, float stepSize, string name)
        {
            const float SliderY = 18f;
            const float ValueY = 3f;
            const float LabelY = -13f;
            const float SliderHeight = 18f;
            const float FloatTextFieldWidth = 45f;
            const float IntTextFieldWidth = 38f;

            // Slider control.
            BOBSlider newSlider = parent.AddUIComponent<BOBSlider>();
            newSlider.size = new Vector2(width, SliderHeight);
            newSlider.relativePosition = new Vector2(xPos, yPos + SliderY);
            newSlider.name = name;

            // Value field - added to parent, not to slider, otherwise slider catches all input attempts.  Integer textfields (stepsize == 1) have shorter widths.
            float textFieldWidth = stepSize == 1 ? IntTextFieldWidth : FloatTextFieldWidth;
            UITextField valueField = UITextFields.AddTinyTextField(parent, xPos + Margin + newSlider.width - textFieldWidth, yPos + ValueY, textFieldWidth);

            // Title label.
            UILabel titleLabel = UILabels.AddLabel(newSlider, 0f, LabelY, Translations.Translate(labelKey), textScale: 0.7f);

            // Autoscale tile label text, with minimum size 0.35.
            while (titleLabel.width > newSlider.width - textFieldWidth && titleLabel.textScale > 0.35f)
            {
                titleLabel.textScale -= 0.05f;
            }

            // Slider track.
            UISlicedSprite sliderSprite = newSlider.AddUIComponent<UISlicedSprite>();
            sliderSprite.atlas = UITextures.InGameAtlas;
            sliderSprite.spriteName = "BudgetSlider";
            sliderSprite.size = new Vector2(newSlider.width, 9f);
            sliderSprite.relativePosition = new Vector2(0f, 4f);

            // Slider thumb.
            UISlicedSprite sliderThumb = newSlider.AddUIComponent<UISlicedSprite>();
            sliderThumb.atlas = UITextures.InGameAtlas;
            sliderThumb.spriteName = "SliderBudget";
            newSlider.thumbObject = sliderThumb;

            // Set references.
            newSlider.ValueField = valueField;

            // Set initial values.
            newSlider.StepSize = stepSize;
            newSlider.maxValue = maxValue;
            newSlider.minValue = minValue;
            newSlider.TrueValue = 0f;

            return newSlider;
        }

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
                    break;

                case ModeIndexes.Line:
                    ToolsModifierControl.toolController.CurrentTool = LineTool.Instance;
                    LineTool.Instance.CurrentMode = new LineMode();
                    break;

                case ModeIndexes.Curve:
                    ToolsModifierControl.toolController.CurrentTool = LineTool.Instance;
                    LineTool.Instance.CurrentMode = new CurveMode();
                    break;

                case ModeIndexes.Circle:
                    ToolsModifierControl.toolController.CurrentTool = LineTool.Instance;
                    LineTool.Instance.CurrentMode = new CircleMode();
                    break;
            }
        }
    }
}
