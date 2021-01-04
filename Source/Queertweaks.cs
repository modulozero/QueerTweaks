using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace QueerTweaks
{
    public class OrientationDistributionScenarioPart : ScenPart
    {
        private float asexualChance = 10f;
        private float bisexualChance = 50f;
        private float gayChance = 20f;
        private float straightChance = 20f;
        private PawnGenerationContext applies_to = PawnGenerationContext.PlayerStarter;

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            base.DoEditInterface(listing);
            if(listing.ButtonTextLabeled("Applies to", this.applies_to.ToStringHuman().CapitalizeFirst()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(label: PawnGenerationContext.All.ToStringHuman(), action: () => this.applies_to = PawnGenerationContext.All));
                options.Add(new FloatMenuOption(label: PawnGenerationContext.NonPlayer.ToStringHuman(), action: () => this.applies_to = PawnGenerationContext.NonPlayer));
                options.Add(new FloatMenuOption(label: PawnGenerationContext.PlayerStarter.ToStringHuman(), action: () => this.applies_to = PawnGenerationContext.PlayerStarter));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.Label("MZQ.StraightChance".Translate() + "  " + (int)straightChance + "%");
            straightChance = listing.Slider(straightChance, 0f, 100.99f);
            if (straightChance > 100.99f - bisexualChance - gayChance)
            {
                straightChance = 100.99f - bisexualChance - gayChance;
            }
            listing.Label("MZQ.BisexualChance".Translate() + "  " + (int)bisexualChance + "%");
            bisexualChance = listing.Slider(bisexualChance, 0f, 100.99f);
            if (bisexualChance > 100.99f - straightChance - gayChance)
            {
                bisexualChance = 100.99f - straightChance - gayChance;
            }
            listing.Label("MZQ.GayChance".Translate() + "  " + (int)gayChance + "%");
            gayChance = listing.Slider(gayChance, 0f, 100.99f);
            if (gayChance > 100.99f - straightChance - bisexualChance)
            {
                gayChance = 100.99f - straightChance - bisexualChance;
            }
            asexualChance = 100 - (int)straightChance - (int)bisexualChance - (int)gayChance;
            listing.Label("MZQ.AsexualChance".Translate() + "  " + asexualChance + "%");
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.asexualChance, "queerTweaksAsexualChance");
            Scribe_Values.Look(ref this.bisexualChance, "queerTweaksBisexualChance");
            Scribe_Values.Look(ref this.straightChance, "queerTweaksStraightChance");
            Scribe_Values.Look(ref this.gayChance, "queerTweaksGayChance");
        }

        public override bool CanCoexistWith(ScenPart other)
        {
            if (!(other is OrientationDistributionScenarioPart others))
            {
                return true;
            }

            return !PawnGenerationContextUtility.OverlapsWith(this.applies_to, others.applies_to);
        }

        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed)
        {
            base.Notify_PawnGenerated(pawn, context, redressed);
            if (!PawnGenerationContextUtility.Includes(this.applies_to, context))
            {
                return;
            }

            float orientation = Rand.Value;
            if (pawn.gender == Gender.None) { return; }

            Trait trait = pawn.story.traits.GetTrait(TraitDefOf.Bisexual);
            if (trait == null)
            {
                trait = pawn.story.traits.GetTrait(TraitDefOf.Gay);
            }
            if (trait == null)
            {
                trait = pawn.story.traits.GetTrait(TraitDefOf.Asexual);
            }
            Log.Message("Hi!");
            if(trait != null)
            {
                pawn.story.traits.allTraits.Remove(trait);

                // Try to refund the removed trait
                Log.Message("Refunding trait " + trait.Label + " for pawn " + pawn.Name.ToStringShort);
                List<TraitDef> doNotWant = new List<TraitDef>();   
                doNotWant.Add(TraitDefOf.Bisexual);
                doNotWant.Add(TraitDefOf.Gay);
                doNotWant.Add(TraitDefOf.Asexual);
                List<TraitDef> possibilities = DefDatabase<TraitDef>.AllDefsListForReading.Where(
                    predicate: td => 
                        !doNotWant.Contains(td) && 
                        !pawn.story.traits.allTraits.Where(t => t.def.Equals(td) || t.def.ConflictsWith(td)).Any() &&
                        !pawn.skills.skills.Where(s => !s.passion.Equals(Passion.None) && td.ConflictsWithPassion(s.def)).Any()
                        ).ToList();
                if (!possibilities.NullOrEmpty())
                {
                    TraitDef newTraitDef = possibilities.RandomElementByWeight(td => td.GetGenderSpecificCommonality(pawn.gender));
                    int degree = 0;
                    if (!newTraitDef.degreeDatas.NullOrEmpty())
                    {
                        degree = newTraitDef.degreeDatas.RandomElementByWeight(dd => dd.commonality).degree;
                    }
                    Trait newTrait = new Trait(newTraitDef, degree, false);
                    pawn.story.traits.GainTrait(newTrait);
                    Log.Message("Refunded with " + newTrait.LabelCap);
                }
            }

            if (orientation < asexualChance / 100)
            {
                if (LovePartnerRelationUtility.HasAnyExLovePartnerOfTheOppositeGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheOppositeGender(pawn))
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, 0, false));
                }
                else if (LovePartnerRelationUtility.HasAnyLovePartnerOfTheSameGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn))
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, 0, false));
                }
                else
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Asexual, 0, false));
                }
            }
            else if (orientation < ((asexualChance + bisexualChance) / 100))
            {
                pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, 0, false));
            }
            else if (orientation < ((asexualChance + bisexualChance + gayChance) / 100))
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

}