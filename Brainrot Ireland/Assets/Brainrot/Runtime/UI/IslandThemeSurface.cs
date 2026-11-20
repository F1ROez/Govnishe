using UnityEngine;
namespace Brainrot
{
    /// <summary>Shared smooth ceramic/glass surfaces. Generated once, independent of live labels and pointer hitboxes.</summary>
    public static class IslandThemeSurface
    {
        static Sprite round,cream,sea,wood,glow;
        public static Sprite Round=>round!=null?round:round=Create(false,false);
        public static Sprite Cream=>cream!=null?cream:cream=Create(true,false);
        public static Sprite Sea=>sea!=null?sea:sea=Create(false,true);
        public static Sprite Wood=>wood!=null?wood:wood=CreateWood();
        public static Sprite Glow=>glow!=null?glow:glow=CreateGlow();
        static Sprite Create(bool paper,bool glass){
            const int size=192;const float radius=20;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=paper?"Светлая керамика":glass?"Бирюзовое стекло":"Крупное скругление",wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float u=x/(size-1f),v=y/(size-1f);
                float dx=Mathf.Max(0,Mathf.Abs(x-(size-1)*.5f)-((size-1)*.5f-radius));
                float dy=Mathf.Max(0,Mathf.Abs(y-(size-1)*.5f)-((size-1)*.5f-radius));
                float alpha=Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy));
                float edge=Mathf.Abs(Mathf.Sqrt(dx*dx+dy*dy)-radius);
                Color color=Color.white*Mathf.Lerp(.84f,1,v);color.a=1;
                if(edge<1.5f)color=Color.white;
                if(paper){float grain=Mathf.Sin(x*1.31f+y*2.17f)*.004f;color=Color.Lerp(new Color(.88f,.81f,.66f),new Color(1,.97f,.87f),v)+new Color(grain,grain,grain,0);}
                if(glass){color=Color.Lerp(new Color(.015f,.21f,.26f),new Color(.025f,.58f,.62f),v);float shine=Mathf.Pow(Mathf.Clamp01((v-.76f)/.24f),2)*.12f;color+=new Color(shine*.5f,shine,shine,0);}
                color.a=alpha;pixels[y*size+x]=color;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(22,22,22,22));
        }
        static Sprite CreateWood(){
            const int size=192;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Тёплая древесная эмаль",wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float u=x/(size-1f),v=y/(size-1f),grain=Mathf.Sin(y*.22f+Mathf.Sin(x*.032f)*1.5f)*.024f;
                float dx=Mathf.Max(0,Mathf.Abs(x-95.5f)-76),dy=Mathf.Max(0,Mathf.Abs(y-95.5f)-76);
                Color c=Color.Lerp(new Color(.27f,.12f,.055f),new Color(.62f,.32f,.12f),v)+new Color(grain,grain*.55f,grain*.2f,0);
                c.a=Mathf.Clamp01(20-Mathf.Sqrt(dx*dx+dy*dy));pixels[y*size+x]=c;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(23,23,23,23));
        }
        static Sprite CreateGlow(){
            const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Мягкие лучи способности",wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};var colors=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float u=(x-63.5f)/64,v=(y-63.5f)/64,d=Mathf.Sqrt(u*u+v*v),angle=Mathf.Atan2(v,u);
                float rays=.40f+.60f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(angle*9+d*1.5f)),5);
                colors[y*size+x]=new Color(1,1,1,Mathf.Clamp01(1-d)*rays*.65f);
            }
            texture.SetPixels(colors);texture.Apply(false,true);return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);
        }
    }
}
