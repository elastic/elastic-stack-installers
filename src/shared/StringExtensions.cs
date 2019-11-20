using System.Text;

namespace ElastiBuild.Extensions
{
    public static class StringExtensions
    {
        public static string Quote(this string value) =>
            "\"" + value + "\"";

        public static string JoinTo(this string value, params string[] others)
        {
            var builder = new StringBuilder(value);
            foreach (var v in others)
                builder.Append(v);

            return builder.ToString();
        }

        public static bool IsEmpty(this string value) =>
            string.IsNullOrWhiteSpace(value);
    }
}
