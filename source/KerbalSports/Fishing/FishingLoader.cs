using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;

namespace Fishing
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class FishingLoader : MonoBehaviour
    {
        void Update()
        {
            if (!PartLoader.Instance.IsReady())
            {
                return;
            }

            // Add the KFP module to each of the kerbal EVA part prefabs
            foreach (AvailablePart part in PartLoader.LoadedPartsList.Where(p => p.name.StartsWith("kerbalEVA")))
            {
                part.partPrefab.gameObject.AddComponent<ModuleFishing>();
            }

            // Done adding part module
            Destroy(this);
        }
    }
}
