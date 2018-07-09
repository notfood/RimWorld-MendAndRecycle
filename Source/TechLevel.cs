using RimWorld;
using Verse;

namespace Mending
{
    public struct TechLevelRange : IExposable
    {
        public readonly TechLevel Min;
        public readonly TechLevel Max;
        public FailChanceByQuality FailChanceByQuality;

        public TechLevelRange(TechLevel min, TechLevel max)
        {
            this.Min = min;
            this.Max = max;
            this.FailChanceByQuality = new FailChanceByQuality();
        }

        public bool IsInRange(TechLevel level)
        {
            return level >= this.Min && level <= this.Max;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref this.FailChanceByQuality, "FailChance");
        }
    }

    public static class TechLevelRangeUtil
    {
        public static TechLevelRange PreIndustrial = new TechLevelRange(TechLevel.Neolithic, TechLevel.Medieval);
        public static TechLevelRange PostIndustrial = new TechLevelRange(TechLevel.Industrial, TechLevel.Ultra);

        public static TechLevelRange GetTechLevel()
        {
            if (PreIndustrial.IsInRange(Faction.OfPlayer.def.techLevel))
            {
                return PreIndustrial;
            }
            return PostIndustrial;
        }

        public static TechLevelRange GetTechLevel(Pawn pawn)
        {
            if (pawn != null && pawn.Faction != null && pawn.Faction.def != null)
            {
                if (PreIndustrial.IsInRange(Faction.OfPlayer.def.techLevel))
                {
                    return PreIndustrial;
                }
                return PostIndustrial;
            }
            return GetTechLevel();
        }
    }
}

