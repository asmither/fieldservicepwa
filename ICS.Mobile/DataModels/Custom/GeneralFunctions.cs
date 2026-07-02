using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICS.Mobile.Helpers;
using Newtonsoft.Json;

namespace ICS.Portal.Data.Custom;

public class HashFunctions
{
    //
    //
    // C#:   INT x = GetDeterministicHashCodeSQL("Kirk");     
    //         ... matches INT return value of: ...
    // SQL:  SELECT @x = BINARY_CHECKSUM(CONVERT(VARCHAR(128),HashBytes('sha1', 'Kirk'),2));
    //
    // in C#:  x = SQLHashFunctions.GetDeterministicHashCodeSQL("Kirk");
    //
    // Both functions are identical to SQL Server's output 

    public static int GetDeterministicHashCodeSQL(string str)
    {
        // THIS IS THE BAD-BOY!  Pass any string, get an INT has back!
        // 
        // HashBytes handles unlimited string lengh and returns a 40 character hex string
        // Binary_Checksum has a 255 char limit on the string.  It returns a 32 bit int.
        //
        // SQL Server compatible functions - Binary_CheckSum of a HashBytes(SHA2, string)
        //return Binary_Checksum(HashBytes(str));

        //int x = 0;
        //x = GetDeterministicHashCodeSQL($"{(44)}{(2)}{(15)}{(0)}"); Console.WriteLine(x);
        // SQL: SELECT @x = BINARY_CHECKSUM(CONVERT(VARCHAR(128), HashBytes('sha1', CONCAT(44,2,15,0)), 2));

        return SqlBinaryChecksum(HashBytes(str));
    }

    #region SQL Server Compatible Hash Functions
    private static int SqlBinaryChecksum(string text)
    {
        uint accumulator = 0;
        for (int i = 0; i < text.Length; i++)
        {
            var leftRotate4bit = (accumulator << 4) | (accumulator >> -4);
            accumulator = leftRotate4bit ^ text[i];
        }
        return (int)accumulator;
    }

    private static int Binary_Checksum(string text)
    {
        return (SqlBinaryChecksum(text));

        //long sum = 0;
        //byte overflow;
        //for (int i = 0; i < text.Length; i++)
        //{
        //    sum = (long)((16 * sum) ^ System.Convert.ToUInt32(text[i]));
        //    overflow = (byte)(sum / 4294967296);
        //    sum = sum - overflow * 4294967296;
        //    sum = sum ^ overflow;
        //}

        //if (sum > 2147483647)
        //    sum = sum - 4294967296;
        //else if (sum >= 32768 && sum <= 65535)
        //    sum = sum - 65536;
        //else if (sum >= 128 && sum <= 255)
        //    sum = sum - 256;

        //return (int)sum;
    }

    private static string HashBytes(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        var sha1 = System.Security.Cryptography.SHA1.Create();
        byte[] hashBytes = sha1.ComputeHash(bytes);

        return HexStringFromBytes(hashBytes);
    }

    private static string HexStringFromBytes(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            var hex = b.ToString("x2");
            sb.Append(hex);
        }
        return sb.ToString().ToUpper();
    }
    #endregion

}

#region Global String Extensions
public static class StringExtensions
{
    public static int HashAsInt(this string StrVal)
    {
        return SQLHashFunctions.GetDeterministicHashCodeSQL(StrVal);
    }
    public static long HashAsLong(this string StrVal)
    {
        return SQLHashFunctions.GetDeterministicHashCodeSQL(StrVal);
    }

    // Because i miss VB...
    public static string Right(this string str, int length)
    {
        if (str == null) return null;
        if (string.IsNullOrEmpty(str)) return "";
        if (str.Length <= length) return str;
        return str.Substring(str.Length - length, length);
    }
    public static string Left(this string str, int length)
    {
        if (str == null) return null;
        if (string.IsNullOrEmpty(str)) return "";
        if (str.Length <= length) return str;
        return str[..length];
    }
    public static string TitleCase(this string StrVal)
    {
        StrVal ??= "";
        string retval = StrVal;
        if (!string.IsNullOrEmpty(StrVal))
        {
            // titlecase it to be nice for our data
            TextInfo addrTI = new CultureInfo("en-US", false).TextInfo;
            retval = addrTI.ToTitleCase(StrVal.ToLower());
        }
        return retval;
    }

