using System;
using System.Collections.Generic;
using UnityEngine;

namespace CreaJuego.Web
{
    public enum RuntimeLevelSize { Small, Medium, Large }

    [Serializable]
    public sealed class RuntimeLevelBounds
    {
        public float left=-25, right=25, bottom=-12, top=15;
        public bool IsValid => IsFinite(left) && IsFinite(right) && IsFinite(bottom) && IsFinite(top) && right-left>=4 && top-bottom>=4;
        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static RuntimeLevelBounds For(RuntimeLevelSize size)
        {
            if(size==RuntimeLevelSize.Small)return new RuntimeLevelBounds{left=-12,right=12,bottom=-8,top=10};
            if(size==RuntimeLevelSize.Large)return new RuntimeLevelBounds{left=-50,right=50,bottom=-16,top=25};
            return new RuntimeLevelBounds();
        }
    }

    [Serializable]
    public sealed class CreaJuegoProjectData
    {
        public int version=1; // Retained for v1 JSON compatibility.
        public int schemaVersion=3;
        public string projectName="Mi juego", teamName="Mi equipo";
        public bool alignAutomatically=true;
        public RuntimeLevelSize levelSize=RuntimeLevelSize.Medium;
        public RuntimeLevelBounds bounds=new RuntimeLevelBounds();
        public List<RuntimeItemData> objects=new List<RuntimeItemData>();
    }

    [Serializable]
    public sealed class RuntimeItemData
    {
        public string instanceId, definitionId;
        public Vector3 position, scale=Vector3.one;
        public float speed=2,jump=10,distance=3,visualScale=1,platformWidth=3;
        public int health=3,damage=1,points=1,attackDamage=1;
        public bool canJump=true,canAttack=true,disappear=true;
        public string message="¡Llegaste a la meta!";
        public Color tint=Color.white;
        public string appearanceId, customImageBase64;
        public bool appearanceChosen;
        public RuntimeItemData Clone()=>JsonUtility.FromJson<RuntimeItemData>(JsonUtility.ToJson(this));
        public static RuntimeItemData From(GameItem item,string definitionId=null)
        {
            if(item==null)throw new ArgumentNullException(nameof(item));
            var box=item.GetComponent<BoxCollider2D>();
            return new RuntimeItemData{instanceId=Guid.NewGuid().ToString("N"),definitionId=definitionId??item.definition?.id??"",
                position=item.transform.position,scale=item.transform.localScale,speed=item.speed,jump=item.jump,distance=item.distance,
                visualScale=item.visualScale,platformWidth=box!=null?box.size.x:3,health=item.health,damage=item.damage,points=item.points,
                attackDamage=item.attackDamage,canJump=item.canJump,canAttack=item.canAttack,disappear=item.disappear,message=item.message,
                tint=item.tint,appearanceId=item.appearanceId};
        }
        public void Apply(GameItem item)
        {
            item.speed=speed;item.jump=jump;item.distance=distance;item.visualScale=visualScale;item.health=health;item.damage=damage;
            item.points=points;item.attackDamage=attackDamage;item.canJump=canJump;item.canAttack=canAttack;item.disappear=disappear;
            item.message=message;item.tint=tint;item.appearanceId=appearanceId??"";item.transform.position=position;item.transform.localScale=scale;
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
            if(data.schemaVersion>3)throw new FormatException("Este proyecto necesita una versión más reciente de CreaJuego.");
            bool migrateFormerDefaults=!json.Contains("\"schemaVersion\"")||data.schemaVersion<3;
            if(!json.Contains("\"schemaVersion\"")){data.alignAutomatically=true;data.levelSize=RuntimeLevelSize.Medium;data.bounds=RuntimeLevelBounds.For(data.levelSize);}
            if(data.bounds==null)data.bounds=RuntimeLevelBounds.For(data.levelSize);
            if(data.objects==null)data.objects=new List<RuntimeItemData>();
            foreach(var item in data.objects){if(item.platformWidth<=0)item.platformWidth=3;if(item.scale==Vector3.zero)item.scale=Vector3.one;if(migrateFormerDefaults&&(item.definitionId=="jugador"&&item.appearanceId=="tiny-dungeon-84"||item.definitionId=="enemigo"&&item.appearanceId=="tiny-dungeon-120"))item.appearanceChosen=false;}
            data.schemaVersion=3;
            return data;
        }
    }
}