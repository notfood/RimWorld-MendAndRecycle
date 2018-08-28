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
            public static RecipeDef MakeMendingKit;
        }

        [DefOf]
		public static class Job
		{
			public static JobDef Mend;
			public static JobDef Recycle;
		}

        [DefOf]
        public static class Thing
        {
            public static ThingDef TableMending;
        }
    }
}
