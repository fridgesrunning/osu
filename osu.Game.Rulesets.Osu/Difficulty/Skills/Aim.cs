// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;


namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : OsuStrainSkill
    {
        public Aim(Mod[] mods, bool withSliders)
            : base(mods)
        {
            this.withSliders = withSliders;
        }

        private readonly bool withSliders;

        private double currentStrain;

        private double skillMultiplier => 14;
        private double strainDecayBase => 0.15;

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => currentStrain * strainDecay(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {

            var osuCurrObj = (OsuDifficultyHitObject)current;

            // d/t^(2 + some function of t) compensation, check 2731312

            double mitigation = (osuCurrObj.LazyJumpDistance / Math.Pow(osuCurrObj.StrainTime, 2)) / (( Math.Pow(strainDecayBase, osuCurrObj.StrainTime / 1000)/ (1 - Math.Pow(strainDecayBase, osuCurrObj.StrainTime / 1000) )) * (osuCurrObj.LazyJumpDistance / osuCurrObj.StrainTime));

            if (mitigation == mitigation)
            {
             currentStrain *= Math.Min(mitigation, 1000) * 527.115;
            }

            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += AimEvaluator.EvaluateDifficultyOf(current, withSliders) * skillMultiplier;
            ObjectStrains.Add(currentStrain);


            return currentStrain;
        }
    }
}