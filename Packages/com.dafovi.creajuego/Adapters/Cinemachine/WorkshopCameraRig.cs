using System.Linq;
using UnityEngine;
using Unity.Cinemachine;
namespace CreaJuego
{
    [DisallowMultipleComponent]
    public sealed class WorkshopCameraRig : MonoBehaviour
    {
        public CinemachineCamera cameraController;
        public Camera output;
        public void ResolvePlayer()
        {
            if(cameraController==null) return;
            var player=SceneObjects.All<GameItem>(gameObject.scene).FirstOrDefault(i=>
                i.isActiveAndEnabled && i.definition!=null && i.definition.kind==ItemKind.Player);
            cameraController.Follow=player!=null ? player.transform : null;
        }
        private void Start()=>ResolvePlayer();
    }
}
