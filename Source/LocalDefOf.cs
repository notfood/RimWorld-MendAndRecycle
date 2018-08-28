using RimWorld;
using Verse;

namespace MendAndRecycle
{

    public static class LocalDefOf
    {
        [DefOf]
        public static class Recipe {
            public static RecipeDef MendSimpleApparel;
            public static RecipeDef MendComplexApparel;
        }

        [DefOf]
		public static class Job
		{
			public static JobDef Mend;
			public static JobDef Recycle;
		}
    }
}
