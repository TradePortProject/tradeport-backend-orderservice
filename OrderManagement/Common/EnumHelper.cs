using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace OrderManagement.Common
{
    public class EnumHelper
    {
        // Helper to get enum int value from display name
        public static int? GetEnumValueFromDisplayName<TEnum>(string displayName) where TEnum : struct, Enum
        {
            foreach (var field in typeof(TEnum).GetFields())
            {
                var attribute = field.GetCustomAttribute<DisplayAttribute>();
                if (attribute != null && attribute.Name.Equals(displayName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return (int)field.GetValue(null);
                }
            }
            return null;
        }


        // Helper to get display name from enum value
        public static string GetEnumDisplayName<TEnum>(TEnum value) where TEnum : Enum
        {
            var field = typeof(TEnum).GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DisplayAttribute>();
            return attribute?.Name ?? value.ToString();
        }
    }
}
