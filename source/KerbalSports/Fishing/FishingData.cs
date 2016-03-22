using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSports.Fishing
{
    public class FishingData
    {
        public class FishingStats
        {
            public int fishCaught;
            public double biggestFish;
        }

        public float skill;
        public Dictionary<CelestialBody, FishingStats> fishingStats = new Dictionary<CelestialBody, FishingStats>();

        public void CaughtFish(CelestialBody body, Fish fish)
        {
            if (!fishingStats.ContainsKey(body))
            {
                fishingStats[body] = new FishingStats();
            }
            fishingStats[body].biggestFish = Math.Max(fishingStats[body].biggestFish, fish.weight);
            fishingStats[body].fishCaught++;
        }

        public void IncreaseSkill(float skillIncrease)
        {
            skill += skillIncrease;
            skill = Math.Min(skill, 100);
        }

        public double BiggestFish(CelestialBody body)
        {
            if (!fishingStats.ContainsKey(body))
            {
                return 0.0;
            }
            return fishingStats[body].biggestFish;
        }

        public int FishCount(CelestialBody body)
        {
            if (!fishingStats.ContainsKey(body))
            {
                return 0;
            }
            return fishingStats[body].fishCaught;
        }
    }
}
