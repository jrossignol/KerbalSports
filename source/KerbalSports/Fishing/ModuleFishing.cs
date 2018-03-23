using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSP.Localization;

namespace KerbalSports.Fishing
{
    public class ModuleFishing : PartModule
    {
        [KSPField(guiName = "#loc.kerbalsports.fishing.fish-type", guiActive=true)]
        string fishType;
        [KSPField(guiName = "#loc.kerbalsports.fishing.biggest-fish", guiActive = true, guiFormat = "N1", guiUnits = "kg")]
        public double fishRecord = 0.0;
        [KSPField(guiName = "#loc.kerbalsports.fishing.number-of-fish", guiActive = true, guiFormat = "N0")]
        public int fishCount = 0;
        ProtoCrewMember pcm;
        bool showing = true;

        FishingData fishingData;

        public override void OnStart(PartModule.StartState state)
        {
            pcm = part.protoModuleCrew.First();
        }

        [KSPEvent(active = true, guiActive = true, guiName = "#loc.kerbalsports.fishing.start-fishing", name = "StartFishing")]
        void StartFishing()
        {
            FishingDriver fishingDriver = PlanetariumCamera.Camera.gameObject.GetComponent<FishingDriver>();
            if (fishingDriver == null)
            {
                fishingDriver = PlanetariumCamera.Camera.gameObject.AddComponent<FishingDriver>();
            }
            fishingDriver.StartFishing(part.vessel, pcm);
        }

        public override void OnUpdate()
        {
            // Need to do this on update, as the part module gets loaded before the scenario
            if (fishingData == null && SportsScenario.Instance != null)
            {
                fishingData = SportsScenario.Instance.GetFishingData(pcm);
                fishRecord = fishingData.BiggestFish(vessel.mainBody);
                fishCount = fishingData.FishCount(vessel.mainBody);
            }

            if (vessel.situation != Vessel.Situations.LANDED || !vessel.mainBody.ocean || vessel.altitude > 250)
            {
                SetShowing(false);
            }
            // Perform the more expensive check
            else
            {
                // Check 5 meters forward for water
                Vector3 checkPosition = vessel.transform.localPosition + vessel.transform.forward * 5.0f;
                double latitude = vessel.mainBody.GetLatitude(checkPosition);
                double longitude = vessel.mainBody.GetLongitude(checkPosition);
                double height = Util.TerrainHeight(vessel.mainBody, latitude, longitude);

                int adminLevel = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.Administration) *
                    ScenarioUpgradeableFacilities.GetFacilityLevelCount(SpaceCenterFacility.Administration)) + 1;

                if (height <= 0.0 || adminLevel == 3 && Util.adminPool.Contains(new Vector2((float)latitude, (float)longitude)))
                {
                    SetShowing(true);

                    // Set the fish type found at the current location
                    Fish.FishType ft = Fish.GetFishType(vessel.mainBody, vessel.latitude, vessel.longitude);
                    fishType = ft.Name();
                }
                else
                {
                    SetShowing(false);
                }
            }
        }

        protected void SetShowing(bool showing)
        {
            if (this.showing != showing)
            {
                this.showing = showing;

                // Show/hide our fields
                Fields["fishType"].guiActive = showing;
                Fields["fishRecord"].guiActive = showing;
                Fields["fishCount"].guiActive = showing;
                Events["StartFishing"].guiActive = showing;
            }
        }
    }
}
