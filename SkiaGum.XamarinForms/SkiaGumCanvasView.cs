﻿using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaGum.Managers;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topten.RichTextKit;
using Microsoft.Xna.Framework;


namespace SkiaGum
{
    public class SkiaGumCanvasView : SKCanvasView, ISystemManagers
    {
        #region Fields/Properties

        private ObservableCollection<BindableGraphicalUiElement> GumElementsInternal { get; set; } = new ObservableCollection<BindableGraphicalUiElement>();

        public IReadOnlyCollection<BindableGraphicalUiElement> GumElements => GumElementsInternal;

        // this is public to support adding GUE's directly in gencode.
        public ObservableCollection<BindableGraphicalUiElement> Children => GumElementsInternal;

        SystemManagers SystemManagers;

        public SemaphoreSlim ExclusiveUiInteractionSemaphor = new SemaphoreSlim(1, 1);

        float yPushed;
        bool isWithinThreshold = false;

        Func<Task> customPushEventToRaise;
        Func<Task> customReleaseEventToRaise;

        Func<float, float, Task> customTouchEvent;

        BindableGraphicalUiElement elementPushed;

        /// <summary>
        /// The scale used when rendering the visuals. This is usually the device density.
        /// Leaving this at 1 will make everything draw to-the-pixel regardles of device density.
        /// </summary>
        public static float GlobalScale { get; set; } = 1;





        #endregion

        public SkiaGumCanvasView()
        {
            GumElementsInternal.CollectionChanged += HandleCollectionChanged;

            SystemManagers = new SystemManagers();
            SystemManagers.Initialize();

            base.Touch += HandleTouch;
        }

        #region Touch-related Logic

        protected virtual async void HandleTouch(object sender, SKTouchEventArgs args)
        {
            // Maybe we need to adjust this for other devices?
            float threshold = (float)20;

            float touchX = args.Location.X / GlobalScale;
            float touchY = args.Location.Y / GlobalScale;



            // SkiaSharp views return
            // whether they handle touches
            // through args.Handled. If the 
            // value is false, then control passes
            // from this view to underlying views. Once
            // it is passed, it does not return to this view
            // until a new touch is initiated.
            switch (args.ActionType)
            {
                case SKTouchAction.Pressed:
                    yPushed = touchY;

                    isWithinThreshold = true;

                    if (customPushEventToRaise != null)
                    {
                        var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                        if (canProceed)
                        {
                            try
                            {
                                await customPushEventToRaise();
                            }
                            finally
                            {
                                ExclusiveUiInteractionSemaphor.Release(1);
                            }
                        }
                    }
                    else
                    {
                        var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                        if (canProceed)
                        {
                            elementPushed = FindElement(touchX, touchY, GumElementsInternal, item => item.ClickedAsync != null || item.DragAsync != null);
                            if (elementPushed != null)
                            {
                                DarkenElement(elementPushed);
                            }
                            ExclusiveUiInteractionSemaphor.Release(1);
                        }

                        await TryPushOnContainedGumObjects(touchX, touchY);
                    }

                    args.Handled = true;
                    break;
                case SKTouchAction.Moved:
                    if (isWithinThreshold)
                    {
                        if (System.Math.Abs(touchY - yPushed) > threshold && elementPushed?.DragAsync == null)
                        {
                            isWithinThreshold = false;
                            var whatToLighten = elementPushed;
                            if (whatToLighten != null)
                            {
                                LightenElement(whatToLighten);
                            }
                        }

                        if (isWithinThreshold)
                        {
                            if (elementPushed?.DragAsync != null)
                            {
                                await elementPushed.DragAsync(touchX, touchY);
                            }
                            if (elementPushed?.DragOff != null && elementPushed.IsPointInside(touchX, touchY) == false)
                            {
                                await elementPushed.DragOff();
                            }
                        }
                    }

                    args.Handled = isWithinThreshold;
                    break;
                case SKTouchAction.Released:
                    {
                        if (customReleaseEventToRaise != null)
                        {
                            var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                            if (canProceed)
                            {
                                try
                                {
                                    await customReleaseEventToRaise();
                                }
                                finally
                                {
                                    ExclusiveUiInteractionSemaphor.Release(1);
                                }
                            }
                        }
                        else
                        {
                            var whatToLighten = elementPushed;
                            if (whatToLighten != null)
                            {
                                LightenElement(whatToLighten);
                            }
                            if (customTouchEvent != null)
                            {
                                var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                                if (canProceed)
                                {
                                    try
                                    {
                                        await customTouchEvent(touchX, touchY);
                                    }
                                    finally
                                    {
                                        ExclusiveUiInteractionSemaphor.Release(1);
                                    }
                                }
                            }

                            await TryClickOnContainedGumObjects(touchX, touchY);
                        }
                    }


                    break;
            }
        }

