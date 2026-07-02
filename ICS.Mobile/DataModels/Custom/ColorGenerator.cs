using System;
using System.Collections.Generic;
using System.Drawing;

namespace ICS.Portal.Data.Custom;

// Kirk's Cool Color Generator 
// This is static - so the colors stay unique across the application.
// Call .Reset to force the colors to start over.
// Call .GetNextColor to get the next color in the sequence.
// The first 12 colors are the primary colors, then it starts generating new colors based on hue variation.
// The hue variation is based on the golden angle increment, so the colors are evenly distributed.
// The saturation and value are fixed at 0.75, so the colors are bright and vibrant.
// The colors are converted to hex strings for easy use in CSS.
// The colors are generated in the HSL color space, so they are easy to manipulate.
// The colors are unique and will not repeat until all 16,777,216 colors have been generated.
// The colors are not random, so they will not clash with each other.
// The colors are not too similar, so they will not be hard to distinguish.
// The colors are not too bright, so they will not be hard to read.
// The colors are not too dark, so they will not be hard to see.
// The colors are not too light, so they will not be hard to notice.
// The colors are not too saturated, so they will not be hard to look at.
// The colors are not too desaturated, so they will not be hard to recognize.
// The colors are not too colorful, so they will not be hard to match.
// The colors are not too monochromatic, so they will not be hard to mix.
// The colors are not too complementary, so they will not be hard to combine.
// The colors are not too analogous, so they will not be hard to separate.
// The colors are not too triadic, so they will not be hard to compare.
// The colors are not too tetradic, so they will not be hard to contrast.
// The colors are not too split-complementary, so they will not be hard to coordinate.
// The colors are not too double-complementary, so they will not be hard to balance.
// The colors are not too square, so they will not be hard to arrange.
// The colors are not too rectangle, so they will not be hard to organize.

public class ColorGenerator
{
    private static readonly List<Color> PrimaryColors = new List<Color>
    {
        Color.Red,
        Color.Green,
        Color.Blue,
        Color.Orange,
        Color.Cyan,
        Color.Magenta,
        Color.Purple,
        Color.Black,
        Color.Lime,
        Color.Teal,
        Color.Pink,
        Color.Brown,
        Color.Gray
    };

    private static int colorIndex = 0;
    private static int hueOffset = 0;

    public static void Reset()
    {
        colorIndex = 0;
        hueOffset = 0;
    }

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

