﻿using FlatRedBall.SpecializedXnaControls;
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using InputLibrary;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextureCoordinateSelectionPlugin.ViewModels;
using TextureCoordinateSelectionPlugin.Views;

namespace TextureCoordinateSelectionPlugin.Logic
{
    public enum RefreshType
    {
        Force,
        OnlyIfGrabbed
    }

    public class ControlLogic : Singleton<ControlLogic>
    {
        LineRectangle textureOutlineRectangle = null;

        MainControlViewModel ViewModel;

        LineGrid lineGrid;

        object oldTextureLeftValue;
        object oldTextureTopValue;
        object oldTextureWidthValue;
        object oldTextureHeightValue;

        /// <summary>
        /// This can be set to false to prevent the
        /// view from refreshing, which we want to do when
        /// the view itself is what set the values
        /// </summary>
        bool shouldRefreshAccordingToVariableSets = true;
        MainControl mainControl;

        Texture2D CurrentTexture
        {
            get => null;// mainControl.InnerControl.CurrentTexture;
            set
            {
                ;
            }// mainControl.InnerControl.CurrentTexture = value;
        }

        public void CreateControl()
        {
            mainControl = new MainControl();
            //var control = new ImageRegionSelectionControl();
            var innerControl = mainControl.InnerControl;

            //innerControl.AvailableZoomLevels = new int[]
            //{
            //    3200,
            //    1600,
            //    1200,
            //    800,
            //    500,
            //    300,
            //    200,
            //    150,
            //    100,
            //    75,
            //    50,
            //    33,
            //    25,
            //    10,
            //};
            //innerControl.StartRegionChanged += HandleStartRegionChanged;
            //innerControl.RegionChanged += HandleRegionChanged;
            //innerControl.EndRegionChanged += HandleEndRegionChanged;

            ////GumCommands.Self.GuiCommands.AddWinformsControl(control, "Texture Coordinates", TabLocation.Right);

            //GumCommands.Self.GuiCommands.AddControl(mainControl, "Texture Coordinates", TabLocation.RightBottom);
            //innerControl.DoubleClick += (not, used) =>
            //    HandleRegionDoubleClicked(innerControl, ref textureOutlineRectangle);

            ViewModel = new MainControlViewModel();
            mainControl.DataContext = ViewModel;

            ViewModel.PropertyChanged += HandleViewModelPropertyChanged;

            CreateLineRectangle();

            RefreshLineGrid();
        }

        private void CreateLineRectangle()
        {
            lineGrid = new LineGrid(mainControl.InnerControl.SystemManagers);
            lineGrid.ColumnWidth = 16;
            lineGrid.ColumnCount = 16;

            lineGrid.RowWidth = 16;
            lineGrid.RowCount = 16;

            lineGrid.Visible = true;
            lineGrid.Z = 1;

            var alpha = .2f;

            // premultiplied
            lineGrid.Color = new Microsoft.Xna.Framework.Color(alpha, alpha, alpha, alpha);

            mainControl.InnerControl.SystemManagers.Renderer.MainLayer.Add(lineGrid);
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //void RefreshSnappingGridSize()
            //{
            //    if (!ViewModel.IsSnapToGridChecked)
            //    {
            //        mainControl.InnerControl.SnappingGridSize = null;
            //    }
            //    else
            //    {
            //        mainControl.InnerControl.SnappingGridSize = ViewModel.SelectedSnapToGridValue;
            //    }
            //}

            //switch (e.PropertyName)
            //{
            //    case nameof(MainControlViewModel.IsSnapToGridChecked):
            //        RefreshSnappingGridSize();
            //        RefreshLineGrid();

            //        break;
            //    case nameof(MainControlViewModel.SelectedSnapToGridValue):
            //        RefreshSnappingGridSize();
            //        RefreshLineGrid();
            //        break;
            //}
        }

        internal void Refresh(Texture2D textureToAssign)
        {
            //mainControl.InnerControl.CurrentTexture = textureToAssign;

            //RefreshSelector(Logic.RefreshType.OnlyIfGrabbed);

            //RefreshOutline(mainControl.InnerControl, ref textureOutlineRectangle);

            //RefreshLineGrid();
        }

