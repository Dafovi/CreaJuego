using System;
using System.Collections.Generic;
using UnityEngine;

namespace CreaJuego.Web
{
    public enum RuntimeLevelSize { Small, Medium, Large, ExtraLarge }

    [Serializable]
    public sealed class RuntimeLevelBounds
    {
        public float left=-50, right=50, bottom=-24, top=30;
        public bool IsValid => IsFinite(left) && IsFinite(right) && IsFinite(bottom) && IsFinite(top) && right-left>=4 && top-bottom>=4;
        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static RuntimeLevelBounds For(RuntimeLevelSize size)
        {
            if(size==RuntimeLevelSize.Small)return new RuntimeLevelBounds{left=-24,right=24,bottom=-16,top=20};
            if(size==RuntimeLevelSize.Large)return new RuntimeLevelBounds{left=-100,right=100,bottom=-32,top=50};
            if(size==RuntimeLevelSize.ExtraLarge)return new RuntimeLevelBounds{left=-240,right=240,bottom=-60,top=90};
            return new RuntimeLevelBounds();
        }
    }

    [Serializable]
    public sealed class CreaJuegoProjectData
    {
        public int version=1; // Retained for v1 JSON compatibility.
        public int schemaVersion=5;
        public string projectName="Mi juego", teamName="Mi equipo";
        public bool alignAutomatically=true;
        public RuntimeLevelSize levelSize=RuntimeLevelSize.Medium;
        public RuntimeLevelBounds bounds=new RuntimeLevelBounds();
        public List<MediaAssetData> mediaAssets=new List<MediaAssetData>();
        public List<RuntimeItemData> objects=new List<RuntimeItemData>();
    }

    public enum MediaAssetSource { File, Camera }

    [Serializable]
    public sealed class MediaAssetData
    {
        public string id;
        public string displayName;
        public string dataUrl;
        public MediaAssetSource source;

        public static MediaAssetData Create(string displayName,string dataUrl,MediaAssetSource source=MediaAssetSource.File)
        {
            return new MediaAssetData{id=Guid.NewGuid().ToString("N"),displayName=string.IsNullOrWhiteSpace(displayName)?"Mi imagen":displayName,dataUrl=dataUrl,source=source};
        }
    }

    [Serializable]
    public sealed class RuntimeItemData
    {
        public string instanceId, definitionId;
        public Vector3 position, scale=Vector3.one;
        public float rotationZ;
        public float speed=2,jump=10,distance=3,visualScale=1,platformWidth=3;
        public int health=3,damage=1,points=1,attackDamage=1;
        public bool canJump=true,canAttack=true,disappear=true;
        public string message="¡Llegaste a la meta!";
        public Color tint=Color.white;
        public string appearanceId, mediaAssetId;
        // Retained only to migrate projects created before the shared media library.
        public string customImageBase64;
        public bool appearanceChosen;
        public RuntimeItemData Clone()=>JsonUtility.FromJson<RuntimeItemData>(JsonUtility.ToJson(this));
        public static RuntimeItemData From(GameItem item,string definitionId=null)
        {
            if(item==null)throw new ArgumentNullException(nameof(item));
            var box=item.GetComponent<BoxCollider2D>();
            return new RuntimeItemData{instanceId=Guid.NewGuid().ToString("N"),definitionId=definitionId??item.definition?.id??"",
                position=item.transform.position,scale=item.transform.localScale,rotationZ=item.transform.eulerAngles.z,speed=item.speed,jump=item.jump,distance=item.distance,
                visualScale=item.visualScale,platformWidth=box!=null?box.size.x:3,health=item.health,damage=item.damage,points=item.points,
                attackDamage=item.attackDamage,canJump=item.canJump,canAttack=item.canAttack,disappear=item.disappear,message=item.message,
                tint=item.tint,appearanceId=item.appearanceId};
        }
        public void Apply(GameItem item)
        {
            item.speed=speed;item.jump=jump;item.distance=distance;item.visualScale=visualScale;item.health=health;item.damage=damage;
            item.points=points;item.attackDamage=attackDamage;item.canJump=canJump;item.canAttack=canAttack;item.disappear=disappear;
            item.message=message;item.tint=tint;item.appearanceId=appearanceId??"";item.transform.position=position;item.transform.localScale=scale;item.transform.rotation=Quaternion.Euler(0,0,rotationZ);
        }
    }

    public interface IProjectStorage { bool Exists {get;} void Save(string json); string Load(); }
    public sealed class FileProjectStorage:IProjectStorage
    {
        readonly string path;
        public FileProjectStorage(string customPath=null)=>path=customPath??System.IO.Path.Combine(Application.persistentDataPath,"ultimo-proyecto.creajuego");
        public bool Exists=>System.IO.File.Exists(path);
        public void Save(string json){var directory=System.IO.Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(directory))System.IO.Directory.CreateDirectory(directory);System.IO.File.WriteAllText(path,json);}
        public string Load()=>Exists?System.IO.File.ReadAllText(path):null;
    }
    public static class ProjectSerializer
    {
        public static string ToJson(CreaJuegoProjectData data)=>JsonUtility.ToJson(data,true);
        public static CreaJuegoProjectData FromJson(string json)
        {
            if(string.IsNullOrWhiteSpace(json))return new CreaJuegoProjectData();
            var data=JsonUtility.FromJson<CreaJuegoProjectData>(json)??new CreaJuegoProjectData();
            if(data.schemaVersion>5)throw new FormatException("Este proyecto necesita una versión más reciente de CreaJuego.");
            bool migrateFormerDefaults=!json.Contains("\"schemaVersion\"")||data.schemaVersion<3;
            if(!json.Contains("\"schemaVersion\"")){data.alignAutomatically=true;data.levelSize=RuntimeLevelSize.Medium;data.bounds=RuntimeLevelBounds.For(data.levelSize);}
            if(data.schemaVersion<4)data.bounds=RuntimeLevelBounds.For(data.levelSize);
            if(data.bounds==null)data.bounds=RuntimeLevelBounds.For(data.levelSize);
            if(data.mediaAssets==null)data.mediaAssets=new List<MediaAssetData>();
            if(data.objects==null)data.objects=new List<RuntimeItemData>();
            int importedNumber=data.mediaAssets.Count+1;
            foreach(var item in data.objects)
            {
                if(item.platformWidth<=0)item.platformWidth=3;if(item.scale==Vector3.zero)item.scale=Vector3.one;if(migrateFormerDefaults&&(item.definitionId=="jugador"&&item.appearanceId=="tiny-dungeon-84"||item.definitionId=="enemigo"&&item.appearanceId=="tiny-dungeon-120"))item.appearanceChosen=false;
                if(string.IsNullOrEmpty(item.mediaAssetId)&&!string.IsNullOrEmpty(item.customImageBase64))
                {
                    var shared=data.mediaAssets.Find(asset=>asset!=null&&asset.dataUrl==item.customImageBase64);
                    if(shared==null){shared=MediaAssetData.Create("Imagen importada "+importedNumber++,item.customImageBase64);data.mediaAssets.Add(shared);}
                    item.mediaAssetId=shared.id;item.customImageBase64=null;
                }
            }
            data.mediaAssets.RemoveAll(asset=>asset==null||string.IsNullOrWhiteSpace(asset.id)||string.IsNullOrWhiteSpace(asset.dataUrl));
            data.schemaVersion=5;
            return data;
        }
    }
}
