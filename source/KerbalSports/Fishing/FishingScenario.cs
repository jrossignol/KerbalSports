using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fishing
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class FishingScenario : ScenarioModule
    {
        public static FishingScenario Instance;

        public override void OnAwake()
        {
            Instance = this;
        }

        [KSPField(isPersistant=true)]
        public int failedAttempts;

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }
    }
}
