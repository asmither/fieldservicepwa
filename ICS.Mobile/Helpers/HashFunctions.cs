using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using System.Text.Json;

namespace ICS.Mobile.Helpers;

public class SQLHashFunctions
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

        return SQLHashFunctions.SqlBinaryChecksum(SQLHashFunctions.HashBytes(str));
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

