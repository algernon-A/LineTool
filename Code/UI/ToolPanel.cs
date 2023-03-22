// <copyright file="ToolPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineTool
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using LineTool.Modes;
    using UnityEngine;

    /// <summary>
    /// Control panel for the line tool.
    /// TODO: quick-and-dirty hack only for development, not production design.
    /// </summary>
    internal class ToolPanel : StandalonePanel
    {
        private readonly UICheckBox _pointCheck;
        private readonly UICheckBox _lineCheck;
        private readonly UICheckBox _circleCheck;
        private readonly UICheckBox _curveCheck;
        private readonly UICheckBox _fenceCheck;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolPanel"/> class.
        /// </summary>
        public ToolPanel()
        {
            float currentY = 50f;

            // Basic controls.
            BOBSlider spacingSlider = AddBOBSlider(this, Margin, currentY, PanelWidth - Margin - Margin, "SPACING", 1f, 100f, 0.1f, "Spacing");
            spacingSlider.value = Tool.Instance.Spacing;
            spacingSlider.eventValueChanged += (c, value) => Tool.Instance.Spacing = value;

            currentY += 40f;
            _pointCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, currentY, Translations.Translate("POINT"));
            currentY += 25f;
            _lineCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, currentY, Translations.Translate("STRAIGHT_LINE"));
            currentY += 25f;
            _circleCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, currentY, Translations.Translate("CIRCLE"));
            currentY += 25f;
            _curveCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, currentY, Translations.Translate("CURVE"));
            currentY += 40f;

            _pointCheck.isChecked = true;
            _pointCheck.eventCheckChanged += (c, isChecked) =>
            {
                if (isChecked)
                {
                    _lineCheck.isChecked = false;
                    _circleCheck.isChecked = false;
                    _curveCheck.isChecked = false;
                    ToolsModifierControl.toolController.CurrentTool = Tool.Instance.BaseTool;
                }
            };

            _lineCheck.eventCheckChanged += (c, isChecked) =>
            {
                if (isChecked)
                {
                    _pointCheck.isChecked = false;
                    _circleCheck.isChecked = false;
                    _curveCheck.isChecked = false;
                    ToolsModifierControl.toolController.CurrentTool = Tool.Instance;
                    Tool.Instance.CurrentMode = new LineMode();
                }
            };

            _circleCheck.eventCheckChanged += (c, isChecked) =>
            {
                if (isChecked)
                {
                    _pointCheck.isChecked = false;
                    _lineCheck.isChecked = false;
                    _curveCheck.isChecked = false;
                    ToolsModifierControl.toolController.CurrentTool = Tool.Instance;
                    Tool.Instance.CurrentMode = new CircleMode();
                }
            };

            _curveCheck.eventCheckChanged += (c, isChecked) =>
            {
                if (isChecked)
                {
                    _pointCheck.isChecked = false;
                    _lineCheck.isChecked = false;
                    _circleCheck.isChecked = false;
                    ToolsModifierControl.toolController.CurrentTool = Tool.Instance;
                    Tool.Instance.CurrentMode = new CurveMode();
                }
            };

            currentY += 25f;
            _fenceCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, currentY, Translations.Translate("FENCEMODE"));
            _fenceCheck.isChecked = Tool.Instance.FenceMode;
            _fenceCheck.eventCheckChanged += (c, isChecked) => Tool.Instance.FenceMode = isChecked;

            currentY += 25f;
            height = currentY;
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
    }
}
