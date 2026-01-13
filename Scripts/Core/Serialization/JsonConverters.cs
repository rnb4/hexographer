using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Hexographer.Core.Serialization;

/// <summary>
/// JSON converter for Godot.Color to/from hex string format "#RRGGBBAA".
/// </summary>
public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hexString = reader.GetString();
        if (string.IsNullOrEmpty(hexString))
            return Colors.White;

        return ParseHexColor(hexString);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ToHexString(value));
    }

    /// <summary>
    /// Converts a Color to hex string format "#RRGGBBAA".
    /// </summary>
    public static string ToHexString(Color color)
    {
        int r = (int)(color.R * 255);
        int g = (int)(color.G * 255);
        int b = (int)(color.B * 255);
        int a = (int)(color.A * 255);
        return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
    }

    /// <summary>
    /// Parses a hex color string to Color.
    /// Supports formats: "#RGB", "#RGBA", "#RRGGBB", "#RRGGBBAA"
    /// </summary>
    public static Color ParseHexColor(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return Colors.White;

        // Remove # prefix if present
        if (hex.StartsWith('#'))
            hex = hex[1..];

        try
        {
            return hex.Length switch
            {
                // #RGB
                3 => new Color(
                    ParseHexDigit(hex[0]) / 15f,
                    ParseHexDigit(hex[1]) / 15f,
                    ParseHexDigit(hex[2]) / 15f,
                    1f),

                // #RGBA
                4 => new Color(
                    ParseHexDigit(hex[0]) / 15f,
                    ParseHexDigit(hex[1]) / 15f,
                    ParseHexDigit(hex[2]) / 15f,
                    ParseHexDigit(hex[3]) / 15f),

                // #RRGGBB
                6 => new Color(
                    ParseHexByte(hex[0..2]) / 255f,
                    ParseHexByte(hex[2..4]) / 255f,
                    ParseHexByte(hex[4..6]) / 255f,
                    1f),

                // #RRGGBBAA
                8 => new Color(
                    ParseHexByte(hex[0..2]) / 255f,
                    ParseHexByte(hex[2..4]) / 255f,
                    ParseHexByte(hex[4..6]) / 255f,
                    ParseHexByte(hex[6..8]) / 255f),

                _ => Colors.White
            };
        }
        catch
        {
            return Colors.White;
        }
    }

    private static int ParseHexDigit(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => 0
        };
    }

    private static int ParseHexByte(string s)
    {
        return Convert.ToInt32(s, 16);
    }
}
