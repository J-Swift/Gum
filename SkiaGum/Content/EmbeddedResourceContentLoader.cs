﻿using RenderingLibrary.Content;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using SKSvg = SkiaSharp.Extended.Svg.SKSvg;

namespace SkiaGum.Content
{
    public class EmbeddedResourceContentLoader : IContentLoader
    {
        public Func<string, string> AdjustContentName;
        public void AddDisposable(string contentName, IDisposable disposable)
        {
            throw new NotImplementedException();
        }

        public T LoadContent<T>(string contentName)
        {
            if (typeof(T) == typeof(SKTypeface))
            {
                return (T)(object)LoadSKTypeface(contentName);
            }
            else if (typeof(T) == typeof(SKSvg))
            {
                return (T)(object)LoadSKSvg(contentName);
            }
            else if (typeof(T) == typeof(SKBitmap))
            {
                return (T)(object)LoadSKBitmap(contentName);
            }
            else

            {
                return default(T);
            }
        }

        private SKSvg LoadSKSvg(string contentName)
        {
            var modifiedContentName = AdjustContentName?.Invoke(contentName) ?? contentName;

            return SkiaResourceManager.GetSvg(modifiedContentName);
        }

        private SKBitmap LoadSKBitmap(string contentName)
        {
            var modifiedContentName = AdjustContentName?.Invoke(contentName) ?? contentName;

            return SkiaResourceManager.GetSKBitmap(modifiedContentName);
        }

        private SKTypeface LoadSKTypeface(string contentName)
        {
            var modifiedContentName = AdjustContentName?.Invoke(contentName) ?? contentName;

            return SkiaResourceManager.GetTypeface(modifiedContentName);
        }

        public T TryGetCachedDisposable<T>(string contentName)
        {
            throw new NotImplementedException();
        }

        public T TryLoadContent<T>(string contentName)
        {
            throw new NotImplementedException();
        }
    }
}
