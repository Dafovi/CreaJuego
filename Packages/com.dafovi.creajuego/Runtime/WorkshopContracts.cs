using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreaJuego
{
    public enum GameSessionState { Playing, Won, Lost }
    public interface IWorkshopSession
    {
        GameSessionState State { get; }
        string ConfigurationError();
    }
    public interface IBackendValidation { string ConfigurationError(); }
    public static class SceneObjects
    {
        public static T[] All<T>(Scene scene) where T : Component =>
            scene.IsValid() && scene.isLoaded ? scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray() : new T[0];
    }
}

