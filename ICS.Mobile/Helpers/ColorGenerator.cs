using System;
using System.Collections.Generic;
using System.Drawing;

namespace ICS.Mobile.Helpers;

//
// Usage for getting unique colors (hex) for use on maps or whatnot
//
//List<string> colorList = new List<string>();
//for (int i = 0; i < 20; i++)
//{
//    string color = ColorGenerator.GetNextColor();
//colorList.Add(color);
//    Console.WriteLine(color);
//}


public class ColorGenerator
{
    private static readonly List<Color> PrimaryColors = new List<Color>
    {
        Color.Red,
        Color.Green,
        Color.Blue,
        Color.Yellow,
        Color.Cyan,
        Color.Magenta,
        Color.Orange,
        Color.Purple,
        Color.Lime,
        Color.Teal,
        Color.Pink,
        Color.Brown,
        Color.Gray
    };

    private static int colorIndex = 0;
    private static int hueOffset = 0;

    public static string GetNextColor()
    {
        Color color;
        if (colorIndex < PrimaryColors.Count)
        {
            color = PrimaryColors[colorIndex];
            colorIndex++;
        }
        else
        {
            // Generate new colors based on hue variation
            float hue = (colorIndex * 137.5077f + hueOffset) % 360; // Using golden angle increment
            color = ColorFromHSV(hue, 0.75, 0.75);
            colorIndex++;
        }

        return ColorToHex(color);
    }

    private static Color ColorFromHSV(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value = value * 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        if (hi == 0)
            return Color.FromArgb(255, v, t, p);
        else if (hi == 1)
            return Color.FromArgb(255, q, v, p);
        else if (hi == 2)
            return Color.FromArgb(255, p, v, t);
        else if (hi == 3)
            return Color.FromArgb(255, p, q, v);
        else if (hi == 4)
            return Color.FromArgb(255, t, p, v);
        else
            return Color.FromArgb(255, v, p, q);
    }

    private static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}

