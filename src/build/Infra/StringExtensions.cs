using System.Text;

namespace ElastiBuild
{
    public static class StringExtensions
    {
        public static string Quote(this string value_)
        {
            return "\"" + value_ + "\"";
        }

        public static string JoinTo(this string value_, params string[] others_)
        {
            var builder = new StringBuilder(value_);
            foreach (var v in others_)
                builder.Append(v);

            return builder.ToString();
        }
    }
}