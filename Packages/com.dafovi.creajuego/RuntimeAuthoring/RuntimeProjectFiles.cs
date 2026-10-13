using System.Runtime.InteropServices;

namespace CreaJuego.Web
{
    public static class RuntimeProjectFiles
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CreaJuegoDownloadProject(string filename,string json);
        [DllImport("__Internal")] static extern void CreaJuegoPickProject(string receiver);
        [DllImport("__Internal")] static extern void CreaJuegoSyncStorage();
#endif
        public static bool Download(string filename,string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CreaJuegoDownloadProject(filename,json);return true;
#else
            return false;
#endif
        }
        public static bool Pick(string receiver)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CreaJuegoPickProject(receiver);return true;
#else
            return false;
#endif
        }
        public static void Flush()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CreaJuegoSyncStorage();
#endif
        }
    }
}
