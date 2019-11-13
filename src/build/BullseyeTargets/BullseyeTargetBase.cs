using System;

namespace ElastiBuild.BullseyeTargets
{
    public abstract class BullseyeTargetBase<T>
    {
        static readonly Lazy<string> lazyName = new Lazy<string>(
            () => typeof(T).Name.Replace("Target", string.Empty));

        public static string Name =>
            lazyName.Value;

        public static string NameWith(string product, string delimiter = "#") =>
            Name + delimiter + product;
    }
}