    public static string GetInitials(string input) =>
        string.IsNullOrWhiteSpace(input)
        ? string.Empty
        : string.Concat(input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(word => word[0]));
}
#endregion

public class GeneralFunctions
{

    #region String Utility Functions

    public static string ObjectToJSON(object o, bool writeIndented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented
        };
        return System.Text.Json.JsonSerializer.Serialize(o, options);
    }

    public static string NSObjectToJSON(object o)
    {
        string SerilizedText = "";
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(o);
        if (!string.IsNullOrEmpty(json)) SerilizedText = Newtonsoft.Json.Linq.JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.Indented);
        json = "";
        return SerilizedText;
    }

    public static bool IsJsonValid(string json)
    {
        try
        {
            var parsedJson = Newtonsoft.Json.Linq.JToken.Parse(json);
            return true;
        }
        catch (Newtonsoft.Json.JsonReaderException)
        {
            return false;
        }
        catch (Exception)
        {
            // Handle any unexpected errors
            return false;
        }
    }

    public static byte[] GetPdfLogoBytes()
    {
        // 64x64 red PDF icon, transparent background
        const string base64 =
            "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAADEElEQVR4nO2awU4aQQCG/1WEUo0hqJuG" +
            "ixiNB09GY/TiyYt3ozHRgxc5a7x50hdQHgFPJPICnjxIkFI0toeebKxtaNpQrFowS93V6aFl1dqBWRgc" +
            "dp0vIZmZzOz+8+0wy4YFJBLJc0axOuA1QOoRhMYoqf10iqJQ59lk5UBPPXkASNGzc4FZgIjJl6inBCYB" +
            "Iidfol4SKgpohMmXqIcES3tAI8Bbgu0EAHwl2FIAwE+CbQUAfCTYWgBQuwTbCwBqk+AIAUD1EhwjAKhO" +
            "gqMEANYlOE4AYE2CIwUA7BIcKwBgk+BoAUBlCY4XUAkpQHQA0UgBogOIRgoQHUA0UoDoAKKRAkQHEI2r" +
            "mkEds7Poi0YftRPDgJ7NopBM4tvmJvKJBH0MIbi9vsbN5SWuMxlcHR4iF4kgv7/PfL77FFIpvB8bszwX" +
            "ritAcbngDgTgn5rCQDwONRQq01lBk8eDFlVF69AQ1MVFDCQS6ItG0dzWxjNWWbgISHu9SCkK3vb0IL+3" +
            "96dRUdAdDqOlq4s65o3bjXf9/fiyvg6i6wD+Xu1YDCjzFFc63/1PNVcf4LwCfp2e4tPy8t3BvV60T0xQ" +
            "+xNdR/H4GJm1NXyYmzPbfZOT6JiZ4RmNCvdNsHhy8qDeoqpM437EYrg6OjLrnQsLPGNR4S7gRW/vg7qe" +
            "zTKPzcfjZrl1eJjab0TTMErIg8+rpSXLWQHOAjzBILo3Nsz6rabh5+4u8/ib83Oz7PL5eEajUtVt8F9G" +
            "NO1xIyH4vLJiaQU0+/1m2bi4oPZLe724LRatRKTCRUAJYhgwcjkUkkl8DYfv7giMtI+Pm+WrgwOe0ahw" +
            "EcDjivinp/FycNCs57a2akzFBtcVYBXF5YInGETn/DwCq6tm+8XODs62t58kgzAB/903AJxFo/gYCgEc" +
            "3g9kQdwKIARE12GUngXSaXyPRFBIJp80RsW/ThrpLbFqGSszz2f/OCwFiA4gGilAdADRSAGiA4hGChAd" +
            "QDRSQKUO5X5G2oFK+ZlWgF0lsORm/grYTQJrXkt7gF0k2CWnRNIA/AbkeN83ccDRLwAAAABJRU5ErkJg" +
            "gg==";

        return Convert.FromBase64String(base64);
    }

    public static string Truncate(string value, int length)
    {
        if (!string.IsNullOrEmpty(value) && length > 0)
        {
            if (value.Length > length) return value.Substring(0, length);
        }
        return value;
    }

    public static string[] ParseExternalKey(string key, int numkeys)
    {
        // Check if the string contains exactly two commas
        if (key.Split(',').Length == numkeys)
        {
            // Split the string by commas and return the parts
            return key.Split(',');
        }
        else
        {
            // Return null if the format is not as expected
            return null;
        }
    }

    public static string ReverseStringByCRLF(string strNote)
    {
        string retVal = "";
        string[] arr = strNote.Replace("\r\n", "\n").Split("\n".ToCharArray());
        if (arr.Count() - 1 > 0)
        {
            for (int i = arr.Count() - 1; i >= 0; i--)
            {
                retVal = string.Concat(retVal, arr[i], "\r\n");
            }
        }
        // strip leading \r\n's
        while (retVal.StartsWith("\r\n"))
        {
            retVal = retVal.Substring(2);
        }
        return retVal;
    }

    public static string PathSafeString(string s, string replaceChar = "", string emptyDefValue = "")
    {
        if (string.IsNullOrEmpty(replaceChar)) replaceChar = "_";
        foreach (char character in Path.GetInvalidFileNameChars())
        {
            s = s.Replace(character.ToString(), replaceChar);
        }

        foreach (char character in Path.GetInvalidPathChars())
        {
            s = s.Replace(character.ToString(), replaceChar);
        }

        if (string.IsNullOrEmpty(s)) s = emptyDefValue ?? "";
        return s;
    }

    public static string CombinePathAndFilename(string path, string filename)
    {
        if (path.Substring(path.Length - 1, 1) != "\\") path += "\\";
        string combinedPath = path + filename;

        return combinedPath;
    }

    public static string RemoveLeadingZeros(string s, string replaceChar = "", string emptyDefValue = "")
    {
        if (string.IsNullOrEmpty(s)) return emptyDefValue;
        s = s.TrimStart('0');
        if (string.IsNullOrEmpty(s)) s = emptyDefValue;
        return s;
    }

    
    public static int GetMantissaLength(double value)
    {
        string valueString = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int decimalPointIndex = valueString.IndexOf('.');

        if (decimalPointIndex == -1)
        {
            return 0;  // No decimal point means no mantissa
        }

        return valueString.Length - decimalPointIndex - 1;
    }

    public static int GetMantissaLength(decimal value)
    {
        string valueString = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int decimalPointIndex = valueString.IndexOf('.');

        if (decimalPointIndex == -1)
        {
            return 0;  // No decimal point means no mantissa
        }

        return valueString.Length - decimalPointIndex - 1;
    }

    



    public static string FormatNameFirstLast(string fullName)
    {
        if (fullName.Contains(","))
        {
            var parts = fullName.Split(new[] { ',' }, 2);
            if (parts.Length == 2)
            {
                var firstName = parts[1].Trim();
                var lastName = parts[0].Trim();
                return $"{firstName} {lastName}";
            }
        }
        // Return original string if no comma is found or splitting fails
        return fullName;
    }




    public static string FormatPhoneNumber(string? phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";
        Regex regex = new Regex(@"[^\d]");
        phone = regex.Replace(phone, "");
        string format = "###-###-####";
        phone = Convert.ToInt64(phone).ToString(format);
        return phone;
    }

    public static string? StripNumerics(string? StrVal) =>
    string.IsNullOrWhiteSpace(StrVal) ? null : new string(StrVal.Where(c => !char.IsDigit(c)).ToArray());

    public static string? OnlyNumerics(string? StrVal) =>
        string.IsNullOrWhiteSpace(StrVal) ? null : new string(StrVal.Where(char.IsDigit).ToArray());

    public static bool HasNumerics(string? StrVal) =>
        !string.IsNullOrWhiteSpace(StrVal) && StrVal.Any(char.IsDigit);

    public static string TitleCaseIt(string? StrVal)
    {
        StrVal ??= "";
        string retval = StrVal;
        if (!string.IsNullOrEmpty(StrVal))
        {
            // titlecase it to be nice for our data
            TextInfo addrTI = new CultureInfo("en-US", false).TextInfo;
            retval = addrTI.ToTitleCase(StrVal.ToLower());
        }
        return retval;
    }

    public static string PhoneNumberFormat(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        value = System.Text.RegularExpressions.Regex.Replace(value, @"\D", "");

        // Strip leading '1' country code
        if ((value.Length == 11 || value.Length == 12) && value[0] == '1')
            value = value.Substring(1);

        return value.Length switch
        {
            7 => $"{value[..3]}-{value[3..]}",
            10 => $"{value[..3]}-{value[3..6]}-{value[6..]}",
            > 10 => $"{value[..3]}-{value[3..6]}-{value[6..10]} {value[10..]}",
            _ => value
        };
    }


    #endregion

}


