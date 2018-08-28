using RimWorld;
using Verse;
using System.Collections.Generic;
using System;

namespace MendAndRecycle
{
    public class FailChanceByQuality : IExposable
    {
        List<int> failChance;

        public FailChanceByQuality()
        {
            var qualities = Enum.GetValues(typeof(QualityCategory));
            failChance = new List<int>(qualities.Length);
            for (int i = 0; i < qualities.Length; ++i)
                failChance.Add(0);
        }

        public int this[QualityCategory i]
        {
            get { return this[(int)i]; }
            set { this[(int)i] = value; }
        }

        public int this[int i]
        {
            get { return failChance[i]; }
            set { failChance[i] = value; }
        }

        public void ExposeData()
        {
            Array qualities = Enum.GetValues(typeof(QualityCategory));
            for (int i = 0; i < qualities.Length; ++i)
            {
                int chance = failChance[i];
                Scribe_Values.Look<int>(ref chance, qualities.GetValue(i).ToString(), -1);
                failChance[i] = chance;
            }
        }
    }

    public static class SuccessChanceUtil
    {
        static readonly Random random = new Random();

        public static bool SuccessOnAction(float skillFactory, Thing t)
        {
            return SuccessOnAction(TechLevelRangeUtil.GetTechLevel().FailChanceByQuality, skillFactory, t);
        }

        public static bool SuccessOnAction(Pawn pawn, float skillFactory, Thing t)
        {
            return SuccessOnAction(TechLevelRangeUtil.GetTechLevel(pawn).FailChanceByQuality, skillFactory, t);
        }

        static bool SuccessOnAction(FailChanceByQuality failChanceByQuality, float skillFactor, Thing t)
        {
            //Log.Error("Begin SuccessChanceUtil.SuccessOnAction");
            QualityCategory qc;
            if (t.TryGetQuality(out qc))
            {
                int r = random.Next() % 101;
                float baseFc = failChanceByQuality[qc];
                float fc = baseFc * skillFactor;
                bool result = r >= fc;
                //Log.Warning("r: " + r + " baseFc: " + baseFc + " fc: " + fc + " result: " + result);
                //Log.Error("End SuccessChanceUtil.SuccessOnAction - Result: " + result);
                return result;
            }
            //Log.Error("End SuccessChanceUtil.SuccessOnAction - true");
            return true;
        }
    }
}

