﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class StaminaEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            // derive strainTime for calculation
            var osuCurrObj = (OsuDifficultyHitObject)current;

            double strainTime = osuCurrObj.StrainTime;
            double speedBonus = 0.0;
            double currentRhythm = RhythmEvaluator.EvaluateDifficultyOf(current);

            // Add additional scaling bonus for streams/bursts higher than 200bpm
            if (DifficultyCalculationUtils.MillisecondsToBPM(strainTime) > 200)
                speedBonus = Math.Pow((DifficultyCalculationUtils.BPMToMilliseconds(200) - strainTime) / 40, 2.4);

                // Add additional scaling bonus for streams/bursts higher than 250bpm
            if (DifficultyCalculationUtils.MillisecondsToBPM(strainTime) > 250)
                speedBonus += 0.5 * Math.Pow((DifficultyCalculationUtils.BPMToMilliseconds(250) - strainTime) / 40, 2.2);

                // Add additional scaling bonus for streams/bursts higher than 300bpm
            if (DifficultyCalculationUtils.MillisecondsToBPM(strainTime) > 300)
                speedBonus += 0.5 * Math.Pow((DifficultyCalculationUtils.BPMToMilliseconds(300) - strainTime) / 40, 2);

                // Add additional scaling bonus for streams/bursts higher than 350bpm
            if (DifficultyCalculationUtils.MillisecondsToBPM(strainTime) > 350)
                speedBonus += Math.Pow((DifficultyCalculationUtils.BPMToMilliseconds(350) - strainTime) / 40, 2);

            if (DifficultyCalculationUtils.MillisecondsToBPM(strainTime) < 200)
                strainTime *= Math.Pow(strainTime / DifficultyCalculationUtils.BPMToMilliseconds(200), 0.5);

            speedBonus /= currentRhythm;

            return (1 + speedBonus) * 1000 / strainTime;
        }
    }
}