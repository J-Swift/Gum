﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Interaction logic for SliderDisplay.xaml
    /// </summary>
    public partial class SliderDisplay : UserControl, IDataUi, ISetDefaultable
    {
        #region Fields/Properties

        public double MaxValue
        {
            get => Slider.Maximum;
            set
            {
                Slider.Maximum = value;
                mTextBoxLogic.MaxValue = (decimal)this.MaxValue;
            }
        }

        public double MinValue
        {
            get => Slider.Minimum;
            set
            {
                Slider.Minimum = value;
                mTextBoxLogic.MinValue = (decimal)this.MinValue;
            }
        }

        /// <summary>
        /// The number of decimal points to show on the text box when dragging the slider.
        /// </summary>
        public int DecimalPointsFromSlider { get; set; } = 2;

        InstanceMember mInstanceMember;
        public InstanceMember InstanceMember
        {
            get
            {
                return mInstanceMember;
            }
            set
            {
                mTextBoxLogic.InstanceMember = value;

                bool valueChanged = mInstanceMember != value;
                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged -= HandlePropertyChange;
                }
                mInstanceMember = value;

                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged += HandlePropertyChange;
                }


                Refresh();
            }
        }

        TextBoxDisplayLogic mTextBoxLogic;

        public bool SuppressSettingProperty { get; set; }

        #endregion

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

            }
        }

        public SliderDisplay()
        {
            InitializeComponent();

            mTextBoxLogic = new TextBoxDisplayLogic(this, TextBox);
            mTextBoxLogic.MinValue = 0;
            mTextBoxLogic.MaxValue = (decimal)this.MaxValue;

            this.RefreshContextMenu(TextBox.ContextMenu);
            this.ContextMenu = TextBox.ContextMenu;
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            // If the user is editing a value, we don't want to change
            // the value under the cursor
            // If we're default, then go ahead and change the value

            var isFocused = this.TextBox.IsFocused || this.Slider.IsFocused;

            bool canRefresh =
                isFocused == false || forceRefreshEvenIfFocused || mTextBoxLogic.InstanceMember.IsDefault;

            if (canRefresh)
            {

                SuppressSettingProperty = true;

                mTextBoxLogic.RefreshDisplay();

                this.Label.Text = InstanceMember.DisplayName;

                SuppressSettingProperty = false;
            }
        }

        public void SetToDefault()
        {

        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            return mTextBoxLogic.TryGetValueOnUi(out value);
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            if(valueOnInstance != null)
            {
                SetTextBoxValue(valueOnInstance);

                SetSliderValue(valueOnInstance);
                return ApplyValueResult.Success;
            }
            else
            {
                return ApplyValueResult.NotSupported;
            }
        }

        private void SetTextBoxValue(object valueOnInstance)
        {
            this.TextBox.Text = mTextBoxLogic.ConvertNumberToString(valueOnInstance);
        }

        private void SetSliderValue(object valueOnInstance)
        {
            if (valueOnInstance is float)
            {
                this.Slider.Value = (float)valueOnInstance;
            }
            else if (valueOnInstance is double)
            {
                this.Slider.Value = (double)valueOnInstance;
            }

            // todo: support int...
        }

        DateTime lastSliderTime = new DateTime();
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // This is required to prevent weird flickering on the slider. Putting 100 ms frequency limiter makes everything work just fine.
            // It's a hack but...not sure what else to do. I also have Slider_DragCompleted so the last value is always pushed.
            var timeSince = DateTime.Now - lastSliderTime;
            if(timeSince.TotalMilliseconds > 100)
            {
                HandleValueChanged();
                lastSliderTime = DateTime.Now;
            }
        }

        private void HandleValueChanged()
        {
            if (!SuppressSettingProperty)
            {

                var value = Slider.Value;
                this.TextBox.Text = value.ToString($"f{DecimalPointsFromSlider}");

                // don't use this method, we want to control the decimals
                //SetTextBoxValue(value);

                mTextBoxLogic.TryApplyToInstance();
            }
        }

        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            mTextBoxLogic.ClampTextBoxValuesToMinMax();

            mTextBoxLogic.TryApplyToInstance();
        }

        private void Slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            HandleValueChanged();
        }
    }
}
