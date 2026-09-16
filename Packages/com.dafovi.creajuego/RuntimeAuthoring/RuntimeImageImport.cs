using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace CreaJuego.Web
{
    public static class RuntimeImageImport
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CreaJuegoPickImage(string target);
#endif
        public static bool Pick(string receiver)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CreaJuegoPickImage(receiver);return true;
#else
            return false;
#endif
        }
        public static Sprite DecodeDataUrl(string dataUrl,out Texture2D texture)
        {
            texture=null;if(string.IsNullOrWhiteSpace(dataUrl))return null;int comma=dataUrl.IndexOf(',');var payload=comma>=0?dataUrl.Substring(comma+1):dataUrl;
            byte[] bytes;try{bytes=Convert.FromBase64String(payload);}catch{return null;}texture=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!texture.LoadImage(bytes)){UnityEngine.Object.Destroy(texture);texture=null;return null;}
            return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),Mathf.Max(texture.width,texture.height)/2f);
        }
    }
}