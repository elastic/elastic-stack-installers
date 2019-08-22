namespace ElastiBuild.BuildTarget
{
    public abstract class BuildTargetBase<T>
        where T: BuildTargetBase<T>, new()
    {
        protected BuildContext Context;

        public static T Create(BuildContext ctx_)
        {
            var tgt = new T
            {
                Context = ctx_
            };

            return tgt;
        }
    }
}
