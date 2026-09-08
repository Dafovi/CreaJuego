using UnityEngine;

namespace CreaJuego.PlaygroundBackend
{
    public sealed class ReachGoalAction : Playground.BaseClasses.Action
    {
        public override bool ExecuteAction(GameObject other)
        {
            var session = FindAnyObjectByType<DemoSession>();
            if (session == null) return false;
            session.Complete(GetComponent<GameItem>().message);
            return true;
        }
    }
}
