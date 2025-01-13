// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class HybridEvaluator
    {

        public static double EvaluateDifficultyOf(DifficultyHitObject current, bool withSliderTravelDistance)
        {
            if (current.BaseObject is Spinner || current.Index <= 5 || current.Previous(0).BaseObject is Spinner)
                return 0;
        var osuCurrObj = (OsuDifficultyHitObject)current;
        var osuLastObj = (OsuDifficultyHitObject)current.Previous(0);
        var osuLastLastObj = (OsuDifficultyHitObject)current.Previous(1);
        var osuL3Obj = (OsuDifficultyHitObject)current.Previous(2);
        var osuL4Obj = (OsuDifficultyHitObject)current.Previous(3);
        var osuL5Obj = (OsuDifficultyHitObject)current.Previous(4);
        var osuL6Obj = (OsuDifficultyHitObject)current.Previous(5);
        
            double AimAverage = (
            AimEvaluator.EvaluateDifficultyOf(osuCurrObj, withSliderTravelDistance) +
            AimEvaluator.EvaluateDifficultyOf(osuLastObj, withSliderTravelDistance) +
            AimEvaluator.EvaluateDifficultyOf(osuLastLastObj, withSliderTravelDistance) +
            AimEvaluator.EvaluateDifficultyOf(osuL3Obj, withSliderTravelDistance) +
            AimEvaluator.EvaluateDifficultyOf(osuL4Obj, withSliderTravelDistance)) / 5;

            double SpeedAverage = (
            SpeedEvaluator.EvaluateDifficultyOf(osuCurrObj) +
            SpeedEvaluator.EvaluateDifficultyOf(osuLastObj) +
            SpeedEvaluator.EvaluateDifficultyOf(osuLastLastObj) +
            SpeedEvaluator.EvaluateDifficultyOf(osuL3Obj) +
            SpeedEvaluator.EvaluateDifficultyOf(osuL4Obj)) / 5;

            double LastAimAverage = (
            AimEvaluator.EvaluateDifficultyOf(osuL6Obj, withSliderTravelDistance) +
            AimEvaluator.EvaluateDifficultyOf(osuL5Obj, withSliderTravelDistance) +
            AimEvaluator.EvaluateDifficultyOf(osuLastLastObj, withSliderTravelDistance) +
            AimEvaluator.EvaluateDifficultyOf(osuL3Obj, withSliderTravelDistance) +
            AimEvaluator.EvaluateDifficultyOf(osuL4Obj, withSliderTravelDistance)) / 5;

            double LastSpeedAverage = (
            SpeedEvaluator.EvaluateDifficultyOf(osuL6Obj) +
            SpeedEvaluator.EvaluateDifficultyOf(osuL5Obj) +
            SpeedEvaluator.EvaluateDifficultyOf(osuLastLastObj) +
            SpeedEvaluator.EvaluateDifficultyOf(osuL3Obj) +
            SpeedEvaluator.EvaluateDifficultyOf(osuL4Obj)) / 5;

            double currRatio = Math.Sqrt(Math.Max(AimAverage, SpeedAverage)) / Math.Sqrt(Math.Min(AimAverage, SpeedAverage));

            double lastRatio = Math.Sqrt(Math.Max(LastAimAverage, LastSpeedAverage)) / Math.Sqrt(Math.Min(LastAimAverage, LastSpeedAverage));

            double RatioChange = Math.Max(currRatio, lastRatio) / Math.Min(currRatio, lastRatio);

            double shortFlowPenalty = 1;

            double shortFlowWeight = 0;

            if (osuCurrObj.Angle != null && osuLastObj.Angle != null && osuLastLastObj.Angle != null)
                shortFlowPenalty = Math.Min(1, (
               0.5 *  Math.Pow(DifficultyCalculationUtils.Smootherstep(Math.Min(osuCurrObj.Angle.Value, osuLastObj.Angle.Value), 180, 140), 0.9) +
                0.5 * Math.Pow(DifficultyCalculationUtils.Smootherstep(Math.Max(osuCurrObj.LazyJumpDistance, osuLastObj.LazyJumpDistance), 0, 300), 0.9))
               * 1.0 / 0.9 );

               if (Math.Max(osuCurrObj.StrainTime, osuLastObj.StrainTime) < 1.25 * Math.Min(osuCurrObj.StrainTime, osuLastObj.StrainTime))
               shortFlowWeight = 1.1 * DifficultyCalculationUtils.Smootherstep(DifficultyCalculationUtils.BPMToMilliseconds(osuCurrObj.StrainTime), 200, 275);

               RatioChange *= Math.Pow(shortFlowPenalty, shortFlowWeight);

            if (RatioChange < 1000)
            return RatioChange * Math.Min(AimAverage, SpeedAverage / 10);
            return 0;
            
        }

    }
}
