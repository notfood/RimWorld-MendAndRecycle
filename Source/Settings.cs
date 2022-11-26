using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;
using UnityEngine;
using Verse;

namespace MendAndRecycle
{
    public class Settings : ModSettings
    {
        public static Dictionary<TechLevel, float> chances = new Dictionary<TechLevel, float>()
        {
            {TechLevel.Undefined, 0.01f},
            {TechLevel.Animal, 0.01f},
            {TechLevel.Neolithic, 0.02f},
            {TechLevel.Medieval, 0.03f},
            {TechLevel.Industrial, 0.04f},
            {TechLevel.Spacer, 0.05f},
            {TechLevel.Ultra, 0.10f},
            {TechLevel.Archotech, 0.25f}
        };

        public static bool removesDeadman = true;
        public static bool useTableMending = true;
        public static bool requiresFuel = true;
        public static bool requiresPower = true;

        public static float costFromMaxHitPoints = 0.1f;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref removesDeadman, "removesDeadman", true, true);
            Scribe_Values.Look(ref useTableMending, "useTableMending", true, true);
            Scribe_Values.Look(ref requiresFuel, "requiresFuel", true, true);
            Scribe_Values.Look(ref requiresPower, "requiresPower", true, true);

            Scribe_Values.Look(ref costFromMaxHitPoints, "costFromMaxHitPoints", 0.1f, true);

            Dictionary<TechLevel, float> savedChances = chances;
            Scribe_Collections.Look(ref savedChances, "chances");
            if (savedChances != null) {
                chances = savedChances;
            }
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard(GameFont.Small)
            {
                ColumnWidth = rect.width
            };

            l.Begin(rect);

            l.CheckboxLabeled(ResourceBank.Strings.RemovesDeadman, ref removesDeadman, ResourceBank.Strings.RemovesDeadmanTooltip);
            l.CheckboxLabeled(ResourceBank.Strings.UseTableMending, ref useTableMending, ResourceBank.Strings.UseTableMendingTooltip);
            l.CheckboxLabeled(ResourceBank.Strings.RequiresFuel, ref requiresFuel, ResourceBank.Strings.RequiresFuelTooltip);
            l.CheckboxLabeled(ResourceBank.Strings.RequiresPower, ref requiresPower, ResourceBank.Strings.RequiresPowerTooltip);

            l.Gap(6);

            l.Label(ResourceBank.Strings.MaxHPCost + ": " + costFromMaxHitPoints.ToString("0.00"));
            costFromMaxHitPoints = l.Slider(costFromMaxHitPoints, 0f, 1f);

            l.Gap(6);

            l.Label(ResourceBank.Strings.FailChances);
            l.Gap(4);
            foreach(var tech in chances.Keys.ToList()) {
                if (tech == TechLevel.Undefined) {
                    continue;
                }
                float value = chances[tech];
                l.Label(tech.ToStringHuman().CapitalizeFirst() + ": " + value.ToString("0.00"));
                value = l.Slider(value, 0f, 1f);
                chances[tech] = value;
                l.Gap(4);
            }
            l.End();
        }
    }
}