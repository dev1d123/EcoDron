using System.Collections.Generic;
using UnityEngine;

namespace EcoDron.Data
{
    /// <summary>
    /// Ordered catalog of all campaign scenarios in EcoDron.
    /// </summary>
    [CreateAssetMenu(fileName = "CampaignDatabase", menuName = "EcoDron/Campaign/Campaign Database")]
    public class CampaignDatabaseSO : ScriptableObject
    {
        public List<ScenarioDataSO> scenarios = new List<ScenarioDataSO>();

        public ScenarioDataSO GetScenarioById(string scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId)) return null;
            for (int i = 0; i < scenarios.Count; i++)
            {
                if (scenarios[i] != null && scenarios[i].scenarioId == scenarioId)
                {
                    return scenarios[i];
                }
            }
            return null;
        }
    }
}
