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
            Scribe_Values.Look(ref this.applies_to, "queerTweaksAppliesTo");
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
            if (redressed || !PawnGenerationContextUtility.Includes(this.applies_to, context) || pawn.gender == Gender.None || !pawn.RaceProps.Humanlike || pawn.story == null)
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

            if (trait != null)
            {
                // Try to refund the removed trait
                Log.Message("Refunding trait " + trait.Label + " for pawn " + pawn.Name.ToStringShort);
                pawn.story.traits.allTraits.Remove(trait);
                
                List<TraitDef> doNotWant = new List<TraitDef>();   
                doNotWant.Add(TraitDefOf.Bisexual);
                doNotWant.Add(TraitDefOf.Gay);
                doNotWant.Add(TraitDefOf.Asexual);

                List<TraitDef> possibilities = DefDatabase<TraitDef>.AllDefsListForReading.Where(
                    predicate: td => 
                        !doNotWant.Contains(td) && 
                        !pawn.story.traits.allTraits.Where(t => t.def.Equals(td) || t.def.ConflictsWith(td)).Any() &&
                        !pawn.skills.skills.Where(s => !s.passion.Equals(Passion.None) && td.ConflictsWithPassion(s.def)).Any() &&
                        (td.requiredWorkTypes == null || !pawn.OneOfWorkTypesIsDisabled(td.requiredWorkTypes)) &&
                        (td.forcedPassions == null || !td.forcedPassions.Any(p => p.IsDisabled(pawn.story.DisabledWorkTagsBackstoryAndTraits, pawn.GetDisabledWorkTypes(true)))) && 
                        !pawn.WorkTagIsDisabled(td.requiredWorkTags)
                        ).ToList();

                if (!possibilities.NullOrEmpty())
                {
                    TraitDef newTraitDef = possibilities.RandomElementByWeight(td => td.GetGenderSpecificCommonality(pawn.gender));
                    int degree = 0;
                    if (!newTraitDef.degreeDatas.NullOrEmpty())
                    {
                        degree = PawnGenerator.RandomTraitDegree(newTraitDef);
                    }
                    Trait newTrait = new Trait(newTraitDef, degree, false);
                    pawn.story.traits.GainTrait(newTrait);

                    Log.Message("Refunded with " + newTrait.LabelCap);
                }
                else
                {
                    Log.Message("Couldn't find a trait to refund that with, giving up and moving on.");
                }
            }
            
            if (orientation < asexualChance / 100)
            {
                if (LovePartnerRelationUtility.HasAnyLovePartner(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheOppositeGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn))
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, PawnGenerator.RandomTraitDegree(TraitDefOf.Bisexual), false));
                }
                else
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Asexual, PawnGenerator.RandomTraitDegree(TraitDefOf.Asexual), false));
                }
            }
            else if (orientation < ((asexualChance + bisexualChance) / 100))
            {
                pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, PawnGenerator.RandomTraitDegree(TraitDefOf.Bisexual), false));
            }
            else if (orientation < ((asexualChance + bisexualChance + gayChance) / 100))
            {
                if (LovePartnerRelationUtility.HasAnyLovePartnerOfTheSameGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn))
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, PawnGenerator.RandomTraitDegree(TraitDefOf.Bisexual), false));
                }
                else
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Gay, PawnGenerator.RandomTraitDegree(TraitDefOf.Gay), false));
                }
            }
            else
            {
                if (LovePartnerRelationUtility.HasAnyLovePartnerOfTheSameGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn))
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual, PawnGenerator.RandomTraitDegree(TraitDefOf.Bisexual), false));
                }
            }
        }
    }

}