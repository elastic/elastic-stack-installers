using System.Text;

namespace ElastiBuild.Infra
{
    public static class StringExtensions
    {
        public static string Quote(this string value)
        {
            return "\"" + value + "\"";
        }

        public static string JoinTo(this string value, params string[] others)
        {
            var builder = new StringBuilder(value);
            foreach (var v in others)
                builder.Append(v);

            return builder.ToString();
        }
    }
}