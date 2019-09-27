namespace ElastiBuild.Infra
{
    public static class StringExtensions
    {
        public static string Quote(this string value)
        {
            return "\"" + value + "\"";
        }
    }
}
