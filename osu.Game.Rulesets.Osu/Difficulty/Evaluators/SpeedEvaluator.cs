﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class SpeedEvaluator
    {
        private const double single_spacing_threshold = 125; // 1.25 circles distance between centers
        private const double min_speed_bonus = 75; // ~200BPM
        private const double speed_balancing_factor = 40;
        private const double distance_multiplier = 1;

        /// <summary>
        /// Evaluates the difficulty of tapping the current object, based on:
        /// <list type="bullet">
        /// <item><description>time between pressing the previous and current object,</description></item>
        /// <item><description>distance between those objects,</description></item>
        /// <item><description>and how easily they can be cheesed.</description></item>
        /// </list>
        /// </summary>
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            // derive strainTime for calculation
            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuPrevObj = current.Index > 0 ? (OsuDifficultyHitObject)current.Previous(0) : null;
            var osuL2Obj = current.Index > 1 ? (OsuDifficultyHitObject)current.Previous(1) : null;
            var osuL3Obj = current.Index > 2 ? (OsuDifficultyHitObject)current.Previous(2) : null;
            var osuL4Obj = current.Index > 3 ? (OsuDifficultyHitObject)current.Previous(3) : null;
            var osuL5Obj = current.Index > 4 ? (OsuDifficultyHitObject)current.Previous(4) : null;
            var osuL6Obj = current.Index > 5 ? (OsuDifficultyHitObject)current.Previous(5) : null;
            var osuL7Obj = current.Index > 6 ? (OsuDifficultyHitObject)current.Previous(6) : null;
            var osuL8Obj = current.Index > 7 ? (OsuDifficultyHitObject)current.Previous(7) : null;

            // mitigate speed for anything below 9 notes by nerfing both deceleration and large acceleration, theres gotta a better way to do this but oh well
            double deceleration =

            osuCurrObj.StrainTime < 1.1 * (osuPrevObj?.StrainTime ?? 0) ? (osuPrevObj?.StrainTime ?? 0) < 1.1 * (osuL2Obj?.StrainTime ?? 0) ? (osuL2Obj?.StrainTime ?? 0) < 1.1 * (osuL3Obj?.StrainTime ?? 0) ? (osuL3Obj?.StrainTime ?? 0) < 1.1 * (osuL4Obj?.StrainTime ?? 0) ? (osuL4Obj?.StrainTime ?? 0) < 1.1 * (osuL5Obj?.StrainTime ?? 0) ? (osuL5Obj?.StrainTime ?? 0) < 1.1 * (osuL6Obj?.StrainTime ?? 0) ? (osuL6Obj?.StrainTime ?? 0) < 1.1 * (osuL7Obj?.StrainTime ?? 0) ? 
         
         // behavior if there has been...
           1 :                //tapping acceleration across all checked objects
             0.95 :           //L6 deceleration
             0.9 :             //L5 deceleration
              0.85 :          //L4 deceleration
                0.8 :          //L3 deceleration
                 0.75 :     //L2 deceleration
                  0.7 :     //last object deceleration
                   0.65;     //current object deceleration
 // We also need to nerf major acceleration.

            double acceleration =

            osuCurrObj.StrainTime > 0.45 * (osuPrevObj?.StrainTime ?? 0) ? (osuPrevObj?.StrainTime ?? 0) > 0.45 * (osuL2Obj?.StrainTime ?? 0) ? (osuL2Obj?.StrainTime ?? 0) > 0.45 * (osuL3Obj?.StrainTime ?? 0) ? (osuL3Obj?.StrainTime ?? 0) > 0.45 * (osuL4Obj?.StrainTime ?? 0) ? (osuL4Obj?.StrainTime ?? 0) > 0.45 * (osuL5Obj?.StrainTime ?? 0) ? (osuL5Obj?.StrainTime ?? 0) > 0.45 * (osuL6Obj?.StrainTime ?? 0) ? (osuL6Obj?.StrainTime ?? 0) > 0.45 * (osuL7Obj?.StrainTime ?? 0) ?

            //behavior if there is...
            1: //no major acceleration
            0.95:
            0.9:
            0.85:
            0.8:
            0.75:
            0.7:
            0.65;


            
            double strainTime = osuCurrObj.StrainTime;
            double doubletapness = 1.0 - osuCurrObj.GetDoubletapness((OsuDifficultyHitObject?)osuCurrObj.Next(0));

            // Cap deltatime to the OD 300 hitwindow.
            // 0.93 is derived from making sure 260bpm OD8 streams aren't nerfed harshly, whilst 0.92 limits the effect of the cap.
            strainTime /= Math.Clamp((strainTime / osuCurrObj.HitWindowGreat) / 0.93, 0.92, 1);

            // speedBonus will be 0.0 for BPM < 200
            double speedBonus = 0.0;

            // Add additional scaling bonus for streams/bursts higher than 200bpm
            if (strainTime < min_speed_bonus)
                speedBonus = 0.95 * Math.Pow((min_speed_bonus - strainTime) / speed_balancing_factor, 2);

                speedBonus *= Math.Min(deceleration, acceleration);

            double travelDistance = osuPrevObj?.TravelDistance ?? 0;
            double distance = travelDistance + osuCurrObj.MinimumJumpDistance;

            // Cap distance at single_spacing_threshold
            distance = Math.Min(distance, single_spacing_threshold);

            // Max distance bonus is 1 * `distance_multiplier` at single_spacing_threshold
            double distanceBonus = Math.Pow(distance / single_spacing_threshold, 4.5) * distance_multiplier;

            // Base difficulty with all bonuses
            double difficulty = (1 + speedBonus + distanceBonus) * 1000 / strainTime;

            // Apply penalty if there's doubletappable doubles
            return difficulty * doubletapness;
        }
    }
}