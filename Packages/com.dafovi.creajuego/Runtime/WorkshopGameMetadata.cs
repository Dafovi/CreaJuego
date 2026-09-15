using UnityEngine;

namespace CreaJuego
{
    [DisallowMultipleComponent]
    public sealed class WorkshopGameMetadata : MonoBehaviour
    {
        [SerializeField] private string gameName;
        [SerializeField] private string teamName;

        public string GameName => gameName;
        public string TeamName => teamName;

        public void Configure(string newGameName, string newTeamName)
        {
            gameName = newGameName?.Trim() ?? string.Empty;
            teamName = newTeamName?.Trim() ?? string.Empty;
        }

        public string DisplayName => string.IsNullOrWhiteSpace(teamName) ? gameName : gameName + " · " + teamName;
    }
}
