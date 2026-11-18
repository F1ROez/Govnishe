using UnityEngine;
using UnityEngine.UI;

namespace Brainrot
{
    public enum IslandSymbol { Play, Shop, Trophy, Settings, Exit, Camera, Target, Shake, Leaf, Laurel }

    /// <summary>Small, resolution-independent menu pictograms. No font glyphs or input interception.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IslandMenuSymbol:MaskableGraphic
    {
        [SerializeField] IslandSymbol symbol;
        Color paint;
        readonly System.Collections.Generic.List<Vector2> points=new System.Collections.Generic.List<Vector2>(512);
        public override Texture mainTexture=>Texture2D.whiteTexture;
        void Tri(VertexHelper mesh,int a,int b,int c){
            var u=points[b]-points[a];var v=points[c]-points[a];
            // Match UGUI Image's clockwise front face for every generated primitive.
            if(u.x*v.y-u.y*v.x>0)mesh.AddTriangle(a,c,b);else mesh.AddTriangle(a,b,c);
        }
        public IslandSymbol Symbol {get=>symbol;set{if(symbol==value)return;symbol=value;SetVerticesDirty();}}
        public static IslandMenuSymbol Place(Transform parent,IslandSymbol symbol,float x,float y,float w,float h,Color color)
        {
            var go=new GameObject(symbol+" — значок",typeof(RectTransform),typeof(IslandMenuSymbol));
            go.transform.SetParent(parent,false);ReferenceScreens.Rect((RectTransform)go.transform,x,y,w,h);
            var graphic=go.GetComponent<IslandMenuSymbol>();graphic.Symbol=symbol;graphic.color=color;graphic.raycastTarget=false;return graphic;
        }
        Vector2 Point(float x,float y)
        {
            var rect=rectTransform.rect;float size=Mathf.Min(rect.width,rect.height);
            return rect.center+new Vector2(x-.5f,y-.5f)*size;
        }
        int Vertex(VertexHelper mesh,float x,float y)
        {
            int index=mesh.currentVertCount;var point=Point(x,y);points.Add(point);mesh.AddVert(point,paint,new Vector2(.5f,.5f));return index;
        }
        void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            int start=Vertex(mesh,a.x,a.y);Vertex(mesh,b.x,b.y);Vertex(mesh,c.x,c.y);Vertex(mesh,d.x,d.y);
            Tri(mesh,start,start+1,start+2);Tri(mesh,start,start+2,start+3);
        }
        void Disc(VertexHelper mesh,float x,float y,float radius)
        {
            const int steps=24;int center=Vertex(mesh,x,y);
            for(int i=0;i<=steps;i++){float angle=i*Mathf.PI*2/steps;Vertex(mesh,x+Mathf.Cos(angle)*radius,y+Mathf.Sin(angle)*radius);}
            for(int i=0;i<steps;i++)Tri(mesh,center,center+i+1,center+i+2);
        }
        void Stroke(VertexHelper mesh,float ax,float ay,float bx,float by,float width)
        {
            var a=new Vector2(ax,ay);var b=new Vector2(bx,by);var side=new Vector2(-(b-a).y,(b-a).x).normalized*(width*.5f);
            Quad(mesh,a-side,b-side,b+side,a+side);Disc(mesh,ax,ay,width*.5f);Disc(mesh,bx,by,width*.5f);
        }
        void Ring(VertexHelper mesh,float x,float y,float inside,float outside,int teeth=0)
        {
            int steps=teeth>0?teeth*8:48;
            for(int i=0;i<steps;i++){
                float a=i*Mathf.PI*2/steps,b=(i+1)*Mathf.PI*2/steps;
                float ra=teeth>0&&(i%8<2||i%8>5)?outside*.79f:outside;
                int next=(i+1)%steps;float rb=teeth>0&&(next%8<2||next%8>5)?outside*.79f:outside;
                var va=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var vb=new Vector2(Mathf.Cos(b),Mathf.Sin(b));var center=new Vector2(x,y);
                Quad(mesh,center+va*inside,center+va*ra,center+vb*rb,center+vb*inside);
            }
        }
        void RoundedBox(VertexHelper mesh,float x,float y,float width,float height,float radius)
        {
            radius=Mathf.Min(radius,Mathf.Min(width,height)*.5f);int center=Vertex(mesh,x+width*.5f,y+height*.5f);
            const int arc=7;const int count=arc*4;
            for(int i=0;i<=count;i++){
                int corner=(i/arc)%4;float angle=(-90+corner*90+(i%arc)*90f/arc)*Mathf.Deg2Rad;
                float cx=corner<2?x+width-radius:x+radius,cy=corner==0||corner==3?y+radius:y+height-radius;
                Vertex(mesh,cx+Mathf.Cos(angle)*radius,cy+Mathf.Sin(angle)*radius);
            }
            for(int i=0;i<count;i++)Tri(mesh,center,center+i+1,center+i+2);
        }
        void Triangle(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c)
        {
            int index=Vertex(mesh,a.x,a.y);Vertex(mesh,b.x,b.y);Vertex(mesh,c.x,c.y);Tri(mesh,index,index+1,index+2);
        }
        void Camera(VertexHelper mesh)
        {
            RoundedBox(mesh,.13f,.27f,.53f,.46f,.065f);
            Quad(mesh,new Vector2(.66f,.40f),new Vector2(.89f,.27f),new Vector2(.89f,.73f),new Vector2(.66f,.60f));
        }
        void CameraWithLens(VertexHelper mesh)
        {
            const int steps=48;int start=mesh.currentVertCount;
            for(int i=0;i<=steps;i++){
                float angle=i*Mathf.PI*2/steps;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                // Intersect a ray with a rounded box. The lens is a true transparent hole.
                float low=0,high=.32f;
                for(int j=0;j<9;j++){
                    float distance=(low+high)*.5f;var point=direction*distance;
                    var q=new Vector2(Mathf.Abs(point.x)-.17f,Mathf.Abs(point.y)-.14f);
                    float sdf=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-.06f;
                    if(sdf>0)high=distance;else low=distance;
                }
                var outer=new Vector2(.5f,.48f)+direction*((low+high)*.5f);var inner=new Vector2(.5f,.48f)+direction*.105f;
                Vertex(mesh,inner.x,inner.y);Vertex(mesh,outer.x,outer.y);
            }
            for(int i=0;i<steps;i++){
                int index=start+i*2;Tri(mesh,index,index+1,index+3);Tri(mesh,index,index+3,index+2);
            }
            RoundedBox(mesh,.40f,.66f,.17f,.10f,.025f);
        }
        void Laurel(VertexHelper mesh)
        {
            // One curved branch; the matching right branch uses a mirrored RectTransform.
            Vector2 previous=new Vector2(.78f,.12f);
            for(int i=1;i<=24;i++){
                float t=i/24f;var next=new Vector2(.78f-.43f*Mathf.Sin(t*1.85f),.12f+t*.76f);
                Stroke(mesh,previous.x,previous.y,next.x,next.y,.033f);previous=next;
            }
            for(int i=0;i<7;i++){
                float t=.13f+i*.12f;var stem=new Vector2(.78f-.43f*Mathf.Sin(t*1.85f),.12f+t*.76f);
                var direction=new Vector2(-.17f,Mathf.Lerp(.09f,.18f,t));var center=stem+direction*.48f;
                var along=direction*.62f;var across=new Vector2(-direction.y,direction.x).normalized*.06f;
                const int steps=18;int start=Vertex(mesh,center.x,center.y);
                for(int j=0;j<=steps;j++){
                    float angle=j*Mathf.PI*2/steps;var point=center+along*Mathf.Cos(angle)+across*Mathf.Sin(angle);Vertex(mesh,point.x,point.y);
                }
                for(int j=0;j<steps;j++)Tri(mesh,start,start+j+1,start+j+2);
            }
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();points.Clear();paint=color;if(rectTransform.rect.width<=0||rectTransform.rect.height<=0)return;
            switch(symbol){
                case IslandSymbol.Play:
                    paint=new Color(.35f,.13f,.02f,color.a);
                    Triangle(mesh,new Vector2(.30f,.20f),new Vector2(.79f,.50f),new Vector2(.30f,.80f));
                    Stroke(mesh,.30f,.20f,.79f,.50f,.10f);Stroke(mesh,.79f,.50f,.30f,.80f,.10f);Stroke(mesh,.30f,.80f,.30f,.20f,.10f);
                    paint=color;Triangle(mesh,new Vector2(.32f,.23f),new Vector2(.76f,.50f),new Vector2(.32f,.77f));
                    Stroke(mesh,.32f,.23f,.76f,.50f,.04f);Stroke(mesh,.76f,.50f,.32f,.77f,.04f);Stroke(mesh,.32f,.77f,.32f,.23f,.04f);
                    paint=new Color(1,.98f,.64f,color.a);Stroke(mesh,.33f,.31f,.33f,.67f,.025f);break;
                case IslandSymbol.Shop:
                    Stroke(mesh,.10f,.80f,.24f,.80f,.075f);Stroke(mesh,.24f,.80f,.36f,.34f,.075f);
                    Stroke(mesh,.36f,.34f,.77f,.34f,.075f);Stroke(mesh,.31f,.70f,.87f,.70f,.075f);
                    Stroke(mesh,.87f,.70f,.78f,.47f,.075f);Stroke(mesh,.78f,.47f,.36f,.47f,.075f);
                    Stroke(mesh,.48f,.68f,.50f,.49f,.05f);Stroke(mesh,.67f,.68f,.65f,.49f,.05f);
                    Disc(mesh,.42f,.18f,.08f);Disc(mesh,.73f,.18f,.08f);break;
                case IslandSymbol.Trophy:
                    RoundedBox(mesh,.27f,.16f,.46f,.09f,.04f);RoundedBox(mesh,.45f,.21f,.10f,.25f,.03f);
                    Quad(mesh,new Vector2(.27f,.82f),new Vector2(.73f,.82f),new Vector2(.68f,.48f),new Vector2(.32f,.48f));
                    Disc(mesh,.50f,.49f,.18f);
                    Stroke(mesh,.29f,.74f,.13f,.74f,.07f);Stroke(mesh,.13f,.74f,.16f,.56f,.07f);Stroke(mesh,.16f,.56f,.30f,.46f,.07f);
                    Stroke(mesh,.71f,.74f,.87f,.74f,.07f);Stroke(mesh,.87f,.74f,.84f,.56f,.07f);Stroke(mesh,.84f,.56f,.70f,.46f,.07f);break;
                case IslandSymbol.Settings:Ring(mesh,.50f,.50f,.145f,.43f,10);break;
                case IslandSymbol.Exit:
                    Stroke(mesh,.18f,.13f,.18f,.88f,.065f);Stroke(mesh,.18f,.88f,.57f,.88f,.065f);Stroke(mesh,.57f,.88f,.57f,.68f,.065f);
                    Quad(mesh,new Vector2(.24f,.82f),new Vector2(.48f,.74f),new Vector2(.48f,.15f),new Vector2(.24f,.22f));
                    Stroke(mesh,.51f,.47f,.82f,.47f,.10f);Triangle(mesh,new Vector2(.75f,.65f),new Vector2(.96f,.47f),new Vector2(.75f,.29f));break;
                case IslandSymbol.Camera:Camera(mesh);break;
                case IslandSymbol.Target:
                    Ring(mesh,.50f,.50f,.25f,.32f);Disc(mesh,.50f,.50f,.06f);
                    Stroke(mesh,.50f,.08f,.50f,.31f,.065f);Stroke(mesh,.50f,.69f,.50f,.92f,.065f);
                    Stroke(mesh,.08f,.50f,.31f,.50f,.065f);Stroke(mesh,.69f,.50f,.92f,.50f,.065f);break;
                case IslandSymbol.Shake:
                    CameraWithLens(mesh);
                    for(int side=0;side<2;side++)for(int line=0;line<2;line++){
                        float x=side==0?.07f+line*.09f:.93f-line*.09f,sign=side==0?1:-1;
                        Stroke(mesh,x,.30f,x+sign*.04f,.42f,.04f);Stroke(mesh,x+sign*.04f,.42f,x,.55f,.04f);Stroke(mesh,x,.55f,x+sign*.04f,.68f,.04f);
                    }break;
                case IslandSymbol.Leaf:
                    Triangle(mesh,new Vector2(.18f,.15f),new Vector2(.17f,.69f),new Vector2(.83f,.86f));
                    Triangle(mesh,new Vector2(.18f,.15f),new Vector2(.83f,.86f),new Vector2(.73f,.25f));
                    Stroke(mesh,.15f,.12f,.73f,.77f,.06f);break;
                case IslandSymbol.Laurel:Laurel(mesh);break;
            }
        }
    }
}
