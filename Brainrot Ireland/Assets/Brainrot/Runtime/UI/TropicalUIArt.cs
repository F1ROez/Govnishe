using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Brainrot
{
    /// <summary>Loads transparent, text-free tropical illustrations; all labels and gauges remain live UI.</summary>
    public static class TropicalUIArt
    {
        [Serializable] sealed class Layout {public Entry[] sprites;}
        [Serializable] sealed class Entry {public string name;public float x,y,width,height,border;}
        static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        static bool loaded;
        static Sprite wood,blue;
        public static Sprite Get(string name){
            if(!loaded){loaded=true;var texture=Resources.Load<Texture2D>("TropicalUIAtlas");var json=Resources.Load<TextAsset>("TropicalUIAtlasLayout");
                if(texture!=null&&json!=null){try{var layout=JsonUtility.FromJson<Layout>(json.text);if(layout!=null&&layout.sprites!=null)foreach(var entry in layout.sprites){if(entry.width<=0||entry.height<=0)continue;var rect=new Rect(entry.x,texture.height-entry.y-entry.height,entry.width,entry.height);if(rect.xMin<0||rect.yMin<0||rect.xMax>texture.width||rect.yMax>texture.height)continue;var border=entry.name=="LogoPlaque"?new Vector4(75,35,40,68):Vector4.one*entry.border;var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);sprite.name=entry.name;sprites[entry.name]=sprite;}}catch(Exception e){Debug.LogWarning("Не удалось прочитать оформление UI: "+e.Message);}}
            }
            if(sprites.TryGetValue(name,out var found))return found;
            if(name=="HeavyIcon")return CartoonUI.Heavy;if(name=="DashIcon")return CartoonUI.Dash;
            bool timber=name.Contains("Wood")||name=="WavePlaque"||name=="LogoPlaque"||name=="ControlsPanel"||name=="StatsStrip";
            if(timber)return wood!=null?wood:wood=Fallback(true);
            return blue!=null?blue:blue=Fallback(false);
        }
        public static Image Place(string name,Transform parent,Vector2 min,Vector2 max,string key,bool preserve=false){
            var image=CartoonUI.Image(name,parent,min,max,Color.white,Get(key));image.preserveAspect=preserve;
            if(image.sprite.border.sqrMagnitude>.01f&&!preserve)image.type=Image.Type.Sliced;
            return image;
        }
        static Sprite Fallback(bool wood){
            const int w=256,h=128;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);texture.name=wood?"Резная деревянная рамка":"Морская эмаль";texture.wrapMode=TextureWrapMode.Clamp;var colors=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++){
                float edge=Mathf.Min(Mathf.Min(x,w-1-x),Mathf.Min(y,h-1-y));float t=y/(h-1f);Color c;
                if(wood){float grain=Mathf.Sin(x*.08f+Mathf.Sin(y*.11f)*2)*.025f+Mathf.Sin(y*.61f+x*.006f)*.022f;c=Color.Lerp(new Color(.42f,.20f,.07f),new Color(.87f,.61f,.25f),t)+new Color(grain,grain*.7f,0,0);}
                else c=Color.Lerp(new Color(.035f,.28f,.56f),new Color(.18f,.66f,.85f),t);
                if(edge<3)c=new Color(.20f,.12f,.06f);else if(edge<7)c=wood?new Color(.99f,.77f,.36f):new Color(.55f,.91f,1);else if(edge<11)c=wood?new Color(.54f,.27f,.07f):new Color(.055f,.31f,.58f);
                float dx=Mathf.Max(0,Mathf.Abs(x-w*.5f)-(w*.5f-14)),dy=Mathf.Max(0,Mathf.Abs(y-h*.5f)-(h*.5f-14));c.a=Mathf.Clamp01(14-Mathf.Sqrt(dx*dx+dy*dy));colors[y*w+x]=c;
            }
            texture.SetPixels(colors);texture.Apply(false,true);return Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(18,18,18,18));
        }
    }
}
