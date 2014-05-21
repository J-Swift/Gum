﻿using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gum.RenderingLibrary;

namespace Gum.Managers
{
    public partial class PropertyGridManager
    {
        internal void PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {

            string changedMember = e.ChangedItem.PropertyDescriptor.Name;
            object oldValue = e.OldValue;



            PropertyValueChanged(changedMember, oldValue);
        }

        public void PropertyValueChanged(string changedMember, object oldValue)
        {
            object selectedObject = SelectedState.Self.SelectedStateSave;

            // We used to suppress
            // saving - not sure why.
            //bool saveProject = true;

            if (selectedObject is StateSave)
            {
                ElementSave parentElement = ((StateSave)selectedObject).ParentContainer;
                InstanceSave instance = SelectedState.Self.SelectedInstance;

                if (instance != null)
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(instance.Name + "." + changedMember);
                }
                else
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(changedMember);
                }
                // Why do we do this before reacting to names?  I think we want to do it after
                //ElementTreeViewManager.Self.RefreshUI();

                ReactToChangedMember(changedMember, oldValue, parentElement, instance);



                // This used to be above the React methods but
                // we probably want to referesh the UI after everything
                // else has changed, don't we?
                // I think this code makes things REALLY slow - we only want to refresh one of the tree nodes:
                //ElementTreeViewManager.Self.RefreshUI();
                ElementTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);

            }


            // Save the change
            if (SelectedState.Self.SelectedElement != null)
            {
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }


            // Inefficient but let's do this for now - we can make it more efficient later
            WireframeObjectManager.Self.RefreshAll(true);
            SelectionManager.Self.Refresh();
        }

        private void ReactToChangedMember(string changedMember, object oldValue, ElementSave parentElement, InstanceSave instance)
        {
            ReactIfChangedMemberIsName(parentElement, instance, changedMember, oldValue);

            ReactIfChangedMemberIsBaseType(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsFont(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsUnitType(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsTexture(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsTextureAddress(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsParent(parentElement, changedMember, oldValue);

            PluginManager.Self.VariableSet(parentElement, instance, changedMember, oldValue);
        }

        private static void ReactIfChangedMemberIsName(ElementSave container, InstanceSave instance, string changedMember, object oldValue)
        {
            if (changedMember == "Name")
            {
                RenameManager.Self.HandleRename(container, instance, (string)oldValue);

            }
        }

        private static void ReactIfChangedMemberIsBaseType(object s, string changedMember, object oldValue)
        {
            if (changedMember == "Base Type")
            {
                ElementSave asElementSave = s as ElementSave;

                asElementSave.ReactToChangedBaseType(SelectedState.Self.SelectedInstance, oldValue.ToString());
            }

        }

        private void ReactIfChangedMemberIsFont(ElementSave parentElement, string changedMember, object oldValue)
        {
            if (changedMember == "Font" || changedMember == "FontSize")
            {
                FontManager.Self.ReactToFontValueSet();

            }
        }

        private void ReactIfChangedMemberIsUnitType(ElementSave parentElement, string changedMember, object oldValue)
        {
            StateSave stateSave = SelectedState.Self.SelectedStateSave;

            IPositionedSizedObject currentIpso =
                WireframeObjectManager.Self.GetSelectedRepresentation();

            float parentWidth = ObjectFinder.Self.GumProjectSave.DefaultCanvasWidth;
            float parentHeight = ObjectFinder.Self.GumProjectSave.DefaultCanvasHeight;

            float fileWidth = 0;
            float fileHeight = 0;

            if (currentIpso != null)
            {
                currentIpso.GetFileWidthAndHeight(out fileWidth, out fileHeight);
                if (currentIpso.Parent != null)
                {
                    parentWidth = currentIpso.Parent.Width;
                    parentHeight = currentIpso.Parent.Height;
                }
            }


            float outX = 0;
            float outY = 0;
            float valueToSet = 0;
            string variableToSet = null;

            bool isWidthOrHeight = false;

            bool wasAnythingSet = false;

            if (changedMember == "X Units" || changedMember == "Y Units" || changedMember == "Width Units" || changedMember == "Height Units")
            {
                object unitType = EditingManager.GetCurrentValueForVariable(changedMember, SelectedState.Self.SelectedInstance);
                XOrY xOrY = XOrY.X;
                if (changedMember == "X Units")
                {
                    variableToSet = "X";
                    xOrY = XOrY.X;
                }
                else if (changedMember == "Y Units")
                {
                    variableToSet = "Y";
                    xOrY = XOrY.Y;
                }
                else if (changedMember == "Width Units")
                {
                    variableToSet = "Width";
                    isWidthOrHeight = true;
                    xOrY = XOrY.X;

                }
                else if (changedMember == "Height Units")
                {
                    variableToSet = "Height";
                    isWidthOrHeight = true;
                    xOrY = XOrY.Y;
                }



                float valueOnObject = (float)stateSave.GetValueRecursive(GetQualifiedName(variableToSet));

                if (xOrY == XOrY.X)
                {
                    UnitConverter.Self.ConvertToPixelCoordinates(
                        valueOnObject, 0, oldValue, null, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                    if (isWidthOrHeight && outX == 0)
                    {
                        outX = fileWidth;
                    }

                    UnitConverter.Self.ConvertToUnitTypeCoordinates(
                        outX, outY, unitType, null, parentWidth, parentHeight, fileWidth, fileHeight, out valueToSet, out outY);
                }
                else
                {
                    UnitConverter.Self.ConvertToPixelCoordinates(
                        0, valueOnObject, null, oldValue, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                    if (isWidthOrHeight && outY == 0)
                    {
                        outY = fileHeight;
                    }

                    UnitConverter.Self.ConvertToUnitTypeCoordinates(
                        outX, outY, null, unitType, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out valueToSet);
                }
                wasAnythingSet = true;

            }

            if (wasAnythingSet)
            {
                InstanceSave instanceSave = SelectedState.Self.SelectedInstance;
                if (SelectedState.Self.SelectedInstance != null)
                {
                    variableToSet = SelectedState.Self.SelectedInstance.Name + "." + variableToSet;
                }

                stateSave.SetValue(variableToSet, valueToSet, instanceSave);

            }


        }

        private void ReactIfChangedMemberIsTexture(ElementSave parentElement, string changedMember, object oldValue)
        {
            VariableSave variable = SelectedState.Self.SelectedVariableSave;
            // Eventually need to handle tunneled variables
            if (variable != null && variable.GetRootName() == "SourceFile")
            {
                StateSave stateSave = SelectedState.Self.SelectedStateSave;

                RecursiveVariableFinder rvf = new RecursiveVariableFinder(stateSave);

                stateSave.SetValue("AnimationFrames", new List<string>());

            }

        }

        private void ReactIfChangedMemberIsParent(ElementSave parentElement, string changedMember, object oldValue)
        {
            VariableSave variable = SelectedState.Self.SelectedVariableSave;
            // Eventually need to handle tunneled variables
            if (variable != null && changedMember == "Parent")
            {
                if ((variable.Value as string) == "<NONE>")
                {
                    variable.Value = null;
                }

            }
        }

        private void ReactIfChangedMemberIsTextureAddress(ElementSave parentElement, string changedMember, object oldValue)
        {
            if (changedMember == "Texture Address")
            {
                RecursiveVariableFinder rvf;

                var instance = SelectedState.Self.SelectedInstance;
                if (instance != null)
                {
                    rvf = new RecursiveVariableFinder(SelectedState.Self.SelectedInstance, parentElement);
                }
                else
                {
                    rvf = new RecursiveVariableFinder(parentElement.DefaultState);
                }

                var textureAddress = rvf.GetValue<TextureAddress>("Texture Address");

                if (textureAddress == TextureAddress.Custom)
                {
                    string sourceFile = rvf.GetValue<string>("SourceFile");

                    if (!string.IsNullOrEmpty(sourceFile))
                    {
                        string absolute = ProjectManager.Self.MakeAbsoluteIfNecessary(sourceFile);

                        if (System.IO.File.Exists(absolute))
                        {
                            var texture = LoaderManager.Self.Load(absolute, null);

                            if (texture != null && instance != null)
                            {
                                parentElement.DefaultState.SetValue(instance.Name + ".Texture Top", 0);
                                parentElement.DefaultState.SetValue(instance.Name + ".Texture Left", 0);
                                parentElement.DefaultState.SetValue(instance.Name + ".Texture Width", texture.Width);
                                parentElement.DefaultState.SetValue(instance.Name + ".Texture Height", texture.Height);
                            }
                        }
                    }
                }
                if (textureAddress == TextureAddress.DimensionsBased)
                {
                    // if the values are 0, then we should set them to 1:
                    float widthScale = rvf.GetValue<float>("Texture Width Scale");
                    float heightScale = rvf.GetValue<float>("Texture Height Scale");

                    if (widthScale == 0)
                    {
                        if (instance != null)
                        {
                            SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".Texture Width Scale", 1.0f);
                        }
                        else
                        {
                            SelectedState.Self.SelectedStateSave.SetValue("Texture Width Scale", 1.0f);
                        }
                    }

                    if (heightScale == 0)
                    {
                        if (instance != null)
                        {
                            SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".Texture Height Scale", 1.0f);
                        }
                        else
                        {
                            SelectedState.Self.SelectedStateSave.SetValue("Texture Height Scale", 1.0f);
                        }
                    }

                }
            }
        }
    }
}