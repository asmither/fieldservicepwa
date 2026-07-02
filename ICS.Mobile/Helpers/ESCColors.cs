using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using System.Text.Json;

namespace ICS.Mobile.Helpers;

public class ESCColors
{
    // convert ESC integer colors into HEX
    public string Int2RGBHex(long n)
    {
        long r = (n >> 0) & 0xff;
        long g = (n >> 8) & 0xff;
        long b = (n >> 16) & 0xff;
        return (string.Concat("#", ToHex(r), ToHex(g), ToHex(b)));
    }

    private string ToHex(long value)
    {
        return value.ToString("X").PadLeft(2, '0');
    }

    public string InvertHexColor(string hexcolor, bool bwonly = true)
    {
        // your guess is as good as mine, here.  Have at it!
        string invColor; // retval

        System.Drawing.Color fromColor = System.Drawing.ColorTranslator.FromHtml(hexcolor);
        System.Drawing.Color invertedColor = System.Drawing.Color.FromArgb(fromColor.ToArgb() ^ 0xffffff);
        if (bwonly)
        {
            if ((invertedColor.R * invertedColor.G * 0.587 + invertedColor.B * 0.114) > 186)
            { invColor = "#ffffff"; }
            else { invColor = "#00000"; }
            return invColor;
        }
        if (invertedColor.R > 110 && invertedColor.R < 150 &&
            invertedColor.G > 110 && invertedColor.G < 150 &&
            invertedColor.B > 110 && invertedColor.B < 150)
        {
            int avg = (invertedColor.R + invertedColor.G + invertedColor.B) / 3;
            avg = avg > 128 ? 200 : 60;
            invertedColor = System.Drawing.Color.FromArgb(avg, avg, avg);

        }
        invColor = string.Concat("#", ToHex(invertedColor.R), ToHex(invertedColor.G), ToHex(invertedColor.B));
        return (invColor);

    }




}
