using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Fishing
{
    public class Fish
    {
        public double weight = 35.0;
        public float position = 0.5f;
        public float speed = 0.0f;

        private double nextChangeTime = Time.fixedTime;
        private static System.Random rnd = new System.Random();

        private float minSpeed = 0.025f;
        private float maxSpeed = 0.15f;
        private float maxTime = 0.75f;

        public Fish(CelestialBody body)
        {
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
                speed = ((float)rnd.NextDouble() * (maxSpeed - minSpeed) + minSpeed) * Math.Sign(x - position);

                // Calculate next change time
                nextChangeTime = Math.Min((x - position) / speed, maxTime) + Time.fixedTime;
            }
            else
            {
                position += speed * Time.fixedDeltaTime;
            }
        }
    }
}
