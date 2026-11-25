using UnityEngine;

namespace EcoDron.Data
{
    /// <summary>
    /// Configuration data for a single playable level inside a scenario.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_X_Y", menuName = "EcoDron/Campaign/Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string levelId = "1.1";
        public int levelNumber = 1;
        public string displayName = "Nuevo Nivel";

        [Header("Gameplay Summary")]
        public int wasteCount = 8;
        public int timeLimitSeconds = 90;
        public string sceneName = "Level_1_1";

        [Header("Visuals")]
        public Sprite thumbnail;
    }
}
