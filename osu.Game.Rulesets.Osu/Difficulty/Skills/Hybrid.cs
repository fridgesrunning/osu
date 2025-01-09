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
    public class Hybrid : OsuStrainSkill
    {
        public Hybrid(Mod[] mods)
            : base(mods)
        {
        }

        private double currentStrain;

        private double strainDecayBase => 0.5;

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => (currentStrain) * strainDecay(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            

            return 1;
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