        private void LightenElement(GraphicalUiElement whatToLighten)
        {
            if (whatToLighten is ColoredCircleRuntime circleRuntime)
            {
                circleRuntime.IsDimmed = false;
                InvalidateSurface();
            }
        }

        private void DarkenElement(BindableGraphicalUiElement elementPushed)
        {
            if (elementPushed is ColoredCircleRuntime circleRuntime)
            {
                circleRuntime.IsDimmed = true;
                InvalidateSurface();
            }
        }

        BindableGraphicalUiElement itemPushed;
        private async Task TryPushOnContainedGumObjects(float x, float y)
        {
            var clickableElement = FindElement(x, y, GumElementsInternal, item => item.PushedAsync != null);

            if (clickableElement != null)
            {
                var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                if (canProceed)
                {
                    try
                    {
                        itemPushed = clickableElement;
                        await clickableElement.PushedAsync(x, y);
                    }
                    finally
                    {
                        ExclusiveUiInteractionSemaphor.Release(1);
                    }
                }
            }
        }

        private async Task TryClickOnContainedGumObjects(float x, float y)
        {
            var clickableElement = FindElement(x, y, GumElementsInternal, item => item.ClickedAsync != null);

            if (clickableElement != null)
            {
                var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                if (canProceed)
                {
                    try
                    {
                        await clickableElement.ClickedAsync();
                    }
                    finally
                    {
                        ExclusiveUiInteractionSemaphor.Release(1);
                    }
                }
            }
        }

        public void SetPushEvent(Func<Task> eventToRaise)
        {
            customPushEventToRaise = eventToRaise;
            EnableTouchEvents = true;
        }

        public void SetReleaseEvent(Func<Task> eventToRaise)
        {
            customReleaseEventToRaise = eventToRaise;
            EnableTouchEvents = true;
        }

        public void SetTouchEvent(Func<float, float, Task> eventHandlingXY)
        {
            customTouchEvent = eventHandlingXY;
            EnableTouchEvents = true;
        }

        public async Task RaiseClickEvent()
        {
            if (customPushEventToRaise != null || customReleaseEventToRaise != null)
            {
                var canProceed = await ExclusiveUiInteractionSemaphor.WaitAsync(0);

                if (canProceed)
                {
                    try
                    {
                        if (customPushEventToRaise != null)
                        {
                            await customPushEventToRaise();
                        }
                        else if (customReleaseEventToRaise != null)
                        {
                            await customReleaseEventToRaise();
                        }
                    }
                    finally
                    {
                        ExclusiveUiInteractionSemaphor.Release(1);
                    }
                }
            }
        }

        private BindableGraphicalUiElement FindElement(float x, float y, IList<BindableGraphicalUiElement> list, Func<BindableGraphicalUiElement, bool> condition)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var gumElement = list[i];

