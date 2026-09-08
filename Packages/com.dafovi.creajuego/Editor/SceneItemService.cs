using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace CreaJuego.Editor
{
    public static class SceneItemService
    {
        public sealed class Entry { public GameItem item; public string label; }
        public static Entry[] Entries()
        {
            var items=SceneObjects.All<GameItem>(SceneManager.GetActiveScene())
                .Where(i=>!EditorUtility.IsPersistent(i)).ToArray();
            string BaseName(GameItem item) {
                string name=item.name.Replace(" (copia)","").Replace(" (Clone)","").Trim();
                return string.IsNullOrEmpty(name) ? item.definition!=null ? item.definition.displayName : "Elemento" : name;
            }
            var names=items.Select(BaseName).ToArray();
            return items.Select((item,index)=>new Entry {item=item, label=names[index]+
                (names.Count(n=>n==names[index])>1 ? " "+names.Take(index+1).Count(n=>n==names[index]) : "")}).ToArray();
        }
        public static string Label(GameItem item) => Entries().FirstOrDefault(e=>e.item==item)?.label ?? item.name;
        public static void Select(GameItem item)
        {
            if(item==null || EditorUtility.IsPersistent(item) || item.gameObject.scene!=SceneManager.GetActiveScene()) return;
            Selection.activeGameObject=item.gameObject;
        }
    }
}
