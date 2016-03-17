using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;

namespace Fishing
{
    public class ModuleFishing : PartModule
    {
        [KSPField(guiName="Biggest Fish Caught", guiActive=true, guiFormat="N1", guiUnits="kg")]
        double fishRecord = 100;
        ProtoCrewMember pcm;

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("ModuleFishing.OnStart(" + state + ")");
            pcm = part.protoModuleCrew.First();
            Debug.Log("    crew = " + pcm);
        }

        [KSPEvent(active=true, guiActive=true, guiName="Start Fishing", name="fish")]
        void StartFishing()
        {
            FishingDriver fishingDriver = PlanetariumCamera.Camera.gameObject.GetComponent<FishingDriver>();
            if (fishingDriver == null)
            {
                fishingDriver = PlanetariumCamera.Camera.gameObject.AddComponent<FishingDriver>();
            }
            fishingDriver.StartFishing(part.vessel, pcm);
        }
    }
}
