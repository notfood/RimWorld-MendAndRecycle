using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Mending
{
    public class SettingsController : Mod
    {
        public SettingsController(ModContentPack content) : base(content)
        {
            base.GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "mending".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }
    }

    public class Settings : ModSettings
    {
        private static readonly int[] DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE;
        private static readonly int[] DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE;

        static Settings()
        {
            var qualities = Enum.GetValues(typeof(QualityCategory));
            DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE = new int[qualities.Length];
            DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE = new int[qualities.Length];
            for (int i = 0; i < qualities.Length; ++i)
            {
                // Not needed but this is to make sure that every QualityCategory will be set
                DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[i] = 0;
                DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[i] = 0;
            }

            DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Awful] = 4;
            DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Poor] = 3;
            DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Normal] = 3;
            DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Good] = 2;
            DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Excellent] = 2;
            DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Masterwork] = 1;
            DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Legendary] = 0;

            DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Awful] = 3;
            DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Poor] = 2;
            DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Normal] = 2;
            DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Good] = 1;
            DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Excellent] = 1;
            DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Masterwork] = 0;
            DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[(int)QualityCategory.Legendary] = 0;

            for (int i = 0; i < qualities.Length; ++i)
            {
                TechLevelRangeUtil.PreIndustrial.FailChanceByQuality[i] = DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[i];
                TechLevelRangeUtil.PostIndustrial.FailChanceByQuality[i] = DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[i];
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref TechLevelRangeUtil.PreIndustrial, "mending.PreIndustrial");
            Scribe_Deep.Look(ref TechLevelRangeUtil.PostIndustrial, "mending.PostIndustrial");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var qualities = Enum.GetValues(typeof(QualityCategory));
                for (int i = 0; i < qualities.Length; ++i)
                {
                    if (TechLevelRangeUtil.PreIndustrial.FailChanceByQuality[i] == -1)
                        TechLevelRangeUtil.PreIndustrial.FailChanceByQuality[i] = DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE[i];

                    if (TechLevelRangeUtil.PostIndustrial.FailChanceByQuality[i] == -1)
                        TechLevelRangeUtil.PostIndustrial.FailChanceByQuality[i] = DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE[i];
                }
            }
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard(GameFont.Small)
            {
                ColumnWidth = Math.Min(400, rect.width / 2)
            };

            l.Begin(rect);
            l.Label("mending.FailChances".Translate());
            l.Gap(6);
            l.Label("mending.PreIndustrial".Translate());
            l.Gap(4);
            DrawFailChances(l, TechLevelRangeUtil.PreIndustrial, DEFAULT_PRE_INDUSTRIAL_FAIL_CHANCE);
            l.Gap(8);
            l.Label("mending.PostIndustrial".Translate());
            l.Gap(4);
            DrawFailChances(l, TechLevelRangeUtil.PostIndustrial, DEFAULT_POST_INDUSTRIAL_FAIL_CHANCE);
            l.End();
        }

        private static void DrawFailChances(Listing_Standard l, TechLevelRange techLevel, int[] defaults)
        {
            var qualities = Enum.GetValues(typeof(QualityCategory));
            for (int i = 0; i < qualities.Length; ++i)
            {
                int failChance = techLevel.FailChanceByQuality[i];
                string buffer = failChance.ToString();
                NumberInput(l, ((QualityCategory)i).ToString(), ref failChance, ref buffer, 0, 100);
                techLevel.FailChanceByQuality[i] = failChance;
            }
            if (l.ButtonText("ResetButton".Translate()))
            {
                for (int i = 0; i < defaults.Length; ++i)
                {
                    techLevel.FailChanceByQuality[i] = defaults[i];
                }
            }
        }

        private static void NumberInput(Listing_Standard l, string label, ref int val, ref string buffer, int min, int max)
        {
            try
            {
                l.TextFieldNumericLabeled<int>(label, ref val, ref buffer, min, max);
            }
            catch
            {
                val = min;
                buffer = min.ToString();
            }
        }
    }
}