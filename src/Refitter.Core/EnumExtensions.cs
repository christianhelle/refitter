using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Refitter.Core
{
    internal static class EnumExtensions
    {
        private static readonly ConcurrentDictionary<string, string> DescriptionCache = new();

        public static string ToDescription(this Enum value)
        {
            var key = $"{value.GetType().FullName}.{value}";

            var displayName = DescriptionCache.GetOrAdd(key, _ =>
            {
                var name = (DescriptionAttribute[])value!
                    .GetType()!
                    .GetTypeInfo()!
                    .GetField(value.ToString())!
                    .GetCustomAttributes(typeof(DescriptionAttribute), false);

                return name.Length > 0 ? name[0].Description : value.ToString();
            });

            return displayName;
        }
    }
}
