// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using System.Linq;
using osu.Framework.Utils;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public abstract class OsuStrainSkill : StrainSkill
    {

        /// <summary>
        /// The baseline multiplier applied to the section with the biggest strain.
        /// </summary>
        protected virtual double ReducedStrainBaseline => 0.75;

        protected List<double> ObjectStrains = new List<double>();
        protected double Difficulty;

        protected OsuStrainSkill(Mod[] mods)
            : base(mods)
        {
        }
		
        public double strainCount = 1;

        public override double DifficultyValue()
        {
            Difficulty = 0;
            double weight = 1;

            // Sections with 0 strain are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
            // These sections will not contribute to the difficulty.
            var peaks = GetCurrentStrainPeaks().Where(p => p > 0);

            List<double> strains = peaks.OrderDescending().ToList();

            // We are reducing the highest strains first to account for extreme difficulty spikes
            for (int i = 0; i < Math.Max(strains.Count, 1); i++)
            {
                double scale = Math.Log10(Interpolation.Lerp(1, strains.Count, Math.Clamp((float)i / strains.Count, 0, 1)));
                strains[i] *= Interpolation.Lerp(ReducedStrainBaseline, 1.0, scale);
            }

			strainCount = strains.Count;

            // Difficulty is the weighted sum of the highest strains from every section.
            // We're sorting from highest to lowest strain.
            foreach (double strain in strains.OrderDescending())
            {
                Difficulty += strain * weight;
                weight *= DecayWeight;
            }

            return Difficulty;
        }

        /// <summary>
        /// Returns something that can be used in length bonus calcuation. That's all I can say.
        /// </summary>
        public double CountDifficultStrains()
        {
            if (Difficulty == 0)
                return 0.0;

            double consistentTopStrain = (Difficulty / strainCount * 0.8);
            
            return ObjectStrains.Sum(s => 1 / (1 + Math.Exp(-10 * (s / consistentTopStrain - strainCount / 12))));
        }

        public static double DifficultyToPerformance(double difficulty) => Math.Pow(5.0 * Math.Max(1.0, difficulty / 0.0675) - 4.0, 3.0) / 100000.0;
    }
}

