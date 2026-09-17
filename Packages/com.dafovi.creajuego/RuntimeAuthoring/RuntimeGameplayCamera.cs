using UnityEngine;
using Unity.Cinemachine;

namespace CreaJuego.Web
{
    public sealed class RuntimeGameplayCamera:MonoBehaviour
    {
        public CinemachineCamera ActiveCamera{get;private set;}
        public void Activate(Camera output,GameItem player,Transform parent)
        {
            var brain=output.GetComponent<CinemachineBrain>();if(brain==null)brain=output.gameObject.AddComponent<CinemachineBrain>();
            var go=new GameObject("Seguimiento del personaje",typeof(CinemachineCamera),typeof(CinemachinePositionComposer),typeof(WorkshopCameraRig));
            go.transform.SetParent(parent,false);go.transform.position=output.transform.position;
            ActiveCamera=go.GetComponent<CinemachineCamera>();ActiveCamera.Lens=LensSettings.FromCamera(output);ActiveCamera.Follow=player.transform;
            var composer=go.GetComponent<CinemachinePositionComposer>();composer.CameraDistance=10;composer.Damping=new Vector3(.3f,.3f,0);
            var rig=go.GetComponent<WorkshopCameraRig>();rig.output=output;rig.cameraController=ActiveCamera;
            brain.enabled=true;output.enabled=true;
        }
        public void Deactivate(Camera output)
        {
            if(output==null)return;output.enabled=false;var brain=output.GetComponent<CinemachineBrain>();if(brain!=null)brain.enabled=false;ActiveCamera=null;
        }
    }
}