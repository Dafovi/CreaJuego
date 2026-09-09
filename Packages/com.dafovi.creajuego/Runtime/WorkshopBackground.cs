using UnityEngine;
namespace CreaJuego
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(10000)]
    public sealed class WorkshopBackground : MonoBehaviour
    {
        public Camera output;
        public void Fit()
        {
            var renderer=GetComponent<SpriteRenderer>();
            if(output==null || !output.orthographic || renderer.sprite==null) return;
            var size=renderer.sprite.bounds.size;
            if(size.x<=0 || size.y<=0) return;
            float scale=Mathf.Max(output.orthographicSize*2*output.aspect/size.x,output.orthographicSize*2/size.y);
            transform.localScale=Vector3.one*scale;
            transform.position=new Vector3(output.transform.position.x,output.transform.position.y,10)-renderer.sprite.bounds.center*scale;
            renderer.sortingOrder=-30000;
        }
        private void LateUpdate()=>Fit();
    }
}
