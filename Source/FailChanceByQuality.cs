using RimWorld;
using Verse;
using System.Collections.Generic;
using System;

namespace Mending
{
    public class FailChanceByQuality : IExposable
    {
        private List<int> failChance;

        public FailChanceByQuality()
        {
            var qualities = Enum.GetValues(typeof(QualityCategory));
            this.failChance = new List<int>(qualities.Length);
            for (int i = 0; i < qualities.Length; ++i)
                this.failChance.Add(0);
        }

        public int this[QualityCategory i]
        {
            get { return this[(int)i]; }
            set { this[(int)i] = value; }
        }

        public int this[int i]
        {
            get { return this.failChance[i]; }
            set { this.failChance[i] = value; }
        }

        public void ExposeData()
        {
            Array qualities = Enum.GetValues(typeof(QualityCategory));
            for (int i = 0; i < qualities.Length; ++i)
            {
                int failChance = this.failChance[i];
                Scribe_Values.Look<int>(ref failChance, qualities.GetValue(i).ToString());
                this.failChance[i] = failChance;
            }
        }
    }

    public static class SuccessChanceUtil
    {
        private static readonly Random random = new Random();

        public static bool SuccessOnAction(float skillFactory, Thing t)
        {
            return SuccessOnAction(TechLevelRangeUtil.GetTechLevel().FailChanceByQuality, skillFactory, t);
        }

        public static bool SuccessOnAction(Pawn pawn, float skillFactory, Thing t)
        {
            return SuccessOnAction(TechLevelRangeUtil.GetTechLevel(pawn).FailChanceByQuality, skillFactory, t);
        }

        private static bool SuccessOnAction(FailChanceByQuality failChanceByQuality, float skillFactor, Thing t)
        {
            Log.Error("Begin SuccessChanceUtil.SuccessOnAction");
            QualityCategory qc;
            if (t.TryGetQuality(out qc))
            {
                int r = random.Next() % 101;
                float baseFc = failChanceByQuality[qc];
                float fc = baseFc * skillFactor;
                bool result = r >= fc;
                Log.Warning("r: " + r + " baseFc: " + baseFc + " fc: " + fc + " result: " + result);
                Log.Error("End SuccessChanceUtil.SuccessOnAction - Result: " + result);
                return result;
            }
            Log.Error("End SuccessChanceUtil.SuccessOnAction - true");
            return true;
        }
    }
}

