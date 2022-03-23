#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace CF_LeatherSkinAsArmor
{
    public class Patcher : Mod
    {
        public static Settings Settings = new();
        string stuffEffectMultiplierArmorBuffer;

        public Patcher(ModContentPack pack) : base(pack)
        {
            Settings = GetSettings<Settings>();
            DoPatching();
        }
        public override string SettingsCategory()
        {
            return "Leather Skin As Armor";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);
            {
                var rect = list.Label(StatDefOf.StuffEffectMultiplierArmor.label, tooltip: "Animals gain additional armor (innermost) through the leather they produce");
                Widgets.TextFieldNumeric(rect.RightPartPixels(50), ref Settings.StuffEffectMultiplierArmor, ref stuffEffectMultiplierArmorBuffer, 0, 2);
            }
            list.End();
            base.DoSettingsWindowContents(inRect);
        }

        public void DoPatching()
        {
            var harmony = new Harmony("com.colinfang.LeatherSkinAsArmor");
            harmony.PatchAll();
        }
    }

    public class Settings : ModSettings
    {
        public float StuffEffectMultiplierArmor = 0.2f;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref StuffEffectMultiplierArmor, "StuffEffectMultiplierArmor", 0.2f);
            base.ExposeData();
        }
    }


    public class StatPart_AnimalArmor: StatPart
    {
        public StatDef? stuffPowerStat;
        public float multiplier;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is Pawn pawn && pawn.RaceProps.leatherDef is {} leatherDef)
            {
                val += leatherDef.GetStatValueAbstract(stuffPowerStat) * multiplier;
            }
        }
        public override string? ExplanationPart(StatRequest req)
        {
            if (req.Thing is Pawn pawn && pawn.RaceProps.leatherDef is {} leatherDef)
            {
                var stuffStat = leatherDef.GetStatValueAbstract(stuffPowerStat);
                if (stuffStat == 0 || multiplier == 0) {
                    return null;
                }

                var value = stuffStat * multiplier;
                var sb = new StringBuilder()
                    .Append("StatsReport_Material".Translate())
                    .Append($" ({leatherDef.label}): +{value * 100:F1}% = {stuffStat:F2} x {multiplier:F2}");
                return sb.ToString();
            }
            return null;
        }
    }


    [HarmonyPatch(typeof(DefGenerator))]
    [HarmonyPatch(nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public static class Patch_DefGenerator_GenerateImpliedDefs_PreResolve
    {
        public static void AddStatpart(StatDef stat)
        {
            var stuffStatPart = stat.GetStatPart<StatPart_Stuff>();
            _ = stuffStatPart?.stuffPowerStat ?? throw new ArgumentException($"Missing StatPart_Stuff.stuffPowerStat in {stat}");
            stat.parts.Add(new StatPart_AnimalArmor() {
                stuffPowerStat = stuffStatPart.stuffPowerStat, multiplier = Patcher.Settings.StuffEffectMultiplierArmor
            });
        }

        public static void Postfix()
        {
            AddStatpart(StatDefOf.ArmorRating_Sharp);
            AddStatpart(StatDefOf.ArmorRating_Blunt);
            AddStatpart(StatDefOf.ArmorRating_Heat);
        }
    }

}