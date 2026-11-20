using UnityEngine;
namespace Brainrot
{
    /// <summary>Shared smooth ceramic/glass surfaces. Generated once, independent of live labels and pointer hitboxes.</summary>
    public static class IslandThemeSurface
    {
        static Sprite round,cream,sea;
        public static Sprite Round=>round!=null?round:round=Create(false,false);
        public static Sprite Cream=>cream!=null?cream:cream=Create(true,false);
        public static Sprite Sea=>sea!=null?sea:sea=Create(false,true);
        static Sprite Create(bool paper,bool glass){
            const int size=192;const float radius=20;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=paper?"Светлая керамика":glass?"Бирюзовое стекло":"Крупное скругление",wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float u=x/(size-1f),v=y/(size-1f);
                float dx=Mathf.Max(0,Mathf.Abs(x-(size-1)*.5f)-((size-1)*.5f-radius));
                float dy=Mathf.Max(0,Mathf.Abs(y-(size-1)*.5f)-((size-1)*.5f-radius));
                float alpha=Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy));
                Color color=Color.white;
                if(paper){color=Color.Lerp(new Color(.94f,.94f,.90f),new Color(.97f,.97f,.94f),v);float bloom=0;color+=new Color(bloom,bloom,bloom,0);}
                if(glass){color=Color.Lerp(new Color(.01f,.34f,.40f),new Color(.005f,.19f,.25f),v);float shine=0;color+=new Color(shine*.3f,shine,shine,0);}
                color.a=alpha;pixels[y*size+x]=color;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(22,22,22,22));
        }
    }
}
