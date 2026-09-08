using UnityEngine;

namespace CreaJuego.PlaygroundBackend
{
    public sealed class ReachGoalAction : Playground.BaseClasses.Action
    {
        public override bool ExecuteAction(GameObject other)
        {
            var session = DemoSession.InScene(gameObject.scene);
            if (session == null || session.State != GameSessionState.Playing) return false;
            session.Complete(GetComponent<GameItem>().message);
            return true;
        }
    }
}
