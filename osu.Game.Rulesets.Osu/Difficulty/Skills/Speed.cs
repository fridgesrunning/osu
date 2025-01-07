﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using System.Linq;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to press keys with regards to keeping up with the speed at which objects need to be hit.
    /// </summary>
    public class Speed : OsuStrainSkill
    {
        private const double single_spacing_threshold = OsuDifficultyHitObject.NORMALISED_DIAMETER * 1.5;
        private const double distance_multiplier = 0.5;
        private double totalMultiplier => 1;
        private double burstMultiplier => 1.8;
        private double staminaMultiplier => 0.11;

        private double currentBurstStrain;
        private double currentStaminaStrain;
        private double currentRhythm;

        public Speed(Mod[] mods)
            : base(mods)
        {
        }

        private double strainDecayBurst(double ms) => Math.Pow(0.1, ms / 1000);
        private double strainDecayStamina(double ms) => Math.Pow(0.1, Math.Pow(ms / 1000, 2));

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => (currentBurstStrain * currentRhythm) * strainDecayBurst(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentBurstStrain *= strainDecayBurst(((OsuDifficultyHitObject)current).StrainTime);
            currentRhythm = RhythmEvaluator.EvaluateDifficultyOf(current);
            double travelDistance = ((OsuDifficultyHitObject)current.Previous(0))?.TravelDistance ?? 0;
            double distance = travelDistance + ((OsuDifficultyHitObject)current).MinimumJumpDistance;
            double distanceBonus = Math.Pow(distance / single_spacing_threshold, 3.95) * distance_multiplier;
            currentBurstStrain += Math.Max(StaminaEvaluator.EvaluateDifficultyOf(current) * staminaMultiplier, (SpeedEvaluator.EvaluateDifficultyOf(current) - 10 * distanceBonus)) * burstMultiplier * Math.Sqrt(currentRhythm);
            

            currentStaminaStrain *= strainDecayStamina(((OsuDifficultyHitObject)current).StrainTime);
            currentStaminaStrain += StaminaEvaluator.EvaluateDifficultyOf(current) * staminaMultiplier;

            double combinedStrain = currentBurstStrain + currentStaminaStrain;

            return combinedStrain;
        }

        public double RelevantNoteCount()
        {
            if (ObjectStrains.Count == 0)
                return 0;

            double maxStrain = ObjectStrains.Max();
            if (maxStrain == 0)
                return 0;

            return ObjectStrains.Sum(strain => 1.0 / (1.0 + Math.Exp(-(strain / maxStrain * 12.0 - 6.0))));
        }
    }
}