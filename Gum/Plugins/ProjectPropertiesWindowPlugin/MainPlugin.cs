﻿using Gum.Commands;
using Gum.DataTypes;
using Gum.Gui.Controls;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.Behaviors;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using WpfDataUi.EventArguments;

namespace Gum.Plugins.PropertiesWindowPlugin
{
    /// <summary>
    /// Plugin for displaying project properties
    /// </summary>
    [Export(typeof(PluginBase))]
    class MainPlugin : InternalPlugin
    {
        #region Fields/Properties

        ProjectPropertiesControl control;

        ProjectPropertiesViewModel viewModel;

        #endregion

        public override void StartUp()
        {
            this.AddMenuItem(new List<string> { "Edit", "Properties" }).Click += HandlePropertiesClicked;

            viewModel = new PropertiesWindowPlugin.ProjectPropertiesViewModel();
            viewModel.PropertyChanged += HandlePropertyChanged;

            // todo - handle loading new Gum project when this window is shown - re-call BindTo
            this.ProjectLoad += HandleProjectLoad;
        }

        private void HandleProjectLoad(GumProjectSave obj)
        {
            if(control != null && viewModel != null)
            {
                viewModel.SetFrom(ProjectManager.Self.GeneralSettingsFile, ProjectState.Self.GumProjectSave);
                control.ViewModel = null;
                control.ViewModel = viewModel;
            }
        }

        private void HandlePropertiesClicked(object sender, EventArgs e)
        {
            try
            {
                if(control == null)
                {
                    control = new ProjectPropertiesControl();

                    control.CloseClicked += HandleCloseClicked;
                }
                viewModel.SetFrom(ProjectManager.Self.GeneralSettingsFile, ProjectState.Self.GumProjectSave);

                GumCommands.Self.GuiCommands.AddControl(control, "Project Properties");
                GumCommands.Self.GuiCommands.ShowControl(control);
                control.ViewModel = viewModel;
            }
            catch(Exception ex)
            {
                GumCommands.Self.GuiCommands.PrintOutput($"Error showing project properties:\n{ex.ToString()}");
            }
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(!viewModel.IsUpdatingFromModel)
            {
                viewModel.ApplyToModelObjects();

                var shouldSaveAndRefresh = true;
                var shouldReloadContent = false;
                switch(e.PropertyName)
                {
                    case nameof(viewModel.LocalizationFile):


                        if (!string.IsNullOrEmpty(viewModel.LocalizationFile) && FileManager.IsRelative(viewModel.LocalizationFile) == false)
                        {
                            viewModel.LocalizationFile = FileManager.MakeRelative(viewModel.LocalizationFile, 
                                GumState.Self.ProjectState.ProjectDirectory);
                            shouldSaveAndRefresh = false;
                        }
                        else
                        {
                            GumCommands.Self.FileCommands.LoadLocalizationFile();

                            WireframeObjectManager.Self.RefreshAll(forceLayout: true, forceReloadTextures: false);
                        }
                        break;
                    case nameof(viewModel.LanguageIndex):
                        LocalizationManager.CurrentLanguage = viewModel.LanguageIndex;
                        break;
                    case nameof(viewModel.ShowLocalization):
                        shouldSaveAndRefresh = true;
                        break;
                    case nameof(viewModel.FontRanges):
                        var isValid = BmfcSave.GetIfIsValidRange(viewModel.FontRanges);
                        var didFixChangeThings = false;
                        if(!isValid)
                        {
                            var fixedRange = BmfcSave.TryFixRange(viewModel.FontRanges);
                            if(fixedRange != viewModel.FontRanges)
                            {
                                // this will recursively call this property, so we'll use this bool to leave this method
                                didFixChangeThings = true;
                                viewModel.FontRanges = fixedRange;
                            }
                        }

                        if(!didFixChangeThings)
                        {
                            if(isValid == false)
                            {
                                GumCommands.Self.GuiCommands.ShowMessage("The entered Font Range is not valid.");
                            }
                            else
                            {
                                if(GumState.Self.ProjectState.GumProjectSave != null)
                                {
                                    FontManager.Self.DeleteFontCacheFolder();

                                    FontManager.Self.CreateAllMissingFontFiles(
                                        ProjectState.Self.GumProjectSave);

                                }
                                shouldSaveAndRefresh = true;
                                shouldReloadContent = true;
                            }
                        }
                        break;
                }

                if(shouldSaveAndRefresh)
                {
                    GumCommands.Self.WireframeCommands.Refresh(forceLayout:true, forceReloadContent: shouldReloadContent);

                    GumCommands.Self.FileCommands.TryAutoSaveProject();
                }
            }
        }

        private void HandleCloseClicked(object sender, EventArgs e)
        {
            GumCommands.Self.GuiCommands.RemoveControl(control);
        }
    }
}
