using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Fishing
{
    public static class Util
    {
        // Admin pool rect (in lat/long)
        public static Rect adminPool = new Rect(-0.0874808776551802f, 285.337706880376f, 0.0013936750742699f, 0.003049928452f);
        public static Rect adminArea = new Rect(adminPool.x - 0.01f, adminPool.y - 0.01f, 0.02f, 0.02f);

        public static void DumpGameObject(GameObject go, string indent = "")
        {
            foreach (Component c in go.GetComponents<Component>())
            {
                Debug.Log(indent + c);
            }

            foreach (Transform c in go.transform)
            {
                DumpGameObject(c.gameObject, indent + "    ");
            }
        }

        public static Transform FindDeepChild(this Transform parent, string name)
        {
            var result = parent.Find(name);
            if (result != null)
                return result;
            foreach (Transform child in parent)
            {
                result = child.FindDeepChild(name);
                if (result != null)
                    return result;
            }
            return null;
        }
        
        public static double NextGaussianDouble(this System.Random r, double mean, double stdDev)
        {
            double u1 = r.NextDouble();
            double u2 = r.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        public static double TerrainHeight(CelestialBody body, double latitude, double longitude)
        {
            // Calculate the terrain height
            double latRads = Math.PI / 180.0 * latitude;
            double lonRads = Math.PI / 180.0 * longitude;
            Vector3d radialVector = new Vector3d(Math.Cos(latRads) * Math.Cos(lonRads), Math.Sin(latRads), Math.Cos(latRads) * Math.Sin(lonRads));
            return body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius;
        }
    }
}
