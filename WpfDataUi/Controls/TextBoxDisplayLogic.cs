﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    public class TextBoxDisplayLogic
    {
        #region Properties

        TextBox mAssociatedTextBox;
        IDataUi mContainer;

        public bool HasUserChangedAnything { get; set; }
        public string TextAtStartOfEditing { get; set; }
        public InstanceMember InstanceMember { get; set; }
        public Type InstancePropertyType { get; set; }

        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }

        public bool HandlesEnter { get; set; } = true;

        #endregion

        public TextBoxDisplayLogic(IDataUi container, TextBox textBox)
        {
            mAssociatedTextBox = textBox;
            mContainer = container;
            mAssociatedTextBox.GotFocus += HandleTextBoxGotFocus;
            mAssociatedTextBox.PreviewKeyDown += HandlePreviewKeydown;
            mAssociatedTextBox.TextChanged += HandleTextChanged;
        }

        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            HasUserChangedAnything = true;
        }

        public void ClampTextBoxValuesToMinMax()
        {
            bool shouldClamp = MinValue.HasValue || MaxValue.HasValue;

            if(shouldClamp)
            {
                decimal parsedDecimal;

                if(decimal.TryParse(mAssociatedTextBox.Text, out parsedDecimal))
                {
                    if (MinValue.HasValue && parsedDecimal < MinValue)
                    {
                        mAssociatedTextBox.Text = MinValue.ToString();
                    }
                    if(MaxValue.HasValue && parsedDecimal > MaxValue)
                    {
                        mAssociatedTextBox.Text = MaxValue.ToString();
                    }
                }
            }
        }

        private void HandlePreviewKeydown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (HandlesEnter))
            {
                e.Handled = true;

                ClampTextBoxValuesToMinMax();

                var result = TryApplyToInstance();

                TextAtStartOfEditing = mAssociatedTextBox.Text;

                if(result == ApplyValueResult.Success)
                {
                    mContainer.Refresh(forceRefreshEvenIfFocused: true);
                }

            }
            else if (e.Key == Key.Escape)
            {
                HasUserChangedAnything = false;
                mAssociatedTextBox.Text = TextAtStartOfEditing;
            }
        }

        void HandleTextBoxGotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            TextAtStartOfEditing = mAssociatedTextBox.Text;

            mAssociatedTextBox.SelectAll();

            HasUserChangedAnything = false;
        }

        public ApplyValueResult TryApplyToInstance()
        {
            object newValue;

            if (HasUserChangedAnything)
            {
                var result = mContainer.TryGetValueOnUi(out newValue);

                if (result == ApplyValueResult.Success)
                {
                    if (InstanceMember.BeforeSetByUi != null)
                    {
                        InstanceMember.CallBeforeSetByUi(mContainer);
                    }

                    // Hold on, the Before set may have actually changed the value, so we should get the value again.
                    mContainer.TryGetValueOnUi(out newValue);

                    if(newValue is string)
                    {
                        newValue = (newValue as string).Replace("\r", "");
                    }
                    // get rid of \r
                    return mContainer.TrySetValueOnInstance(newValue);
                }
                else
                {
                    if(InstanceMember.SetValueError != null)
                    {
                        InstanceMember.SetValueError(mAssociatedTextBox.Text);
                    }

                }
            }
            return ApplyValueResult.Success;
        }

        public string ConvertStringToUsableValue()
        {

            string text = mAssociatedTextBox.Text;

            if (InstancePropertyType.Name == "Vector3" ||
                InstancePropertyType.Name == "Vector2")
            {
                text = text.Replace("{", "").Replace("}", "").Replace("X:", "").Replace("Y:", "").Replace("Z:", "").Replace(" ", ",");

            }
            if (InstancePropertyType.Name == "Color")
            {
                // I think this expects byte values, so we gotta make sure it's not giving us floats
                text = text.Replace("{", "").Replace("}", "").Replace("A:", "").Replace("R:", "").Replace("G:", "").Replace("B:", "").Replace(" ", ",");

            }

            return text;
            
        }

        public string ConvertNumberToString(object value)
        {
            string text = value?.ToString();

            if(value is float)
            {

                // I came to this method through a lot of trial and error.
                // Initially I just used ToString, but that introduces exponential
                // notation, which is confusing and weird for tools to display.
                // I then tried ToString("f0"), which gets rid of exponents, but also
                // truncates at 0 decimals.
                // So I did ToString("#.##############") which will display as many decimals
                // as it can, but this takes numbers like 21.2 and instead shows 21.199997
                // or something. I really want ToString to do ToString unless there is an exponent, and if so
                // then let's fall back to a version that does not show exponents. Which we do depends on if
                // the shown exponent is positive (really large number, abs greater than 1) or really small
                float floatValue = (float)value;
                text = floatValue.ToString();
                if (text.Contains("E"))
                {
                    if (Math.Abs(floatValue) > 1)
                    {
                        // truncating decimals:
                        text = (floatValue).ToString("f0");
                    }
                    else
                    {
                        text = (floatValue).ToString("0.################################");
                    }
                }
            }
            if(value is double)
            {
                double doubleValue = (double)value;
                text = doubleValue.ToString();
                if (text.Contains("e"))
                {
                    if (Math.Abs(doubleValue) > 1)
                    {
                        // truncating decimals:
                        text = (doubleValue).ToString("f0");
                    }
                    else
                    {
                        text = (doubleValue).ToString("#.################################");
                    }
                }
            }

            return text;
        }

        private bool GetIfConverterCanConvert(TypeConverter converter)
        {
            string converterTypeName = converter.GetType().Name;
            if (converterTypeName == "MatrixConverter" ||
                converterTypeName == "CollectionConverter"
                )
            {
                return false;
            }
            return true;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            var result = ApplyValueResult.UnknownError;



            value = null;
            if (!mContainer.HasEnoughInformationToWork() || InstancePropertyType == null)
            {
                result = ApplyValueResult.NotEnoughInformation;
            }
            else
            {
                try
                {
                    var usableString = ConvertStringToUsableValue();

                    var converter = TypeDescriptor.GetConverter(InstancePropertyType);

                    bool canConverterConvert = GetIfConverterCanConvert(converter);

                    if (canConverterConvert)
                    {
                        // The user may have put in a bad value
                        try
                        {
                            if(string.IsNullOrEmpty(usableString))
                            {
                                if(InstancePropertyType == typeof(float))
                                {
                                    value = 0.0f;
                                    result = ApplyValueResult.Success;
                                }
                                else if(InstancePropertyType == typeof(int))
                                {
                                    value = 0;
                                    result = ApplyValueResult.Success;
                                }
                                else if(InstancePropertyType == typeof(double))
                                {
                                    value = 0.0;
                                    result = ApplyValueResult.Success;
                                }
                                else if(InstancePropertyType == typeof(long))
                                {
                                    value = (long)0;
                                    result = ApplyValueResult.Success;
                                }
                                else if(InstancePropertyType == typeof(byte))
                                {
                                    value = (byte)0;
                                    result = ApplyValueResult.Success;
                                }
                            }

                            if(result != ApplyValueResult.Success)
                            {
                                // This used to convert from invariant string, but we want to use commas if the native 
                                // computer settings use commas
                                value = converter.ConvertFromString(usableString);
                                result = ApplyValueResult.Success;
                            }
                        }
                        catch (FormatException)
                        {
                            result = ApplyValueResult.InvalidSyntax;
                        }
                        catch(Exception e)
                        {
                            var wasMathOperation = false;
                            if(e.InnerException is FormatException)
                            {
                                var computedValue = TryHandleMathOperation(usableString, InstancePropertyType);
                                if(computedValue != null)
                                {
                                    wasMathOperation = true;
                                    value = converter.ConvertFrom(computedValue.ToString());
                                }
                                else
                                {
                                    wasMathOperation = false;
                                }
                            }
                            if(wasMathOperation)
                            {
                                result = ApplyValueResult.Success;
                            }
                            else
                            {
                                result = ApplyValueResult.InvalidSyntax;
                            }
                        }
                    }
                    else
                    {
                        result = ApplyValueResult.NotSupported;
                    }
                }
                catch
                {
                    result = ApplyValueResult.UnknownError;
                }
            }

            return result;
        }

        private object TryHandleMathOperation(string usableString, Type instancePropertyType)
        {
            if(instancePropertyType == typeof(float))
            {
                var result = new DataTable().Compute(usableString, null);
            
                if(result is float || result is int || result is decimal || result is double)
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
            else if(instancePropertyType == typeof(int))
            {
                var result = new DataTable().Compute(usableString, null);
                if (result is float || result is int || result is decimal || result is double)
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public SolidColorBrush DefaultValueBackground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 255, 180));
        public SolidColorBrush CustomValueBackground = System.Windows.Media.Brushes.White;

        public void RefreshDisplay()
        {
            if (mContainer.HasEnoughInformationToWork())
            {
                Type type = mContainer.GetPropertyType();

                InstancePropertyType = type;
            }

            object valueOnInstance;
            bool successfulGet = mContainer.TryGetValueOnInstance(out valueOnInstance);
            if (successfulGet)
            {
                mContainer.TrySetValueOnUi(valueOnInstance);
            }


            bool isDefault = InstanceMember.IsDefault;
            if (isDefault)
            {
                mAssociatedTextBox.Background = DefaultValueBackground;
            }
            else
            {
                mAssociatedTextBox.Background = CustomValueBackground;
            }
        }
    }
}
