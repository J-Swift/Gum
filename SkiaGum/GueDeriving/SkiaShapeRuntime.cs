﻿using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public abstract class SkiaShapeRuntime : BindableGraphicalUiElement
    {
        protected abstract RenderableBase ContainedRenderable { get; }

        #region Solid colors

        public int Alpha
        {
            get => ContainedRenderable.Alpha;
            set => ContainedRenderable.Alpha = value;
        }

        public int Blue
        {
            get => ContainedRenderable.Blue;
            set => ContainedRenderable.Blue = value;
        }

        public int Green
        {
            get => ContainedRenderable.Green;
            set => ContainedRenderable.Green = value;
        }

        public int Red
        {
            get => ContainedRenderable.Red;
            set => ContainedRenderable.Red = value;
        }

        public SKColor Color
        {
            get => ContainedRenderable.Color;
            set => ContainedRenderable.Color = value;
        }
        #endregion

        #region Filled/Stroke

        public bool IsFilled
        {
            get => ContainedRenderable.IsFilled;
            set => ContainedRenderable.IsFilled = value;
        }

        public float StrokeWidth
        {
            get => ContainedRenderable.StrokeWidth;
            set => ContainedRenderable.StrokeWidth = value;
        }

        #endregion

        #region Dropshadow

        public int DropshadowAlpha
        {
            get => ContainedRenderable.DropshadowAlpha;
            set => ContainedRenderable.DropshadowAlpha = value;
        }

        public int DropshadowBlue
        {
            get => ContainedRenderable.DropshadowBlue;
            set => ContainedRenderable.DropshadowBlue = value;
        }

        public int DropshadowGreen
        {
            get => ContainedRenderable.DropshadowGreen;
            set => ContainedRenderable.DropshadowGreen = value;
        }

        public int DropshadowRed
        {
            get => ContainedRenderable.DropshadowRed;
            set => ContainedRenderable.DropshadowRed = value;
        }


        public bool HasDropshadow
        {
            get => ContainedRenderable.HasDropshadow;
            set => ContainedRenderable.HasDropshadow = value;
        }

        public float DropshadowOffsetX
        {
            get => ContainedRenderable.DropshadowOffsetX;
            set => ContainedRenderable.DropshadowOffsetX = value;
        }
        public float DropshadowOffsetY
        {
            get => ContainedRenderable.DropshadowOffsetY;
            set => ContainedRenderable.DropshadowOffsetY = value;
        }

        public float DropshadowBlurX
        {
            get => ContainedRenderable.DropshadowBlurX;
            set => ContainedRenderable.DropshadowBlurX = value;
        }
        public float DropshadowBlurY
        {
            get => ContainedRenderable.DropshadowBlurY;
            set => ContainedRenderable.DropshadowBlurY = value;
        }

        #endregion
    }
}
