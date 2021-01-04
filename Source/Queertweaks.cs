using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace QueerTweaks
{
    class QueerTweaks : Mod
    {
        public static Settings Settings;
        public override string SettingsCategory() { return "MZQ.QueerTweaks".Translate(); }
        public override void DoSettingsWindowContents(Rect canvas) { Settings.DoWindowContents(canvas); }
        public QueerTweaks(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("xyz.modzero.rimworld.mod.queertweaks");
            harmony.PatchAll();
            Settings = GetSettings<Settings>();
        }
    }

    public class Settings : ModSettings
    {
        public float asexualChance = 10f;
        public float bisexualChance = 50f;
        public float gayChance = 20f;
        public float straightChance = 20f;

        public void DoWindowContents(Rect canvas)
        {
            Listing_Standard list = new Listing_Standard
            {
                ColumnWidth = canvas.width
            };
            list.Begin(canvas);
            list.Gap(24);
            Text.Font = GameFont.Tiny;
            list.Label("MZQ.Overview".Translate());
            Text.Font = GameFont.Small;
            list.Gap();
            list.Label("MZQ.StraightChance".Translate() + "  " + (int)straightChance + "%");
            straightChance = list.Slider(straightChance, 0f, 100.99f);
            if (straightChance > 100.99f - bisexualChance - gayChance)
            {
                straightChance = 100.99f - bisexualChance - gayChance;
            }
            list.Gap();
            list.Label("MZQ.BisexualChance".Translate() + "  " + (int)bisexualChance + "%");
            bisexualChance = list.Slider(bisexualChance, 0f, 100.99f);
            if (bisexualChance > 100.99f - straightChance - gayChance)
            {
                bisexualChance = 100.99f - straightChance - gayChance;
            }
            list.Gap();
            list.Label("MZQ.GayChance".Translate() + "  " + (int)gayChance + "%");
            gayChance = list.Slider(gayChance, 0f, 100.99f);
            if (gayChance > 100.99f - straightChance - bisexualChance)
            {
                gayChance = 100.99f - straightChance - bisexualChance;
            }
            list.Gap();
            asexualChance = 100 - (int)straightChance - (int)bisexualChance - (int)gayChance;
            list.Label("MZQ.AsexualChance".Translate() + "  " + asexualChance + "%");
            list.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref asexualChance, "asexualChance", 10.0f);
            Scribe_Values.Look(ref bisexualChance, "bisexualChance", 50.0f);
            Scribe_Values.Look(ref gayChance, "gayChance", 20.0f);
            Scribe_Values.Look(ref straightChance, "straightChance", 20.0f);
        }

        public static class ExtraTraits
        {
            public static void AssignOrientation(Pawn pawn)
            {
                float orientation = Rand.Value;
                if (pawn.gender == Gender.None) { return; }
                if (orientation < QueerTweaks.Settings.asexualChance / 100)
                {
                    if (LovePartnerRelationUtility.HasAnyExLovePartnerOfTheOppositeGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheOppositeGender(pawn)) 
                    {
                        pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, 0, false));
                    } 
                    else if(LovePartnerRelationUtility.HasAnyLovePartnerOfTheSameGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn)) 
                    {
                        pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, 0, false));
                    } 
                    else
                    {
                        pawn.story.traits.GainTrait(new Trait(TraitDefOf.Asexual, 0, false));
                    }
                } 
                else if (orientation < ((QueerTweaks.Settings.asexualChance + QueerTweaks.Settings.bisexualChance) / 100))
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, 0, false));
                }
                else if (orientation < ((QueerTweaks.Settings.asexualChance + QueerTweaks.Settings.bisexualChance + QueerTweaks.Settings.gayChance) / 100))
                {
                    if (LovePartnerRelationUtility.HasAnyLovePartnerOfTheOppositeGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheOppositeGender(pawn))
                    {
                        pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, 0, false));
                    }
                    else
                    {
                        pawn.story.traits.GainTrait(new Trait(TraitDefOf.Gay, 0, false));
                    }
                }
                else
                {
                    if (LovePartnerRelationUtility.HasAnyLovePartnerOfTheSameGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn))
                    {
                        pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, 0, false));
                    }
                    if (LovePartnerRelationUtility.HasAnyLovePartnerOfTheSameGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn))
                    {
                        pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, 0, false));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits", null)]
        public static class PawnGenerator_GenerateTraits
        {
            public static void Postfix(Pawn pawn)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Asexual) || pawn.story.traits.HasTrait(TraitDefOf.Bisexual) || pawn.story.traits.HasTrait(TraitDefOf.Gay))
                {
                    return;
                }

                ExtraTraits.AssignOrientation(pawn);
            }
        }
    }
}