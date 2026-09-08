using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
namespace CreaJuego.Editor
{
    // Presentation only; SceneService retains every existing rule for entering Play.
    public static class WorkshopPresentation
    {
        public sealed class Check { public string text; public bool passed, optional; }
        public sealed class Preflight {
            public bool ready; public string title,help;
            public List<Check> checks=new List<Check>();
        }
        public static Preflight Describe(IReadOnlyList<SceneCheck> source)
        {
            var result=new Preflight {ready=source.All(c=>c.passed),
                title=source.All(c=>c.passed)?"✓ ¡Todo listo para jugar!":"TU JUEGO NECESITA ALGO",
                help="Tu juego tiene los elementos necesarios."};
            var active=SceneObjects.All<GameItem>(SceneManager.GetActiveScene())
                .Where(i=>i.enabled && i.gameObject.activeInHierarchy && i.definition!=null).ToArray();
            bool Passed(string label)=>source.Any(c=>c.label==label && c.passed);
            result.checks.Add(new Check {text="Jugador",passed=Passed("Personaje")});
            result.checks.Add(new Check {text="Plataforma",passed=active.Any(i=>i.definition.kind==ItemKind.Platform),optional=true});
            result.checks.Add(new Check {text="Algo con qué interactuar",passed=active.Any(i=>i.definition.kind==ItemKind.Prize || i.definition.kind==ItemKind.Hazard || i.definition.kind==ItemKind.Enemy),optional=true});
            result.checks.Add(new Check {text="Meta",passed=Passed("Meta")});
            var missing=source.Where(c=>!c.passed).ToArray();
            var messages=new List<string>();
            foreach(var check in missing) {
                switch(check.label) {
                    case "Personaje": messages.Add("Añade un Jugador. Tu juego necesita un solo personaje activo."); break;
                    case "Meta": messages.Add("Agrega una Meta para indicar dónde termina tu juego."); break;
                    case "Una escena de taller": messages.Add("Abre un solo juego para trabajar en el taller."); break;
                    case "Elementos completos": messages.Add("Hay un elemento incompleto. Vuelve a añadirlo o pide ayuda a quien guía el taller."); break;
                    default:
                        const string preparation="Pulsa Preparar escena. Si el aviso continúa, pide ayuda a quien guía el taller.";
                        if(!messages.Contains(preparation)) messages.Add(preparation);
                        break;
                }
            }
            if(missing.Any(c=>c.label!="Personaje" && c.label!="Meta")) result.checks.Add(new Check {text="Preparación del juego",passed=false});
            if(messages.Count>0) result.help=string.Join(" ",messages);
            return result;
        }
    }
}
