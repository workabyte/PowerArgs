﻿using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    public abstract class Container : ConsoleControl
    {
        internal Container() { }
        public abstract IEnumerable<ConsoleControl> Children { get; }

        public IEnumerable<ConsoleControl> Descendents
        {
            get
            {
                List<ConsoleControl> descendends = new List<ConsoleControl>();
                VisitControlTree((d) =>
                {
                    descendends.Add(d);
                    return false;
                });

                return descendends.AsReadOnly();
            }
        }

        /// <summary>
        /// Visits every control in the control tree, recursively, using the visit action provided
        /// </summary>
        /// <param name="visitAction">the visitor function that will be run for each child control, the function can return true if it wants to stop further visitation</param>
        /// <param name="root">set to null, used for recursion</param>
        /// <returns>true if the visitation was short ciruited by a visitor, false otherwise</returns>
        public bool VisitControlTree(Func<ConsoleControl, bool> visitAction, Container root = null)
        {
            bool shortCircuit = false;
            root = root ?? this;

            foreach (var child in root.Children)
            {
                shortCircuit = visitAction(child);
                if (shortCircuit) return true;

                if (child is Container)
                {
                    shortCircuit = VisitControlTree(visitAction, child as Container);
                    if (shortCircuit) return true;
                }
            }

            return false;
        }

        protected void Compose(ConsoleControl control)
        {
            if (control.IsVisible == false) return;
            control.Paint();

            foreach(var filter in control.RenderFilters)
            {
                filter.Control = control;
                filter.Filter(control.Bitmap);
            }

            if (control.CompositionMode == CompositionMode.PaintOver)
            {
                ComposePaintOver(control);
            }
            else if(control.CompositionMode == CompositionMode.BlendBackground)
            {
                ComposeBlendBackground(control);
            }
            else 
            {
                ComposeBlendVisible(control);
            }

        }

        private void ComposePaintOver(ConsoleControl control)
        {
            var maxX = control.X + control.Width;
            var maxY = control.Y + control.Height;
            for (var x = control.X; x < maxX; x++)
            {
                for (var y = control.Y; y < maxY; y++)
                {

                    var controlPixel = control.Bitmap.GetPixel(x - control.X, y - control.Y).Value;
                    Bitmap.DrawPoint(controlPixel.Value, x, y);
                }
            }
        }

        private void ComposeBlendBackground (ConsoleControl control)
        {
            var maxX = control.X + control.Width;
            var maxY = control.Y + control.Height;
            for (var x = control.X; x < maxX; x++)
            {
                for (var y = control.Y; y < maxY; y++)
                {
                    if (Bitmap.IsInBounds(x, y) == false)
                    {
                        continue;
                    }
                    var controlPixel = control.Bitmap.GetPixel(x - control.X, y - control.Y).Value;

                    if (controlPixel?.BackgroundColor != ConsoleString.DefaultBackgroundColor)
                    {
                        Bitmap.DrawPoint(controlPixel.Value, x, y);
                    }
                    else
                    {
                        var myPixel = Bitmap.GetPixel(x, y).Value;
                        if (myPixel.HasValue && myPixel.Value.BackgroundColor != ConsoleString.DefaultBackgroundColor)
                        {
                            var composedValue = new ConsoleCharacter(controlPixel.Value.Value, controlPixel.Value.ForegroundColor, myPixel.Value.BackgroundColor);
                            Bitmap.DrawPoint(composedValue, x, y);
                        }
                        else
                        {
                            Bitmap.DrawPoint(controlPixel.Value, x, y);
                        }
                    }
                }
            }
        }

        private bool IsVisibleOnMyPanel(ConsolePixel pixel)
        {
            if (pixel.Value.HasValue == false) return false;

            var c = pixel.Value.Value;

            if(c.Value == ' ')
            {
                return c.BackgroundColor != Background;
            }
            else
            {
                return c.ForegroundColor != Background || c.BackgroundColor != Background;
            }
        }

        private void ComposeBlendVisible(ConsoleControl control)
        {
            var maxX = control.X + control.Width;
            var maxY = control.Y + control.Height;
            for (var x = control.X; x < maxX; x++)
            {
                for (var y = control.Y; y < maxY; y++)
                {
                    if (Bitmap.IsInBounds(x, y) == false)
                    {
                        continue;
                    }
                    var controlPixel = control.Bitmap.GetPixel(x - control.X, y - control.Y);

                    var controlPixelHasRenderableContent = IsVisibleOnMyPanel(controlPixel);

 
                    if (controlPixelHasRenderableContent)
                    {
                        Bitmap.DrawPoint(controlPixel.Value.Value, x, y);
                    }
                }
            }
        }
    }
}
