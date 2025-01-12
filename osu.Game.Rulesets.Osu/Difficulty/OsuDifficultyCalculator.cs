// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.0655;

        public override int Version => 20241007;

        public OsuDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new OsuDifficultyAttributes { Mods = mods };

            double aimRating = Math.Sqrt(skills[0].DifficultyValue()) * difficulty_multiplier;
            double aimRatingNoSliders = Math.Sqrt(skills[1].DifficultyValue()) * difficulty_multiplier;
            double speedRating = Math.Sqrt(skills[2].DifficultyValue()) * difficulty_multiplier;
            double speedNotes = ((Speed)skills[2]).RelevantNoteCount();
            double difficultSliders = ((Aim)skills[0]).GetDifficultSliders();
            double flashlightRating = 0.0;
            double hybridRating = Math.Sqrt(skills[3].DifficultyValue()) * difficulty_multiplier;

            if (mods.Any(h => h is OsuModFlashlight))
                flashlightRating = Math.Sqrt(skills[4].DifficultyValue()) * difficulty_multiplier;

            double aimDifficultyStrainCount = ((OsuStrainSkill)skills[0]).CountTopWeightedStrains();
            double speedDifficultyStrainCount = ((OsuStrainSkill)skills[2]).CountTopWeightedStrains();
            double hybridDifficultyStrainCount = ((OsuStrainSkill)skills[3]).CountTopWeightedStrains();

            double aimNoSlidersTopWeightedSliderCount = ((OsuStrainSkill)skills[1]).CountTopWeightedSliders();
            double aimNoSlidersDifficultyStrainCount = ((OsuStrainSkill)skills[1]).CountTopWeightedStrains();
            double aimTopWeightedSliderFactor = aimNoSlidersTopWeightedSliderCount / (aimNoSlidersDifficultyStrainCount - aimNoSlidersTopWeightedSliderCount);
            double speedTopWeightedSliderCount = ((OsuStrainSkill)skills[2]).CountTopWeightedSliders();
            double speedTopWeightedSliderFactor = speedTopWeightedSliderCount / (speedDifficultyStrainCount - speedTopWeightedSliderCount);

            if (mods.Any(m => m is OsuModTouchDevice))
            {
                aimRating = Math.Pow(aimRating, 0.8);
                aimRatingNoSliders = Math.Pow(aimRatingNoSliders, 0.8);
                flashlightRating = Math.Pow(flashlightRating, 0.8);
            }

            if (mods.Any(h => h is OsuModRelax))
            {
                aimRating *= 0.9;
                aimRatingNoSliders *= 0.9;
                speedRating = 0.0;
                flashlightRating *= 0.7;
            }
            else if (mods.Any(h => h is OsuModAutopilot))
            {
                speedRating *= 0.5;
                aimRating = 0.0;
                flashlightRating *= 0.4;
            }

            double aimRelevantObjectCount = ((OsuStrainSkill)skills[0]).CountRelevantObjects();
            double aimNoSlidersRelevantObjectCount = ((OsuStrainSkill)skills[1]).CountRelevantObjects();
            double speedRelevantObjectCount = ((OsuStrainSkill)skills[2]).CountRelevantObjects();
            double hybridRelevantObjectCount = ((OsuStrainSkill)skills[3]).CountRelevantObjects();

            double aimLengthBonus = 1.0 + Math.Min(1.0, aimRelevantObjectCount / 300.0) +
                                    (aimRelevantObjectCount > 300.0 ? 2.0 * Math.Log10(aimRelevantObjectCount / 300.0) : 0);
            aimRating *= Math.Cbrt(aimLengthBonus);

            double aimNoSlidersLengthBonus = 1.0 + Math.Min(1.0, aimNoSlidersRelevantObjectCount / 300.0) +
                                             (aimNoSlidersRelevantObjectCount > 300.0 ? 2.0 * Math.Log10(aimNoSlidersRelevantObjectCount / 300.0) : 0);
            aimRatingNoSliders *= Math.Cbrt(aimNoSlidersLengthBonus);

            double speedLengthBonus = 1.0 + Math.Min(0.2, speedRelevantObjectCount / 900.0) +
                                      (speedRelevantObjectCount > 300 ? 0.5 * Math.Log10(speedRelevantObjectCount / 300.0) : 0.0);
            speedRating *= Math.Cbrt(speedLengthBonus);

            double hybridLengthBonus = 1.0 + Math.Min(0.5, hybridRelevantObjectCount / 800.0) +
                                    (hybridRelevantObjectCount > 400.0 ? 0.5 * Math.Log10(hybridRelevantObjectCount / 400.0) : 0);
            hybridRating *= Math.Cbrt(hybridLengthBonus);
            
            hybridRating = 2 * Math.Min(0.4, 1.5 * Math.Pow(hybridRating / Math.Max(aimRating, speedRating), 3));

            double sliderFactor = aimRating > 0 ? aimRatingNoSliders / aimRating : 1;

            double baseAimPerformance = OsuStrainSkill.DifficultyToPerformance(aimRating);
            double baseSpeedPerformance = OsuStrainSkill.DifficultyToPerformance(speedRating);
            double baseHybridPerformance = OsuStrainSkill.DifficultyToPerformance(hybridRating);    
            double baseFlashlightPerformance = 0.0;

            if (mods.Any(h => h is OsuModFlashlight))
                baseFlashlightPerformance = Flashlight.DifficultyToPerformance(flashlightRating);

            double basePerformance =
                Math.Pow(
                    Math.Pow(baseAimPerformance, 1.9 - hybridRating) +
                    Math.Pow(baseSpeedPerformance, 1.9 - hybridRating) +
                    Math.Pow(baseFlashlightPerformance, 1.9 - hybridRating), 1.0 / (1.9 - hybridRating)
                );

            double starRating = basePerformance > 0.00001
                ? Math.Cbrt(OsuPerformanceCalculator.PERFORMANCE_BASE_MULTIPLIER) * 0.026 * (Math.Cbrt(100000 / Math.Pow(2, 1 / 1.1) * basePerformance) + 4)
                : 0;

            double preempt = IBeatmapDifficultyInfo.DifficultyRange(beatmap.Difficulty.ApproachRate, 1800, 1200, 450) / clockRate;
            double drainRate = beatmap.Difficulty.DrainRate;

            int hitCirclesCount = beatmap.HitObjects.Count(h => h is HitCircle);
            int sliderCount = beatmap.HitObjects.Count(h => h is Slider);
            int spinnerCount = beatmap.HitObjects.Count(h => h is Spinner);

            HitWindows hitWindows = new OsuHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            double hitWindowGreat = hitWindows.WindowFor(HitResult.Great) / clockRate;
            double hitWindowOk = hitWindows.WindowFor(HitResult.Ok) / clockRate;
            double hitWindowMeh = hitWindows.WindowFor(HitResult.Meh) / clockRate;


            OsuDifficultyAttributes attributes = new OsuDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                AimDifficulty = aimRating,
                AimDifficultSliderCount = difficultSliders,
                SpeedDifficulty = speedRating,
                SpeedNoteCount = speedNotes,
                HybridRating = hybridRating,
                FlashlightDifficulty = flashlightRating,
                SliderFactor = sliderFactor,
                AimDifficultStrainCount = aimDifficultyStrainCount,
                SpeedDifficultStrainCount = speedDifficultyStrainCount,
                AimTopWeightedSliderFactor = aimTopWeightedSliderFactor,
                SpeedTopWeightedSliderFactor = speedTopWeightedSliderFactor,
                ApproachRate = preempt > 1200 ? (1800 - preempt) / 120 : (1200 - preempt) / 150 + 5,
                OverallDifficulty = (80 - hitWindowGreat) / 6,
                GreatHitWindow = hitWindowGreat,
                OkHitWindow = hitWindowOk,
                MehHitWindow = hitWindowMeh,
                DrainRate = drainRate,
                MaxCombo = beatmap.GetMaxCombo(),
                HitCircleCount = hitCirclesCount,
                SliderCount = sliderCount,
                SpinnerCount = spinnerCount,
            };

            return attributes;
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            List<DifficultyHitObject> objects = new List<DifficultyHitObject>();

            // The first jump is formed by the first two hitobjects of the map.
            // If the map has less than two OsuHitObjects, the enumerator will not return anything.
            for (int i = 1; i < beatmap.HitObjects.Count; i++)
            {
                var lastLast = i > 1 ? beatmap.HitObjects[i - 2] : null;
                objects.Add(new OsuDifficultyHitObject(beatmap.HitObjects[i], beatmap.HitObjects[i - 1], lastLast, clockRate, objects, objects.Count));
            }

            return objects;
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            var skills = new List<Skill>
            {
                new Aim(mods, true),
                new Aim(mods, false),
                new Speed(mods),
				new Hybrid(mods, true)
            };

            if (mods.Any(h => h is OsuModFlashlight))
                skills.Add(new Flashlight(mods));

            return skills.ToArray();
        }

        protected override Mod[] DifficultyAdjustmentMods => new Mod[]
        {
            new OsuModTouchDevice(),
            new OsuModDoubleTime(),
            new OsuModHalfTime(),
            new OsuModEasy(),
            new OsuModHardRock(),
            new OsuModFlashlight(),
            new MultiMod(new OsuModFlashlight(), new OsuModHidden())
        };
    }
}
