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
            void Refresh()=>current.text=items.Any(i=>i.SelectedAppearance!=items[0].SelectedAppearance || i.customSprite!=items[0].customSprite) ? "Varias apariencias" : items[0].customSprite!=null ? "Imagen personalizada" : items[0].SelectedAppearance?.displayName ?? "Imagen original";
            Refresh();
            var custom=new ObjectField("Usar otra imagen…"){objectType=typeof(Sprite),allowSceneObjects=false,value=items[0].customSprite};
            var pack=items[0].definition.appearancePack;
            if(pack!=null)
            {
                var search=new ToolbarSearchField{name="buscar-apariencia",tooltip="Buscar apariencia"}; Add(search);
                var scroll=new ScrollView{name="lista-apariencias"}; scroll.style.maxHeight=260; Add(scroll);
                var cards=new VisualElement(); cards.style.flexDirection=FlexDirection.Row; cards.style.flexWrap=Wrap.Wrap; scroll.Add(cards);
                search.RegisterValueChangedCallback(e=>cards.Children().ToList().ForEach(card=>card.style.display=card.tooltip.IndexOf(e.newValue??"",System.StringComparison.CurrentCultureIgnoreCase)>=0 ? DisplayStyle.Flex : DisplayStyle.None));
                void Card(IAppearanceData option,System.Action choose) {
                    var button=new Button(()=>{choose();custom.SetValueWithoutNotify(null);custom.showMixedValue=false;Refresh();}){tooltip=option.displayName??"Sin nombre"};
                    button.style.width=100;
                    button.Add(new Image{sprite=option.sprite,scaleMode=ScaleMode.ScaleToFit,style={height=52}});
                    button.Add(new Label(option.displayName){style={whiteSpace=WhiteSpace.Normal}});
                    button.SetEnabled(option.sprite!=null);
                    cards.Add(button);
                }
                var category=pack.CategoryFor(items[0].definition.kind);
                if(category!=null) {
                    foreach(var option in category.options.Where(a=>a!=null))
                        Card(option,()=>ItemAppearance.ChooseOption(items,category,option.id));
                    if(category.options.Count==0) cards.Add(new Label("Esta categoría todavía no tiene opciones."));
                } else {
                    // Old packs remain readable. An explicitly empty new list stays empty.
                    foreach(var appearance in pack.appearances.Where(a=>a!=null && a.kind==items[0].definition.kind))
                        Card(appearance,()=>ItemAppearance.Choose(items,appearance,null));
                }
            }
            custom.showMixedValue=items.Any(i=>i.customSprite!=items[0].customSprite);
            custom.RegisterValueChangedCallback(e=>{ItemAppearance.SetCustomSprite(items,e.newValue as Sprite);Refresh();}); Add(custom);
            if(items.Any(i=>i.appearanceCategory!=null && i.SelectedAppearance==null))
                Add(new HelpBox("La opción elegida ya no está en la lista. Elige otra apariencia.",HelpBoxMessageType.Warning));
            Add(new Label("La imagen cambia. Las reglas y la superficie se conservan."){style={whiteSpace=WhiteSpace.Normal}});
        }
    }
}
