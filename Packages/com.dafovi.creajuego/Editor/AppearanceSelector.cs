using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace CreaJuego.Editor
{
    public sealed class AppearanceSelector : VisualElement
    {
        public AppearanceSelector(GameItem[] items)
        {
            name="apariencias"; AddToClassList("property-group");
            Add(new Label("APARIENCIA"));
            var current=new Label(); Add(current);
            void Refresh() => current.text=items.Any(i=>i.appearance!=items[0].appearance || i.customSprite!=items[0].customSprite) ? "Varias apariencias" : items[0].customSprite!=null ? "Imagen personalizada" : items[0].appearance!=null ? items[0].appearance.displayName : "Imagen original";
            Refresh();
            var custom=new ObjectField("Usar otra imagen…") { objectType=typeof(Sprite),allowSceneObjects=false,value=items[0].customSprite };
            var pack=items[0].definition.appearancePack;
            if(pack!=null)
            {
                                var search=new ToolbarSearchField { name="buscar-apariencia",tooltip="Buscar apariencia" }; Add(search);
                var scroll=new ScrollView { name="lista-apariencias" }; scroll.style.maxHeight=260; Add(scroll);
                var cards=new VisualElement(); cards.style.flexDirection=FlexDirection.Row; cards.style.flexWrap=Wrap.Wrap; scroll.Add(cards);
                search.RegisterValueChangedCallback(e=>cards.Children().ToList().ForEach(card=>card.style.display=card.tooltip.IndexOf(e.newValue??"",System.StringComparison.CurrentCultureIgnoreCase)>=0 ? DisplayStyle.Flex : DisplayStyle.None));
                foreach(var appearance in pack.appearances.Where(a=>a!=null && a.kind==items[0].definition.kind))
                {
                    var button=new Button(()=> { ItemAppearance.Choose(items,appearance,null); custom.SetValueWithoutNotify(null); custom.showMixedValue=false; Refresh(); }) { tooltip=appearance.displayName };
                    button.style.width=100;
                    button.Add(new Image { sprite=appearance.sprite, scaleMode=ScaleMode.ScaleToFit, style={height=52} });
                    button.Add(new Label(appearance.displayName) { style={whiteSpace=WhiteSpace.Normal} }); cards.Add(button);
                }
            }

            custom.showMixedValue=items.Any(i=>i.customSprite!=items[0].customSprite);
            custom.RegisterValueChangedCallback(e=> { ItemAppearance.Choose(items,items[0].appearance,e.newValue as Sprite); Refresh(); }); Add(custom);
            Add(new Label("La imagen cambia. Las reglas y la superficie se conservan.") { style={whiteSpace=WhiteSpace.Normal} });
        }
    }
}



