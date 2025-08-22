using System.Text;

namespace Shared.Extensions;

public static class StringUtils
{
    // Method to convert a normal string to a Base64 string
    public static string StringToBase64(this string? normalString)
    {
        if (string.IsNullOrEmpty(normalString)) normalString = string.Empty;

        byte[] bytes = Encoding.UTF8.GetBytes(normalString);
        return Convert.ToBase64String(bytes);
    }

    // Method to convert a Base64 string back to a normal string
    public static string Base64ToString(this string base64String)
    {
        byte[] bytes = Convert.FromBase64String(base64String);
        return Encoding.UTF8.GetString(bytes);
    }

    // Method to convert a Base64 string back to a normal string
    public static string GenerateFixedNumericCode(this string value, int length = 3)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "000";

        // Remove any whitespace and convert to uppercase
        // var processedName = fullName.Replace(" ", "").ToUpper();
        var processedName = value;

        // Calculate a consistent numeric value based on character positions
        int sum = 0;
        for (int i = 0; i < processedName.Length; i++)
        {
            // Use position and character value to create a consistent number
            sum += (processedName[i] - 'A' + 1) * (i + 1);
        }

        // Take last 3 digits using modulo
        int threeDigitNumber = sum % 1000;

        // Convert to string with padding
        string format = $"D{length}";
        return threeDigitNumber.ToString(format);
    }

    public static string GenerateRandomNumericCode(this string value, int length = 3)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "000";

        // Remove any whitespace and convert to uppercase
        // var processedName = value.Replace(" ", "").ToUpper();
        var processedName = value;

        // Get hash code from the name
        int hashCode = Math.Abs(processedName.GetHashCode());

        // Take last 3 digits using modulo
        int threeDigitNumber = Math.Abs(hashCode * Random.Shared.Next(0, 999) % 1000);

        // Convert to string with padding
        string format = $"D{length}";
        return threeDigitNumber.ToString(format);
    }


    public static string GenerateNameCode(this string value, int length = 4)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "XXX";

        var initials = value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(x => x.Length > 0)
                            .Select(x => char.ToUpper(x[0]));
        //.Take(length);

        if (initials.Count() < length)
        {
            return string.Concat(initials).PadRight(length, 'X');
        }

        return string.Concat(initials);
    }
}