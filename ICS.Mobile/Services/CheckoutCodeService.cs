namespace ICS.Mobile.Services;

public static class CheckoutCodeService
{
    private const int Scrambler6 = 834527;
    private const long Scrambler8 = 83452789L;
    private const int CheckMult = 7;

    #region Generate

    public static string GenerateCode(int workflowStepId, int issuingEmployeeId)
    {
        if (issuingEmployeeId < 0)
            throw new ArgumentOutOfRangeException(nameof(issuingEmployeeId));

        return issuingEmployeeId <= 9999
            ? GenerateCode6(workflowStepId, issuingEmployeeId)
            : GenerateCode8(workflowStepId, issuingEmployeeId);
    }

    private static string GenerateCode6(int workflowStepId, int employeeId)
    {
        int key = GetLastFourDigits(workflowStepId);  // ✓
        int check = (key * 3 + employeeId * CheckMult) % 100;
        int raw = employeeId * 100 + check;
        long scrambled = ((long)raw + (long)key * Scrambler6) % 1000000;
        return scrambled.ToString("D6");
    }

    private static string GenerateCode8(int workflowStepId, int employeeId)
    {
        if (employeeId > 999999)
            throw new ArgumentOutOfRangeException(nameof(employeeId), "Must be 0-999999");

        int key = GetLastFourDigits(workflowStepId);  // ✓
        int check = (key * 3 + employeeId * CheckMult) % 100;
        long raw = (long)employeeId * 100 + check;
        long scrambled = (raw + (long)key * Scrambler8) % 100000000L;
        return scrambled.ToString("D8");
    }

    #endregion

    #region Validate

    public static int? ValidateCode(string code, int workflowStepId)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        code = code.Trim().Replace("-", "").Replace(" ", "");

        return code.Length switch
        {
            6 => ValidateCode6(code, workflowStepId),
            8 => ValidateCode8(code, workflowStepId),
            _ => null
        };
    }

    private static int? ValidateCode6(string code, int workflowStepId)
    {
        if (!int.TryParse(code, out int codeNum))
            return null;

        int key = GetLastFourDigits(workflowStepId);  // ✓ FIXED
        long raw = ((long)codeNum - (long)key * Scrambler6) % 1000000;
        if (raw < 0) raw += 1000000;

        int employeeId = (int)(raw / 100);
        int check = (int)(raw % 100);
        int expectedCheck = (key * 3 + employeeId * CheckMult) % 100;

        if (check != expectedCheck || employeeId > 9999)
            return null;

        return employeeId;
    }

    private static int? ValidateCode8(string code, int workflowStepId)
    {
        if (!long.TryParse(code, out long codeNum))
            return null;

        int key = GetLastFourDigits(workflowStepId);  // ✓ FIXED
        long raw = (codeNum - (long)key * Scrambler8) % 100000000L;
        if (raw < 0) raw += 100000000L;

        int employeeId = (int)(raw / 100);
        int check = (int)(raw % 100);
        int expectedCheck = (key * 3 + employeeId * CheckMult) % 100;

        if (check != expectedCheck || employeeId > 999999)
            return null;

        return employeeId;
    }

    #endregion

    #region Truncate number functions
    public static int GetLastFourDigits(int workflowStepId) => Math.Abs(workflowStepId % 10000);
    public static string GetLast4DigitsPadded(int number)
    {
        // Convert number to string, pad with zeros if needed, and take last 4 digits
        var numStr = number.ToString().PadLeft(4, '0');
        return numStr.Length > 4 ? numStr.Substring(numStr.Length - 4) : numStr;
    }
    #endregion


}