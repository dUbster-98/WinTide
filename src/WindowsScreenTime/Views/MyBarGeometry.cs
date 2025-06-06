﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Drawing;
using SkiaSharp;
using static WindowsScreenTime.ViewModels.HomeViewModel;

namespace WindowsScreenTime.Views
{
    class MyBarGeometry : BoundedDrawnGeometry, IDrawnElement<SkiaSharpDrawingContext>
    {
        public string? IconPath { get; set; }
        private Dictionary<string, SKBitmap> iconCache = [];

        public void UpdateData(ProcessChartInfo data)
        {
            IconPath = data.IconPath;
        }

        public void Draw(SkiaSharpDrawingContext context)
        {
            var canvas = context.Canvas;
            var paint = context.ActiveSkiaPaint;
                        
            // 막대 그리기
            if (paint != null)
            {
                canvas.DrawRoundRect(X, Y, Width, Height, 15, 15, paint);
            }

            if (!string.IsNullOrEmpty(IconPath))
            {
                SKBitmap bitmap;
                if (!iconCache.TryGetValue(IconPath, out bitmap))
                {
                    bitmap = SKBitmap.Decode(IconPath);
                    if (bitmap != null)
                    {
                        iconCache[IconPath] = bitmap;
                    }
                }

                if (bitmap != null)
                {
                    using var image = SKImage.FromBitmap(bitmap);
                    using var paintImage = new SKPaint
                    {
                        IsAntialias = true
                    };
                    float iconWidth = 32;
                    float iconHeight = 32;

                    float iconX = X + Width + 5;
                    float iconY = Y + (Height - iconHeight) / 2; // 막대 상단 위에 배치
                    
                    var destRect = new SKRect(iconX, iconY, iconX + iconWidth, iconY + iconHeight);
                    canvas.DrawImage(image, destRect, paintImage);
                }
            }
        }

        public void Dispose()
        {
            foreach (var bitmap in iconCache.Values)
            {
                bitmap?.Dispose();
            }
            iconCache.Clear();
        }
    }
}
