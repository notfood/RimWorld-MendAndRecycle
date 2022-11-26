using RimWorld;
using UnityEngine;
using Verse;

namespace MendAndRecycle
{
    public static class ResourceBank
    {
        public static class Strings
        {
            const string PREFIX = "MendRecycle.";

            static string TL(string s) => (PREFIX + s).Translate();

            public static readonly string MendAndRecycle = "MendRecycle".Translate();

            public static readonly string RemovesDeadman = TL("RemovesDeadman");
            public static readonly string RemovesDeadmanTooltip = TL("RemovesDeadmanTooltip");
            public static readonly string UseTableMending = TL("UseTableMending");
            public static readonly string UseTableMendingTooltip = TL("UseTableMendingTooltip");
            public static readonly string RequiresFuel = TL("RequiresFuel");
            public static readonly string RequiresFuelTooltip = TL("RequiresFuelTooltip");
            public static readonly string RequiresPower = TL("RequiresPower");
            public static readonly string RequiresPowerTooltip = TL("RequiresPowerTooltip");
            public static readonly string MaxHPCost = TL("MaxHPCost");
            public static readonly string FailChances = TL("FailChances");
        }

        [DefOf]
        public static class Recipe
        {
            public static RecipeDef MendSimpleApparel;
            public static RecipeDef MendComplexApparel;
            public static RecipeDef MendSimpleWeapon;
            public static RecipeDef MendComplexWeapon;
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

        [DefOf]
        public static class ResearchProject
        {
            public static ResearchProjectDef Mending;
            public static ResearchProjectDef Electricity;
        }

        [StaticConstructorOnStartup]
        public static class Textures
        {
            public static readonly Texture2D Outside = ContentFinder<Texture2D>.Get("UI/Designators/NoRoofArea", true);
            public static readonly Texture2D Inside = ContentFinder<Texture2D>.Get("UI/Designators/HomeAreaOn", true);
        }
    }
}

