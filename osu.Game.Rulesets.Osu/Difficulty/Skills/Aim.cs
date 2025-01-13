// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq; 
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Objects;

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

        private double skillMultiplier => 13.15;
        private double strainDecayBase => 0.175;

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => currentStrain * strainDecay(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            var osuCurrObj = (OsuDifficultyHitObject)current;

            // d/t^(2 + some function of t) compensation, check 2731312

            double mitigation = (osuCurrObj.LazyJumpDistance / Math.Pow(osuCurrObj.StrainTime, 2)) /
            (( Math.Pow(strainDecayBase, osuCurrObj.StrainTime / 1000)/ (1 - Math.Pow(strainDecayBase, osuCurrObj.StrainTime / 1000) )) * (osuCurrObj.LazyJumpDistance / osuCurrObj.StrainTime));

            if (mitigation == mitigation)
            {
             currentStrain *= Math.Min(mitigation, 5) * 573.7336; // about 1x multiplier at 0ms https://www.desmos.com/calculator/wlorqfyf8j
            }
            
            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += AimEvaluator.EvaluateDifficultyOf(current, withSliders) * skillMultiplier;

            if (current.BaseObject is Slider)
                SliderStrains.Add(currentStrain);

            return currentStrain;
        }

        public double GetDifficultSliders()
        {
            if (SliderStrains.Count == 0)
                return 0;

            double[] sortedStrains = SliderStrains.OrderDescending().ToArray();

            double maxSliderStrain = sortedStrains.Max();
            if (maxSliderStrain == 0)
                return 0;

            return sortedStrains.Sum(strain => 1.0 / (1.0 + Math.Exp(-(strain / maxSliderStrain * 12.0 - 6.0))));
        }
    }
}
