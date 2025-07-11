﻿namespace AlienRace
{
    using System.Collections.Generic;
    using System.Linq;
    using RimWorld;
    using UnityEngine;
    using Verse;

    [StaticConstructorOnStartup]
    public class ScenPart_StartingHumanlikes : ScenPart
    {

        static ScenPart_StartingHumanlikes()
        {
            ScenPartDef scenPart = new()
                                   {
                                       defName         = "StartingHumanlikes",
                                       label           = "HAR.StartingHumanlikesScenPart.Label".Translate(),
                                       scenPartClass   = typeof(ScenPart_StartingHumanlikes),
                                       category        = ScenPartCategory.StartingImportant,
                                       selectionWeight = 1.0f,
                                       summaryPriority = 10
                                   };
            scenPart.ResolveReferences();
            scenPart.PostLoad();
            DefDatabase<ScenPartDef>.Add(scenPart);
        }

        private PawnKindDef kindDef;
        private int pawnCount;
        private string buffer;

        public PawnKindDef KindDef
        {
            get => this.kindDef ??= PawnKindDefOf.Villager;
            set => this.kindDef = value;
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 3f);
            if (Widgets.ButtonText(scenPartRect.TopPart(0.45f), this.KindDef.label.CapitalizeFirst()))
            {
                List<FloatMenuOption> list = new();
                list.AddRange(DefDatabase<RaceSettings>.AllDefsListForReading.Where(ar => ar.pawnKindSettings.startingColonists != null)
                   .SelectMany(ar => ar.pawnKindSettings.startingColonists.SelectMany(ste => ste.pawnKindEntries.SelectMany(pke => pke.kindDefs)))
                   .Where(s => s != null).Select(pkd => new FloatMenuOption($"{pkd.LabelCap} | {pkd.race.LabelCap}", () => this.KindDef = pkd)));
                list.Add(new FloatMenuOption(PawnKindDefOf.Villager.LabelCap, () => this.KindDef = PawnKindDefOf.Villager));
                list.Add(new FloatMenuOption(PawnKindDefOf.Slave.LabelCap,    () => this.KindDef = PawnKindDefOf.Slave));
                Find.WindowStack.Add(new FloatMenu(list));
            }

            Widgets.TextFieldNumeric(scenPartRect.BottomPart(0.45f), ref this.pawnCount, ref this.buffer);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.pawnCount, "alienRaceScenPawnCount");
            Scribe_Defs.Look(ref this.kindDef, "PawnKindDefAlienRaceScen");
        }

        public override string Summary(Scenario scen) => ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith", ScenPart_StartingThing_Defined.PlayerStartWithIntro);

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == "PlayerStartsWith") 
                yield return this.KindDef.LabelCap + " x" + this.pawnCount;
        }

        public override bool TryMerge(ScenPart other)
        {
            if (other is not ScenPart_StartingHumanlikes others || others.KindDef != this.KindDef) 
                return false;

            this.pawnCount += others.pawnCount;
            return true;
        }

        public IEnumerable<Pawn> GetPawns()
        {
            List<WorkTypeDef> list = DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(wtd => wtd.requireCapableColonist).ToList();

            bool PawnCheck(Pawn p) => p != null && list.TrueForAll(match: w => !p.WorkTypeIsDisabled(w));


            for (int i = 0; i < this.pawnCount; i++)
            {
                Pawn newPawn = null;
                for (int x = 0; x < 200; x++)
                {
                    newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.KindDef, Faction.OfPlayer, PawnGenerationContext.PlayerStarter, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, colonistRelationChanceFactor: 26f, forceAddFreeWarmLayerIfNeeded: true));
                    if (PawnCheck(newPawn))
                        x = 200;
                }
                yield return newPawn;
            }
        }
    }
}