        private void RefreshLineGrid()
        {
            lineGrid.Visible = ViewModel.IsSnapToGridChecked;


            lineGrid.ColumnWidth = ViewModel.SelectedSnapToGridValue;
            lineGrid.RowWidth = ViewModel.SelectedSnapToGridValue;

            if (CurrentTexture != null)
            {
                var totalWidth = CurrentTexture.Width;

                var columnCount = (totalWidth / lineGrid.ColumnWidth);
                if(columnCount != (int)columnCount)
                {
                    columnCount++;
                }

                lineGrid.ColumnCount = (int)columnCount;


                var totalHeight = CurrentTexture.Height;
                var rowCount = (totalHeight / lineGrid.RowWidth);
                if(rowCount != (int)rowCount)
                {
                    rowCount++;
                }

                lineGrid.RowCount = (int)rowCount;
            }
        }

        public void HandleRegionDoubleClicked(ImageRegionSelectionControl control, ref LineRectangle textureOutlineRectangle)
        {
            var state = SelectedState.Self.SelectedStateSave;
            var instancePrefix = SelectedState.Self.SelectedInstance?.Name;
            var graphicalUiElement = SelectedState.Self.SelectedIpso as GraphicalUiElement;

            if (!string.IsNullOrEmpty(instancePrefix))
            {
                instancePrefix += ".";
            }

            if (state != null && graphicalUiElement != null)
            {
                graphicalUiElement.TextureAddress = TextureAddress.Custom;

                var cursorX = (int)control.XnaCursor.GetWorldX(control.SystemManagers);
                var cursorY = (int)control.XnaCursor.GetWorldY(control.SystemManagers);


                int left = Math.Max(0, cursorX - 32);
                int top = Math.Max(0, cursorY - 32);
                int right = left + 64;
                int bottom = top + 64;

                int width = right - left;
                int height = bottom - top;

                graphicalUiElement.TextureLeft = MathFunctions.RoundToInt(left);
                graphicalUiElement.TextureTop = MathFunctions.RoundToInt(top);

                graphicalUiElement.TextureWidth = MathFunctions.RoundToInt(width);
                graphicalUiElement.TextureHeight = MathFunctions.RoundToInt(height);

                state.SetValue($"{instancePrefix}Texture Left", left, "int");
                state.SetValue($"{instancePrefix}Texture Top", top, "int");
                state.SetValue($"{instancePrefix}Texture Width", width, "int");
                state.SetValue($"{instancePrefix}Texture Height", height, "int");
                state.SetValue($"{instancePrefix}Texture Address",
                    Gum.Managers.TextureAddress.Custom, nameof(TextureAddress));

                RefreshOutline(control, ref textureOutlineRectangle);

                RefreshSelector(RefreshType.Force);
            }


        }

        private void HandleStartRegionChanged(object sender, EventArgs e)
        {
            UndoManager.Self.RecordUndo();

            var state = SelectedState.Self.SelectedStateSave;

            var instancePrefix = SelectedState.Self.SelectedInstance?.Name;

            if (!string.IsNullOrEmpty(instancePrefix))
            {
                instancePrefix += ".";
            }

            oldTextureLeftValue = state.GetValue($"{instancePrefix}Texture Left");
            oldTextureTopValue = state.GetValue($"{instancePrefix}Texture Top");
            oldTextureWidthValue = state.GetValue($"{instancePrefix}Texture Width");
            oldTextureHeightValue = state.GetValue($"{instancePrefix}Texture Height");
        }

        private void HandleRegionChanged(object sender, EventArgs e)
        {
            var control = sender as ImageRegionSelectionControl;

            var graphicalUiElement = SelectedState.Self.SelectedIpso as GraphicalUiElement;

            if (graphicalUiElement != null)
            {
                var selector = control.RectangleSelector;

                graphicalUiElement.TextureLeft = MathFunctions.RoundToInt(selector.Left);
                graphicalUiElement.TextureTop = MathFunctions.RoundToInt(selector.Top);

                graphicalUiElement.TextureWidth = MathFunctions.RoundToInt(selector.Width);
                graphicalUiElement.TextureHeight = MathFunctions.RoundToInt(selector.Height);

                var state = SelectedState.Self.SelectedStateSave;
                var instancePrefix = SelectedState.Self.SelectedInstance?.Name;

                if (!string.IsNullOrEmpty(instancePrefix))
                {
                    instancePrefix += ".";
                }



                state.SetValue($"{instancePrefix}Texture Left", graphicalUiElement.TextureLeft, "int");
                state.SetValue($"{instancePrefix}Texture Top", graphicalUiElement.TextureTop, "int");
                state.SetValue($"{instancePrefix}Texture Width", graphicalUiElement.TextureWidth, "int");
                state.SetValue($"{instancePrefix}Texture Height", graphicalUiElement.TextureHeight, "int");

                
                GumCommands.Self.GuiCommands.RefreshPropertyGridValues();
            }
        }

