using System;
using System.Collections.Generic;
using UnityEngine;
namespace CreaJuego.Web
{
    [Serializable] public sealed class CreaJuegoProjectData
    {
        public int version=1; public string projectName="Mi juego"; public string teamName="Mi equipo";
        public List<RuntimeItemData> objects=new List<RuntimeItemData>();
    }
    [Serializable] public sealed class RuntimeItemData
    {
        public string instanceId; public string definitionId; public Vector3 position; public Vector3 scale=Vector3.one;
        public float speed=2,jump=10,distance=3,visualScale=1; public int health=3,damage=1,points=1,attackDamage=1;
        public bool canJump=true,canAttack=true,disappear=true; public string message="¡Llegaste a la meta!";
        public Color tint=Color.white; public string appearanceId; public string customImageBase64;
        public RuntimeItemData Clone()=>JsonUtility.FromJson<RuntimeItemData>(JsonUtility.ToJson(this));
        public static RuntimeItemData From(GameItem item)
        {
            return new RuntimeItemData { instanceId=Guid.NewGuid().ToString("N"), definitionId=item.definition.id, position=item.transform.position,
                scale=item.transform.localScale, speed=item.speed,jump=item.jump,distance=item.distance,visualScale=item.visualScale,
                health=item.health,damage=item.damage,points=item.points,attackDamage=item.attackDamage,canJump=item.canJump,
                canAttack=item.canAttack,disappear=item.disappear,message=item.message,tint=item.tint,appearanceId=item.appearanceId };
        }
        public void Apply(GameItem item)
        {
            item.speed=speed; item.jump=jump; item.distance=distance; item.visualScale=visualScale; item.health=health; item.damage=damage;
            item.points=points; item.attackDamage=attackDamage; item.canJump=canJump; item.canAttack=canAttack; item.disappear=disappear;
            item.message=message; item.tint=tint; item.appearanceId=appearanceId??""; item.transform.position=position; item.transform.localScale=scale;
        }
    }
    public interface IProjectStorage { bool Exists {get;} void Save(string json); string Load(); }
    public sealed class FileProjectStorage:IProjectStorage
    {
        readonly string path; public FileProjectStorage(string customPath=null)=>path=customPath??System.IO.Path.Combine(Application.persistentDataPath,"ultimo-proyecto.creajuego");
        public bool Exists=>System.IO.File.Exists(path);
        public void Save(string json){var directory=System.IO.Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(directory))System.IO.Directory.CreateDirectory(directory);System.IO.File.WriteAllText(path,json);}
        public string Load()=>Exists?System.IO.File.ReadAllText(path):null;
    }
    public static class ProjectSerializer
    {
        public static string ToJson(CreaJuegoProjectData data)=>JsonUtility.ToJson(data,true);
        public static CreaJuegoProjectData FromJson(string json)=>string.IsNullOrWhiteSpace(json)?new CreaJuegoProjectData():JsonUtility.FromJson<CreaJuegoProjectData>(json);
    }
}