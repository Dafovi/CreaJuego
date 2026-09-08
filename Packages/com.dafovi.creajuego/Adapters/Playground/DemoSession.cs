using Playground.UserInterface;
using Playground.Movement;
using UnityEngine;
using UnityEngine.UI;

namespace CreaJuego.PlaygroundBackend
{
    // Minimal bridge to the real Playground score/health UI. No global singleton.
    public sealed class DemoSession : MonoBehaviour
    {
        public UIScript playgroundUI;
        public Text status;
        public bool Completed { get; private set; }
        public void Complete(string message)
        {
            if (Completed || playgroundUI.gameOverPanel.activeSelf) return;
            Completed = true;
            status.text = message;
            foreach (var movement in FindObjectsByType<Move>()) movement.enabled = false;
            foreach (var jump in FindObjectsByType<Jump>()) jump.enabled = false;
        }
    }
}
