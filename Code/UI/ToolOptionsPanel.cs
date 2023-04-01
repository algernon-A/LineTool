// <copyright file="ToolOptionsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LineToolMod
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Control panel for the line tool.
    /// TODO: quick-and-dirty hack only for development, not production design.
    /// </summary>
    internal class ToolOptionsPanel : StandalonePanel
    {
        // Panel components.
        private readonly BOBSlider _spacingSlider;
        private readonly UIButton _stepButton;
        private readonly UIButton _skipButton;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolOptionsPanel"/> class.
        /// </summary>
        public ToolOptionsPanel()
        {
            const float DoubleMargin = Margin * 2f;
            float currentY = 50f;

            // Spacing slider.
            _spacingSlider = AddBOBSlider(this, DoubleMargin, currentY, PanelWidth - DoubleMargin - DoubleMargin, "SPACING", 1f, 100f, 0.1f, "Spacing");
            _spacingSlider.value = LineTool.Instance.Spacing;
            _spacingSlider.eventValueChanged += (c, value) => LineTool.Instance.Spacing = value;
            currentY += 40f;

            // Rotation slider.
            BOBSlider rotationSlider = AddBOBSlider(this, DoubleMargin, currentY, PanelWidth - DoubleMargin - DoubleMargin, "ROTATION", 0f, 360f, 0.1f, "Rotation");
            rotationSlider.value = LineTool.Instance.Rotation * Mathf.Rad2Deg;
            rotationSlider.eventValueChanged += (c, value) => LineTool.Instance.Rotation = value * Mathf.Deg2Rad;
            currentY += 40f;

            // Relative rotation check.
            UICheckBox relativeRotationCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, currentY, Translations.Translate("ROTATION_RELATIVE"));
            relativeRotationCheck.isChecked = LineTool.Instance.RelativeRotation;
            relativeRotationCheck.eventCheckChanged += (c, isChecked) => LineTool.Instance.RelativeRotation = isChecked;
            currentY += 25f;

            // Step check.
            UICheckBox stepCheck = UICheckBoxes.AddLabelledCheckBox(this, Margin, currentY, Translations.Translate("STEP_ENABLED"));
            stepCheck.isChecked = LineTool.Instance.StepMode;
            stepCheck.eventCheckChanged += (c, isChecked) => LineTool.Instance.StepMode = isChecked;
            currentY += 25f;

            // Step button.
            float buttonWidth = (PanelWidth / 2f) - (Margin * 2f);
            _stepButton = UIButtons.AddEvenSmallerButton(this, Margin, currentY, Translations.Translate("STEP"), buttonWidth);
            _stepButton.eventClicked += (c, p) =>
            {
                LineTool.Instance.Step();
            };

            // Skip button.
            _skipButton = UIButtons.AddEvenSmallerButton(this, (Margin * 3f) + buttonWidth, currentY, Translations.Translate("SKIP"), buttonWidth);
            _skipButton.eventClicked += (c, p) =>
            {
                LineTool.Instance.Skip();
            };

            // Set initial state.
            UpdateButtonStates();
        }

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        public override float PanelWidth => 36f * 7f;

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        public override float PanelHeight => 210f;

        /// <summary>
        /// Gets the panel's title.
        /// </summary>
        protected override string PanelTitle => Translations.Translate("MOD_NAME");

        /// <summary>
        /// Updates button states to reflect the current tool state.
        /// </summary>
        internal void UpdateButtonStates()
        {
            // Set according to current stepping state.
            if (LineTool.Instance != null && LineTool.Instance.Stepping)
            {
                _spacingSlider.Hide();
                _skipButton.Enable();
                _stepButton.Enable();
            }
            else
            {
                _spacingSlider.Show();
                _skipButton.Disable();
                _stepButton.Disable();
            }
        }

        /// <summary>
        /// Gets the panel's default position.
        /// </summary>
        protected override void ApplyDefaultPosition()
        {
            // Set position.
            UIComponent optionsBar = GameObject.Find("OptionsBar").GetComponent<UIComponent>();
            absolutePosition = optionsBar.absolutePosition - new Vector3(0f, PanelHeight + Margin);
        }

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
        private BOBSlider AddBOBSlider(UIComponent parent, float xPos, float yPos, float width, string labelKey, float minValue, float maxValue, float stepSize, string name)
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
