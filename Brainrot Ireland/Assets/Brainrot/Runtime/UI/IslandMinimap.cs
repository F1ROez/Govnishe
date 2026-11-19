using UnityEngine;
using UnityEngine.UI;

namespace Brainrot
{
    /// <summary>Reusable UI map: matches IslandMap's zone classifier, never allocates textures per frame.</summary>
    public sealed class IslandMinimap : MonoBehaviour
    {
        RectTransform playerDot;
        RectTransform heading;
        Font font;
        Sprite solid;
        const float WorldSpan = 240;

        public void Initialize(Font mapFont, Sprite sprite, bool large)
        {
            font = mapFont; solid = sprite;
            var map = new GameObject("Остров", typeof(RectTransform), typeof(IslandMapGraphic));
            map.transform.SetParent(transform, false);
            var rect = map.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            map.GetComponent<IslandMapGraphic>().raycastTarget = false;
            for (int i = 0; i < 5; i++)
            {
                var zone = (IslandZone)i;
                var position = IslandMap.Center(zone);
                var label = new GameObject("Зона " + (i + 1), typeof(RectTransform), typeof(Text));
                label.transform.SetParent(map.transform, false);
                var lr = label.GetComponent<RectTransform>();
                var point = new Vector2(.5f + position.x / WorldSpan, .5f + position.z / WorldSpan);
                lr.anchorMin = lr.anchorMax = point; lr.sizeDelta = new Vector2(large ? 200 : 28, large ? 52 : 26);
                var text = label.GetComponent<Text>(); text.font = font; text.text = large ? (i + 1) + ". " + IslandMap.Name(zone) : (i + 1).ToString();
                text.fontSize = large ? 17 : 16; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.04f,.12f,.16f); text.raycastTarget = false;
            }
            var marker = new GameObject("Вы", typeof(RectTransform), typeof(Image));
            marker.transform.SetParent(map.transform, false); playerDot = marker.GetComponent<RectTransform>();
            playerDot.sizeDelta = Vector2.one * (large ? 12 : 8);
            var image = marker.GetComponent<Image>(); image.sprite = solid; image.color = Color.white; image.raycastTarget = false;
            var arrow = new GameObject("Направление", typeof(RectTransform), typeof(Image));
            arrow.transform.SetParent(marker.transform, false); heading = arrow.GetComponent<RectTransform>();
            heading.anchorMin = heading.anchorMax = new Vector2(.5f,.5f); heading.pivot = new Vector2(.5f,0); heading.sizeDelta = new Vector2(large ? 3 : 2,large ? 20 : 13);
            image=arrow.GetComponent<Image>();image.sprite=solid;image.color=new Color(1,.86f,.3f);image.raycastTarget=false;
        }
        void LateUpdate()
        {
            var player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (player == null || playerDot == null) return;
            var p = player.transform.position;
            playerDot.anchorMin = playerDot.anchorMax = new Vector2(.5f + p.x / WorldSpan,.5f + p.z / WorldSpan);
            playerDot.anchoredPosition = Vector2.zero;
            heading.localRotation = Quaternion.Euler(0,0,-player.transform.eulerAngles.y);
        }
    }

    public sealed class IslandMapGraphic : MaskableGraphic
    {
        const int Cells = 48;
        const float Span = 240;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var rect=GetPixelAdjustedRect();float step=1f/Cells;
            for(int y=0;y<Cells;y++)for(int x=0;x<Cells;x++)
            {
                var point=new Vector3(((x+.5f)/Cells-.5f)*Span,0,((y+.5f)/Cells-.5f)*Span);
                Color tint=new Color(.035f,.35f,.49f);
                if(IslandMap.IsWalkable(point,0))tint=Color.Lerp(IslandMap.Color(IslandMap.GetZone(point)),Color.white,.12f);
                AddQuad(helper,rect,new Vector2(x*step,y*step),new Vector2((x+1)*step,(y+1)*step),tint);
            }
            // Main paths are schematic and follow the same hub connections used by the world.
            Path(helper,rect,IslandMap.Center(IslandZone.Beach),IslandMap.Center(IslandZone.Meadows));
            Path(helper,rect,IslandMap.Center(IslandZone.Meadows),IslandMap.Center(IslandZone.Forest));
            Path(helper,rect,IslandMap.Center(IslandZone.Meadows),IslandMap.Center(IslandZone.Ruins));
            Path(helper,rect,IslandMap.Center(IslandZone.Meadows),IslandMap.Center(IslandZone.Swamp));
        }
        static void AddQuad(VertexHelper h,Rect r,Vector2 a,Vector2 b,Color c)
        {
            int n=h.currentVertCount;
            h.AddVert(new Vector3(r.xMin+a.x*r.width,r.yMin+a.y*r.height),c,Vector2.zero);
            h.AddVert(new Vector3(r.xMin+a.x*r.width,r.yMin+b.y*r.height),c,Vector2.zero);
            h.AddVert(new Vector3(r.xMin+b.x*r.width,r.yMin+b.y*r.height),c,Vector2.zero);
            h.AddVert(new Vector3(r.xMin+b.x*r.width,r.yMin+a.y*r.height),c,Vector2.zero);
            h.AddTriangle(n,n+1,n+2);h.AddTriangle(n,n+2,n+3);
        }
        static void Path(VertexHelper h,Rect r,Vector3 start,Vector3 end)
        {
            Vector2 a=new Vector2(r.center.x+start.x/Span*r.width,r.center.y+start.z/Span*r.height);
            Vector2 b=new Vector2(r.center.x+end.x/Span*r.width,r.center.y+end.z/Span*r.height);
            Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized*Mathf.Max(1,r.width*.004f);
            int n=h.currentVertCount;Color c=new Color(.91f,.77f,.46f);
            h.AddVert(a-side,c,Vector2.zero);h.AddVert(a+side,c,Vector2.zero);h.AddVert(b+side,c,Vector2.zero);h.AddVert(b-side,c,Vector2.zero);
            h.AddTriangle(n,n+1,n+2);h.AddTriangle(n,n+2,n+3);
        }
    }
}
