using Domain.Enums;

namespace Application.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Converts transmission enum to readable text.
    /// </summary>
    public static string ToDisplayName(this enTransmissionType type)
    {
        return type.ToString();
    }
    public static string ToLocalizeDisplayName(this enTransmissionType type, string lang)
    {
        if (lang.ToLower() == "ar")
        {
            return type == enTransmissionType.Automatic ? "أوتوماتيك" : "عادي";
        }
        return ToDisplayName(type);

    }

    /// <summary>
    /// Converts fleet status enum to readable text.
    /// </summary>
    public static string ToDisplayName(this enFleetConditionStatus status)
    {
        return status.ToString();
    }
}