using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalSports.Fishing
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class SportsScenario : ScenarioModule
    {
        private Dictionary<string, FishingData> fishingData = new Dictionary<string, FishingData>();

        public static SportsScenario Instance;

        public override void OnAwake()
        {
            Instance = this;
        }

        // Fishing stuff
        public int failedAttempts;
        public bool tutorialDialogShown = false;

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            
            ConfigNode fishingNode = new ConfigNode("FISHING");
            node.AddNode(fishingNode);

            fishingNode.AddValue("failedAttempts", failedAttempts);
            fishingNode.AddValue("tutorialDialogShown", tutorialDialogShown);

            foreach (KeyValuePair<string, FishingData> pair in fishingData)
            {
                ConfigNode kerbalNode = new ConfigNode("KERBAL");
                fishingNode.AddNode(kerbalNode);
                kerbalNode.AddValue("name", pair.Key);
                kerbalNode.AddValue("skill", pair.Value.skill);
                foreach (KeyValuePair<CelestialBody, FishingData.FishingStats> bodyPair in pair.Value.fishingStats)
                {
                    ConfigNode bodyNode = new ConfigNode(bodyPair.Key.name);
                    kerbalNode.AddNode(bodyNode);
                    bodyNode.AddValue("fishCaught", bodyPair.Value.fishCaught);
                    bodyNode.AddValue("biggestFish", bodyPair.Value.biggestFish);
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (node.HasNode("FISHING"))
            {
                ConfigNode fishingNode = node.GetNode("FISHING");
                failedAttempts = Convert.ToInt32(fishingNode.GetValue("failedAttempts"));
                if (fishingNode.HasValue("tutorialDialogShown"))
                {
                    tutorialDialogShown = Convert.ToBoolean(fishingNode.GetValue("tutorialDialogShown"));
                }

                foreach (ConfigNode kerbalNode in fishingNode.GetNodes("KERBAL"))
                {
                    string name = kerbalNode.GetValue("name");
                    FishingData data = fishingData[name] = new FishingData();
                    data.skill = (float)Convert.ToDouble(kerbalNode.GetValue("skill"));
                    foreach (ConfigNode bodyNode in kerbalNode.nodes)
                    {
                        CelestialBody body = FlightGlobals.Bodies.Where(cb => cb.name == bodyNode.name).FirstOrDefault();
                        if (body != null)
                        {
                            FishingData.FishingStats stats = data.fishingStats[body] = new FishingData.FishingStats();
                            stats.biggestFish = Convert.ToDouble(bodyNode.GetValue("biggestFish"));
                            stats.fishCaught = Convert.ToInt32(bodyNode.GetValue("fishCaught"));
                        }
                    }
                }
            }
        }

        public FishingData GetFishingData(ProtoCrewMember pcm)
        {
            if (pcm == null)
            {
                return null;
            }

            if (!fishingData.ContainsKey(pcm.name))
            {
                FishingData data = fishingData[pcm.name] = new FishingData();

                string traitName = pcm.experienceTrait.Config.Name;
                data.skill = traitName == "Engineer" ? 20 : traitName == "Scientist" ? 10 : 0;
            }
            return fishingData[pcm.name];
        }
    }
}
