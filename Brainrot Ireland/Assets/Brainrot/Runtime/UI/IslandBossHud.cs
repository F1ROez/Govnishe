using UnityEngine;
using UnityEngine.UI;
namespace Brainrot
{
    /// <summary>Live encounter presentation. Reads only the current scene-owned boss; never owns combat state.</summary>
    public sealed class IslandBossHud:MonoBehaviour
    {
        Text title,phase,health;Image fill,echo;EnemyController shown;float value=1,trail=1;int hp=-1,max=-1;bool second;
        public static IslandBossHud Build(Transform parent){
            var outer=ReferenceScreens.Surface(parent,"Здоровье босса",.30f,.087f,.40f,.078f,new Color(.075f,.27f,.30f,.97f));
            var hud=outer.gameObject.AddComponent<IslandBossHud>();
            hud.title=ReferenceScreens.Text(outer.transform,"",16,.035f,.045f,.72f,.33f);hud.title.alignment=TextAnchor.MiddleLeft;
            hud.phase=ReferenceScreens.Text(outer.transform,"I ФАЗА",11,.77f,.065f,.19f,.30f);hud.phase.color=new Color(1,.83f,.43f);
            var track=ReferenceScreens.Surface(outer.transform,"Шкала HP босса",.035f,.45f,.93f,.29f,new Color(.025f,.10f,.13f));
            hud.echo=ReferenceScreens.Surface(track.transform,"След урона",0,0,1,1,new Color(1,.87f,.55f)).GetComponent<Image>();
            hud.fill=ReferenceScreens.Surface(track.transform,"HP босса",0,0,1,1,new Color(1,.39f,.20f)).GetComponent<Image>();
            foreach(var image in new[]{hud.fill,hud.echo}){image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.fillAmount=1;}
            hud.health=ReferenceScreens.Text(outer.transform,"",10,.26f,.76f,.48f,.22f);outer.gameObject.SetActive(false);return hud;
        }
        public void Present(EnemyController current){
            bool active=current!=null&&current.Alive&&current.IsBoss&&current.BossEncounter!=null&&current.BossEncounter.IsActive;
            if(gameObject.activeSelf!=active)gameObject.SetActive(active);if(!active){shown=null;return;}
            var encounter=current.BossEncounter;float target=Mathf.Clamp01(current.Health/Mathf.Max(1,current.MaxHealth));
            if(shown!=current){shown=current;value=trail=target;hp=max=-1;title.text=encounter.Title;second=!encounter.PhaseTwo;}
            value=Mathf.MoveTowards(value,target,Time.unscaledDeltaTime*2.7f);trail=Mathf.MoveTowards(trail,value,Time.unscaledDeltaTime*.45f);
            fill.fillAmount=value;echo.fillAmount=trail;
            if(second!=encounter.PhaseTwo){second=encounter.PhaseTwo;phase.text=second?"II ФАЗА":"I ФАЗА";fill.color=second?new Color(1,.27f,.19f):new Color(1,.48f,.21f);}
            int a=Mathf.CeilToInt(current.Health),b=Mathf.CeilToInt(current.MaxHealth);if(a!=hp||b!=max){hp=a;max=b;health.text=a+" / "+b;}
        }
    }
    /// <summary>Two clean tapered arcs beneath the illustrated thrown bat, rather than a text glyph.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IslandBoomerangSwoosh:Graphic
    {
        void Add(VertexHelper mesh,Vector2 p,Rect r){mesh.AddVert(new Vector3(r.x+p.x*r.width,r.y+p.y*r.height),color,Vector2.zero);}
        protected override void OnPopulateMesh(VertexHelper mesh){
            mesh.Clear();var r=rectTransform.rect;const int steps=18;
            for(int i=0;i<steps;i++){
                float t=i/(float)steps,u=(i+1)/(float)steps;
                for(int band=0;band<2;band++){
                    float a=Mathf.Lerp(150,285,t)*Mathf.Deg2Rad,b=Mathf.Lerp(150,285,u)*Mathf.Deg2Rad;
                    float radius=.42f-band*.08f,width=.013f*(1-t)+.003f;
                    Vector2 center=new Vector2(.66f,.71f);
                    Vector2 p0=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius-width),p1=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius+width);
                    Vector2 p2=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(radius+width),p3=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(radius-width);
                    int start=mesh.currentVertCount;Add(mesh,p0,r);Add(mesh,p1,r);Add(mesh,p2,r);Add(mesh,p3,r);
                    mesh.AddTriangle(start,start+2,start+1);mesh.AddTriangle(start,start+3,start+2);
                }
            }
        }
    }
}
