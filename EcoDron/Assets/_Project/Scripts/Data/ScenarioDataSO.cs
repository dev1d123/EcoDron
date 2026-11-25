using System.Collections.Generic;
using UnityEngine;

namespace EcoDron.Data
{
    /// <summary>
    /// Configuration data for a campaign scenario containing ordered levels.
    /// </summary>
    [CreateAssetMenu(fileName = "Scenario_0X", menuName = "EcoDron/Campaign/Scenario Data")]
    public class ScenarioDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string scenarioId = "scenario_01";
        public int scenarioNumber = 1;
        public string headerTitle = "ESCENARIO 1: EL COLEGIO";
        public string shortName = "El Colegio";
        public bool unlockedByDefault;

        [Header("Visuals")]
        public Sprite cardThumbnail;
        public Sprite routeBackground;

        [Header("Levels")]
        public List<LevelDataSO> levels = new List<LevelDataSO>();
    }
}
