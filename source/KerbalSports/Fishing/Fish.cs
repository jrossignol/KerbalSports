using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Fishing
{
    public class Fish
    {
        public enum FishType
        {
            Pond,
            Freshwater,
            Coastal,
            Ocean,
            DeepOcean,
            Kraken
        };

        public double weight;
        public float position = 0.5f;
        public float speed = 0.0f;
        public FishType fishType;
        public double difficulty;

        protected double bodyDifficulty;

        private double nextChangeTime = Time.fixedTime;
        private static System.Random rnd = new System.Random();

        private float minSpeed = 0.02f;
        private float maxSpeed = 0.12f;
        private float maxTime = 0.75f;

        public Fish(Vessel vessel, double bodyDifficulty)
        {
            this.bodyDifficulty = bodyDifficulty;

            // Determine the fish type
            fishType = GetFishType(vessel.mainBody, vessel.latitude, vessel.longitude);

            // Determine the fish weight
            double mean = 0.0;
            double stdDev = 0.0;
            switch (fishType)
            {
                case FishType.Pond:
                    mean = 2;
                    stdDev = 0.5;
                    break;
                case FishType.Freshwater:
                    mean = 8;
                    stdDev = 2;
                    break;
                case FishType.Coastal:
                    mean = 15;
                    stdDev = 3;
                    break;
                case FishType.Ocean:
                    mean = 50;
                    stdDev = 13;
                    break;
                case FishType.DeepOcean:
                    mean = 100;
                    stdDev = 20;
                    break;
                case FishType.Kraken:
                    mean = 500;
                    stdDev = 100;
                    break;
            }

            double gravityModifier = vessel.mainBody.gravParameter / (vessel.mainBody.Radius * vessel.mainBody.Radius) / 9.81;
            weight = rnd.NextGaussianDouble(mean, stdDev) * gravityModifier;
            weight = Mathf.Clamp((float)weight, fishType == FishType.Pond ? 0.1f : 1.0f, float.MaxValue);

            // Determine the fish difficulty modifier
            difficulty = Mathf.Clamp((float)(Math.Log(weight, 16) + 0.5), 0.5f, float.MaxValue);

            Debug.Log("Generated a fish of type " + fishType + ", weight " + weight.ToString("N1") + " kg, and difficulty " + difficulty + ".");

            // Do an immediate fixed update call to set the inital speed/direction
            FixedUpdate();
        }

        public void FixedUpdate()
        {
            // If we would overtake on the next delta, do a change
            if (nextChangeTime + Time.fixedDeltaTime < Time.fixedTime)
            {
                // Pick a random point on the line (0, 1)
                float x = (float)rnd.NextDouble();

                // Pick a random speed
                speed = (float)((rnd.NextDouble() * (maxSpeed - minSpeed) + minSpeed) * Math.Sign(x - position) * (Math.Log(difficulty * bodyDifficulty, 2) + 1));

                // Calculate next change time
                nextChangeTime = Math.Min((x - position) / speed, maxTime) + Time.fixedTime;
            }
            else
            {
                position += speed * Time.fixedDeltaTime;
            }
        }

        public static FishType GetFishType(CelestialBody body, double latitude, double longitude)
        {
            if (body.isHomeWorld && Util.adminArea.Contains(new Vector2((float)latitude, (float)longitude)))
            {
                return FishType.Pond;
            }

            string biome = body.isHomeWorld ? ScienceUtil.GetExperimentBiome(body, latitude, longitude) : "Water";
            if (biome != "Water" && biome != "Shores")
            {
                return FishType.Freshwater;
            }
            else
            {
                // Calculate the terrain height
                double height = Util.TerrainHeight(body, latitude, longitude);

                if (height > -150)
                {
                    return FishType.Coastal;
                }
                else if (height > -1000)
                {
                    return FishType.Ocean;
                }
                else if (height > -2500)
                {
                    return FishType.DeepOcean;
                }
                else
                {
                    return FishType.Kraken;
                }
            }
        }
    }
}