        private void HandleEndRegionChanged(object sender, EventArgs e)
        {
            var element = SelectedState.Self.SelectedElement;
            var instance = SelectedState.Self.SelectedInstance;

            shouldRefreshAccordingToVariableSets = false;
            {
                // This could be really heavy if we notify everyone of the changes. We should only do it when the editing stops...
                SetVariableLogic.Self.ReactToPropertyValueChanged("Texture Left", oldTextureLeftValue,
                    element, instance, refresh: false);
                SetVariableLogic.Self.ReactToPropertyValueChanged("Texture Top", oldTextureTopValue,
                    element, instance, refresh: false);
                SetVariableLogic.Self.ReactToPropertyValueChanged("Texture Width", oldTextureWidthValue,
                    element, instance, refresh: false);
                SetVariableLogic.Self.ReactToPropertyValueChanged("Texture Height", oldTextureHeightValue,
                    element, instance, refresh: false);
            }
            shouldRefreshAccordingToVariableSets = true;

            UndoManager.Self.RecordUndo();

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }

        public void RefreshOutline(ImageRegionSelectionControl control, ref LineRectangle textureOutlineRectangle)
        {
            var shouldShowOutline = control.CurrentTexture != null;
            if (shouldShowOutline)
            {
                if (textureOutlineRectangle == null)
                {
                    textureOutlineRectangle = new LineRectangle(control.SystemManagers);
                    textureOutlineRectangle.IsDotted = false;
                    textureOutlineRectangle.Color = new Microsoft.Xna.Framework.Color(255, 255, 255, 128);
                    control.SystemManagers.ShapeManager.Add(textureOutlineRectangle);
                }
                textureOutlineRectangle.Width = control.CurrentTexture.Width;
                textureOutlineRectangle.Height = control.CurrentTexture.Height;
                textureOutlineRectangle.Visible = true;
            }
            else
            {
                if (textureOutlineRectangle != null)
                {
                    textureOutlineRectangle.Visible = false;
                }
            }
        }

        public void RefreshSelector(RefreshType refreshType)
        {
            //if(mainControl.InnerControl.CurrentTexture == null)
            //{
            //    return;
            //}

            //var control = mainControl.InnerControl;

            //// early out
            //if (refreshType == RefreshType.OnlyIfGrabbed &&
            //    control.RectangleSelector != null &&
            //    control.RectangleSelector.SideGrabbed != FlatRedBall.SpecializedXnaControls.RegionSelection.ResizeSide.None)
            //{
            //    return;
            //}

            //if(shouldRefreshAccordingToVariableSets == false)
            //{
            //    return;
            //}

            //if(SelectedState.Self.SelectedElement == null)
            //{
            //    // in case a behavior is selected:
            //    return;
            //}

            //////////////end early out///////////////////////////////

            var shouldClearOut = true;
            if (SelectedState.Self.SelectedStateSave != null)
            {

                var graphicalUiElement = SelectedState.Self.SelectedIpso as GraphicalUiElement;
                var rfv = new RecursiveVariableFinder(SelectedState.Self.SelectedStateSave);
                var instancePrefix = SelectedState.Self.SelectedInstance?.Name;

                if (!string.IsNullOrEmpty(instancePrefix))
                {
                    instancePrefix += ".";
                }

                var textureAddress = rfv.GetValue<Gum.Managers.TextureAddress>($"{instancePrefix}Texture Address");
                if (textureAddress == Gum.Managers.TextureAddress.Custom)
                {
                    //shouldClearOut = false;
                    //control.DesiredSelectorCount = 1;

                    //var selector = control.RectangleSelector;


                    //selector.Left = rfv.GetValue<int>($"{instancePrefix}Texture Left");
                    //selector.Width = rfv.GetValue<int>($"{instancePrefix}Texture Width");

                    //selector.Top = rfv.GetValue<int>($"{instancePrefix}Texture Top");
                    //selector.Height = rfv.GetValue<int>($"{instancePrefix}Texture Height");

                    //selector.Visible = true;
                    //selector.ShowHandles = true;
                    //selector.ShowMoveCursorWhenOver = true;

                    //control.SystemManagers.Renderer.Camera.X =
                    //    selector.Left + selector.Width / 2.0f;
                    //control.SystemManagers.Renderer.Camera.Y =
                    //    selector.Top + selector.Height / 2.0f;

                }
            }

            if (shouldClearOut)
            {
                //control.DesiredSelectorCount = 0;
            }
        }
    }
}
