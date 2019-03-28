using System;

namespace GraphQL.Client.Extensions {
    public static class StringExtension {

        public static string ToCamelCase(this string str) {
            if (string.IsNullOrEmpty(str)) {
                return str;
            }

            if (str.Length == 1) {
                return Char.ToLowerInvariant(str[0]) + "";
            } else {
                return Char.ToLowerInvariant(str[0]) + str.Substring(1);
            }
        }

    }
}