using System;
using System.Linq;
using Playground.UserInterface;
using Playground.Movement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreaJuego.PlaygroundBackend
{
    [DefaultExecutionOrder(-300)]
    public sealed class DemoSession : MonoBehaviour, IWorkshopSession
    {
        public UIScript playgroundUI;
        public Text status;
        public GameSessionState State { get; private set; } = GameSessionState.Playing;
        public bool Completed => State == GameSessionState.Won;
        public event System.Action<GameSessionState> ResultChanged;
        public static DemoSession InScene(Scene scene) => SceneObjects.All<DemoSession>(scene).FirstOrDefault(s => s.isActiveAndEnabled);
        private void Awake()
        {
            State = GameSessionState.Playing;
            if (playgroundUI == null) return;
            // The vendor remains the score/health presenter; only this session decides the result.
            playgroundUI.gameType = UIScript.GameType.Endless;
            playgroundUI.gameOverPanel?.SetActive(false); playgroundUI.winPanel?.SetActive(false);
            playgroundUI.statsPanel?.SetActive(true);
            if (playgroundUI.numberLabels != null && playgroundUI.numberLabels.Length > 1 && playgroundUI.numberLabels[1] != null)
                playgroundUI.numberLabels[1].text = "0";
        }
        public string ConfigurationError()
        {
            if (!isActiveAndEnabled || playgroundUI == null || !playgroundUI.isActiveAndEnabled || status == null)
                return "Prepara la sesión y sus indicaciones.";
            if (playgroundUI.gameObject.scene != gameObject.scene || status.gameObject.scene != gameObject.scene)
                return "El marcador debe pertenecer a esta escena.";
            if (SceneObjects.All<UIScript>(gameObject.scene).Length != 1 || playgroundUI.numberOfPlayers != UIScript.Players.OnePlayer)
                return "Este taller necesita un único marcador para un personaje.";
            if (playgroundUI.numberLabels == null || playgroundUI.numberLabels.Length != 2 ||
                playgroundUI.numberLabels.Any(t => t == null || t.gameObject.scene != gameObject.scene) ||
                playgroundUI.statsPanel == null || playgroundUI.gameOverPanel == null ||
                playgroundUI.winPanel == null || playgroundUI.winLabel == null)
                return "Faltan partes del marcador de puntos y puntos de vida.";
            if (playgroundUI.GetComponent<Canvas>() == null) return "Falta la presentación del marcador.";
            return null;
        }
        public void ObserveHealth(int remaining)
        {
            if (remaining <= 0) Finish(GameSessionState.Lost, "Sin puntos de vida. Detén y vuelve a jugar.");
        }
        public void Complete(string message) => Finish(GameSessionState.Won, message);
        private void Finish(GameSessionState result, string message)
        {
            if (State != GameSessionState.Playing) return;
            State = result;
            if (status != null) status.text = message;
            if (playgroundUI != null)
            {
                playgroundUI.gameOverPanel?.SetActive(result == GameSessionState.Lost);
                playgroundUI.winPanel?.SetActive(result == GameSessionState.Won);
                if (result == GameSessionState.Won && playgroundUI.winLabel != null) playgroundUI.winLabel.text = message;
            }
            foreach (var item in SceneObjects.All<GameItem>(gameObject.scene))
            {
                if (item.TryGetComponent<Move>(out var move)) move.enabled = false;
                if (item.TryGetComponent<Jump>(out var jump)) jump.enabled = false;
                if (item.TryGetComponent<Patrol>(out var patrol)) patrol.enabled = false;
                if (item.TryGetComponent<Rigidbody2D>(out var body))
                {
                    body.linearVelocity = Vector2.zero; body.angularVelocity = 0; body.simulated = false;
                }
                foreach (var collider in item.GetComponents<Collider2D>()) collider.enabled = false;
            }
            ResultChanged?.Invoke(State);
        }
    }
}