                // Children may sit outside of a container, so we should not restrict children checking on visibility bounds.
                // Yea this makes it slower but it's important for some clicks
                //if (gumElement.Visible && gumElement.IsPointInside(x, y))
                if (gumElement.Visible)
                {
                    if ((condition == null || condition(gumElement)) && gumElement.IsPointInside(x, y))
                    {
                        return gumElement;
                    }
                    else
                    {
                        var children = gumElement.Children.Select(item => item as BindableGraphicalUiElement).Where(item => item != null).ToList();

                        var foundElement = FindElement(x, y, children, condition);

                        if (foundElement != null)
                        {
                            return foundElement;
                        }
                    }
                }
            }
            return null;
        }

        public void SimulateSkTouchAction(SKTouchAction action, float x, float y)
        {
            HandleTouch(this, new SKTouchEventArgs(0, action, new SKPoint(x, y), inContact: true));
        }

        #endregion

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var toAdd in e.NewItems)
                    {
                        var bindableGue = toAdd as BindableGraphicalUiElement;

                        bindableGue.AddToManagers(this);
                        bindableGue.BindingContext = this.BindingContext;
                    }

                    break;
            }
        }

        public void Add(BindableGraphicalUiElement toAdd)
        {
            GumElementsInternal.Add(toAdd);

            if (toAdd.ClickedAsync != null || toAdd.PushedAsync != null || toAdd.DragAsync != null)
            {
                this.EnableTouchEvents = true;
            }
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            foreach (var element in GumElementsInternal)
            {
                if (element is BindableGraphicalUiElement bindableGue)
                {
                    if (!string.IsNullOrEmpty(element.BindingContextBinding))
                    {
                        if (BindingContext != null)
                        {
                            var vmProperty = BindingContext.GetType().GetProperty(element.BindingContextBinding);
                            var value = vmProperty.GetValue(BindingContext);

                            bindableGue.BindingContext = value;

                        }

                    }
                    else
                    {
                        bindableGue.BindingContext = this.BindingContext;
                    }
                }
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs args)
        {
            var canvas = args.Surface.Canvas;
            SKImageInfo info = args.Info;

            SystemManagers.Canvas = canvas;

            GraphicalUiElement.CanvasWidth = info.Width / GlobalScale;
            GraphicalUiElement.CanvasHeight = info.Height / GlobalScale;
            SystemManagers.Renderer.Camera.Zoom = GlobalScale;

            SystemManagers.Renderer.Draw(this.GumElementsInternal, SystemManagers);

            base.OnPaintSurface(args);
        }

        public GraphicalUiElement GetViewAt(float x, float y)
        {
            var found = Children.FirstOrDefault(item =>
            {
                if (item.Visible)
                {
                    return item.IsPointInside(x, y);
                }
                else
                {
                    return false;
                }
            });

            return found;
        }

        public void ForceGumLayout()
        {
            var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended;
            GraphicalUiElement.IsAllLayoutSuspended = false;
            foreach (var item in this.GumElementsInternal)
            {
                item.UpdateLayout();
            }
            GraphicalUiElement.IsAllLayoutSuspended = wasSuspended;
        }

        public Vector2 GetBottomRightMostElementCorner()
        {
            Vector2 bottomRight = Vector2.Zero;

            foreach (var item in this.Children)
            {
                GetBottomRightMostRecursive(item, ref bottomRight);
            }
            return bottomRight;
        }

        private void GetBottomRightMostRecursive(BindableGraphicalUiElement gue, ref Vector2 bottomRight)
        {
            var right = gue.GetAbsoluteRight();
            var bottom = gue.GetAbsoluteBottom();

            bottomRight.X = Math.Max(right, bottomRight.X);
            bottomRight.Y = Math.Max(bottom, bottomRight.Y);

            if (gue.Children == null)
            {
                foreach (BindableGraphicalUiElement item in gue.ContainedElements)
                {
                    GetBottomRightMostRecursive(item, ref bottomRight);
                }
            }
            else
            {
                foreach (BindableGraphicalUiElement item in gue.Children)
                {
                    GetBottomRightMostRecursive(item, ref bottomRight);
                }
            }
        }

        // This currently assumes a height request for when it's added to a StackLayout. Maybe at some point in the
        // future we'd want to do the same thing for canvases added to AbsoluteLayouts?
        public void SetHeightRequestToContents()
        {
            ForceGumLayout();
            var requiredSize = GetBottomRightMostElementCorner();
            HeightRequest = requiredSize.Y;
        }

    }
}
