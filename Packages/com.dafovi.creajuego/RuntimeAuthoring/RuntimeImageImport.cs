using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CreaJuego.Web
{
    public static class RuntimeImageImport
    {
        public const int MaxBytes=2*1024*1024,MaxDimension=2048;
        public const string TooLargeMessage="Esta imagen es demasiado grande. Elige una imagen más pequeña.";
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CreaJuegoPickImage(string target,int maxBytes,int maxDimension);
#endif
        public static bool Pick(string receiver)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CreaJuegoPickImage(receiver,MaxBytes,MaxDimension);return true;
#else
            return false;
#endif
        }
        public static bool ValidateDataUrl(string dataUrl,out string error)
        {
            error=null;if(string.IsNullOrWhiteSpace(dataUrl)){error="No se pudo leer esta imagen.";return false;}
            bool png=dataUrl.StartsWith("data:image/png;base64,",StringComparison.OrdinalIgnoreCase),jpg=dataUrl.StartsWith("data:image/jpeg;base64,",StringComparison.OrdinalIgnoreCase)||dataUrl.StartsWith("data:image/jpg;base64,",StringComparison.OrdinalIgnoreCase);
            if(!png&&!jpg){error="Elige una imagen PNG o JPG.";return false;}int comma=dataUrl.IndexOf(',');byte[] bytes;try{bytes=Convert.FromBase64String(dataUrl.Substring(comma+1));}catch{error="No se pudo leer esta imagen.";return false;}
            if(bytes.Length>MaxBytes){error=TooLargeMessage;return false;}if(!Dimensions(bytes,png,out var width,out var height)){error="No se pudo leer esta imagen.";return false;}if(width>MaxDimension||height>MaxDimension){error=TooLargeMessage;return false;}return true;
        }
        public static bool TryDecodeDataUrl(string dataUrl,out Sprite sprite,out Texture2D texture,out string error)
        {
            sprite=null;texture=null;if(!ValidateDataUrl(dataUrl,out error))return false;var bytes=Convert.FromBase64String(dataUrl.Substring(dataUrl.IndexOf(',')+1));texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            if(!texture.LoadImage(bytes)){UnityEngine.Object.Destroy(texture);texture=null;error="No se pudo leer esta imagen.";return false;}
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),Mathf.Max(texture.width,texture.height)/2f);return true;
        }
        public static Sprite DecodeDataUrl(string dataUrl,out Texture2D texture){return TryDecodeDataUrl(dataUrl,out var sprite,out texture,out _)?sprite:null;}
        static bool Dimensions(byte[] bytes,bool png,out int width,out int height)
        {
            width=height=0;if(png){if(bytes.Length<24)return false;width=ReadBig(bytes,16);height=ReadBig(bytes,20);return width>0&&height>0;}
            int index=2;while(index+8<bytes.Length){if(bytes[index]!=0xff){index++;continue;}int marker=bytes[index+1];if(marker==0xd8||marker==0xd9){index+=2;continue;}int length=(bytes[index+2]<<8)|bytes[index+3];if(length<2||index+2+length>bytes.Length)return false;if((marker>=0xc0&&marker<=0xc3)||(marker>=0xc5&&marker<=0xc7)||(marker>=0xc9&&marker<=0xcb)||(marker>=0xcd&&marker<=0xcf)){height=(bytes[index+5]<<8)|bytes[index+6];width=(bytes[index+7]<<8)|bytes[index+8];return width>0&&height>0;}index+=2+length;}return false;
        }
        static int ReadBig(byte[] bytes,int index)=>(bytes[index]<<24)|(bytes[index+1]<<16)|(bytes[index+2]<<8)|bytes[index+3];
    }
}