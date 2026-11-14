using UnityEngine;
using System.Collections.Generic;
namespace Brainrot
{
    /// <summary>Replace this visual child with an imported rig without changing gameplay components.</summary>
    public sealed class CharacterVisual : MonoBehaviour
    {
        public Transform Weapon;
        public Transform WeaponTip;
        ImportedSahurVisual imported;
        ImportedEnemyVisual importedEnemy;
        public bool AttackActive => imported!=null?imported.AttackActive:attackRemaining > 0;
        TrailRenderer batTrail;
        float attackRemaining, attackDuration = .32f;
        Vector3 weaponRestPosition;
        Quaternion weaponRestRotation;
        static Material trailMaterial;
        Transform leftLeg,rightLeg,body,leftArm,rightArm;
        Vector3 bodyPosition; bool isShark;
        float phase,attackBlend;
        Renderer[] feedbackRenderers;
        Color[][] feedbackBaseColors;
        MaterialPropertyBlock feedbackBlock;
        float flashRemaining, flashDuration, flashStrength;
        bool feedbackApplied;
        static readonly int BaseColorId=Shader.PropertyToID("_BaseColor");
        static readonly int ColorId=Shader.PropertyToID("_Color");
        static Material white,black,wood,shoe,mouth;
        static readonly Dictionary<Color,Material> skins=new Dictionary<Color,Material>();
        static void Materials()
        {
            if(white!=null)return;
            white=WorldBuilder.Material(new Color(.98f,.98f,.91f));black=WorldBuilder.Material(new Color(.025f,.04f,.065f));
            wood=WorldBuilder.Material(new Color(.54f,.27f,.10f));shoe=WorldBuilder.Material(new Color(.12f,.30f,.94f));mouth=WorldBuilder.Material(new Color(.36f,.08f,.09f));
        }
        public static CharacterVisual Create(Transform parent,bool shark,Color color,float size=1)
        {
            return CreateInternal(parent,shark?0:-1,color,size);
        }
        public static CharacterVisual CreatePlayer(Transform parent,PlayerVisualStyle style,Color color,float size=1)
        {
            var prefab=Resources.Load<GameObject>("ImportedSahur");
            if(prefab!=null){
                var go=Object.Instantiate(prefab,parent,false);go.name="Тун-Тун Сахур — модель владельца";go.transform.localScale=Vector3.one*size;
                var c=go.AddComponent<CharacterVisual>();c.imported=go.GetComponent<ImportedSahurVisual>();
                if(c.imported!=null){c.Weapon=c.imported.Weapon;c.WeaponTip=c.imported.WeaponTip;c.CacheFeedbackRenderers();return c;}
                Object.Destroy(go);
            }
            Debug.LogWarning("Импортированная модель Сахура ещё не создана. Выполните Tools → TungTung → Создать или обновить игру. Пока используется временная модель.");
            return CreateInternal(parent,-1,color,size);
        }
        public static CharacterVisual CreateEnemy(Transform parent,EnemyKind kind,Color color,float size=1)
        {
            string resource=kind==EnemyKind.BlueberrinniOctopusini?"ImportedOctopus":kind==EnemyKind.Normal?"ImportedTralala":null;
            if(resource!=null){
                var prefab=Resources.Load<GameObject>(resource);
                if(prefab!=null){
                    var go=Object.Instantiate(prefab,parent,false);go.transform.localScale=Vector3.one*size;
                    var c=go.AddComponent<CharacterVisual>();c.importedEnemy=go.GetComponent<ImportedEnemyVisual>();
                    if(c.importedEnemy!=null){c.CacheFeedbackRenderers();return c;}
                    Object.Destroy(go);
                }
            }
            int species=kind==EnemyKind.BlueberrinniOctopusini?5:kind==EnemyKind.LiriliLarila?1:kind==EnemyKind.BrrBrrPatapim?2:kind==EnemyKind.CappuccinoAssassino?3:kind==EnemyKind.BombardiroCrocodilo?4:0;
            return CreateInternal(parent,species,color,size);
        }
        static CharacterVisual CreateInternal(Transform parent,int species,Color color,float size)
        {
            Materials();
            string[] names={"Тралалеро Тралала","Лирили Ларила","Брр Брр Патапим","Капучино Ассассино","Бомбардиро Крокодило","Блюберринни Октопусини"};
            var go=new GameObject(species<0?"Тун Тун Сахур":names[species]);go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*size;
            var c=go.AddComponent<CharacterVisual>();c.isShark=species==0;
            var skin=Skin(color);
            c.body=new GameObject("Тело").transform;c.body.SetParent(go.transform,false);
            switch(species)
            {
                case 0:c.BuildShark(skin);break;
                case 1:c.BuildElephant(skin);break;
                case 2:c.BuildPatapim(skin);break;
                case 3:c.BuildAssassin(skin);break;
                case 4:c.BuildCrocodile(skin);break;
                case 5:c.BuildOctopus();break;
                default:c.BuildSahur(skin);break;
            }
            c.bodyPosition=c.body.localPosition;c.CacheFeedbackRenderers();return c;
        }
        static Material Skin(Color color)
        {
            if(!skins.TryGetValue(color,out var skin)||skin==null){skin=WorldBuilder.Material(color,"Материал персонажа");skins[color]=skin;}
            return skin;
        }
        GameObject Part(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 s,Material mat)
        {return WorldBuilder.Primitive(name,type,parent,p,s,mat);}
        Transform Limb(string name,Vector3 pivot,Material mat)
        {
            var t=new GameObject(name).transform;t.SetParent(body,false);t.localPosition=pivot;
            Part(name+" shape",PrimitiveType.Capsule,t,new Vector3(0,-.23f,0),new Vector3(.19f,.32f,.19f),mat);return t;
        }
        void BuildSahur(Material skin)
        {
            Part("Log body",PrimitiveType.Cylinder,body,new Vector3(0,1.15f,0),new Vector3(.75f,.68f,.65f),skin);
            Part("Wood top",PrimitiveType.Cylinder,body,new Vector3(0,1.84f,0),new Vector3(.68f,.025f,.59f),wood);
            for(int j=0;j<7;j++)
            {
                float a=j*Mathf.PI*2/7;
                Part("Bark groove",PrimitiveType.Cube,body,new Vector3(Mathf.Cos(a)*.365f,1.15f,Mathf.Sin(a)*.31f),new Vector3(.022f,1.15f,.022f),wood);
            }
            Face(body,new Vector3(0,1.50f,.32f),.20f);
            leftLeg=Limb("Left leg",new Vector3(-.22f,.55f,0),skin);rightLeg=Limb("Right leg",new Vector3(.22f,.55f,0),skin);
            Part("Left foot",PrimitiveType.Sphere,leftLeg,new Vector3(0,-.43f,.08f),new Vector3(.24f,.16f,.39f),wood);
            Part("Right foot",PrimitiveType.Sphere,rightLeg,new Vector3(0,-.43f,.08f),new Vector3(.24f,.16f,.39f),wood);
            leftArm=Limb("Left arm",new Vector3(-.48f,1.35f,0),skin);rightArm=Limb("Bat arm",new Vector3(.48f,1.35f,0),skin);
            Weapon=new GameObject("Деревянная бита").transform;Weapon.SetParent(body,false);
            weaponRestPosition=new Vector3(.48f,.85f,.12f);weaponRestRotation=Quaternion.Euler(-35,22,-12);
            Weapon.localPosition=weaponRestPosition;Weapon.localRotation=weaponRestRotation;
            var handle=Part("Рукоять",PrimitiveType.Cylinder,Weapon,new Vector3(0,0,.21f),new Vector3(.105f,.28f,.105f),wood);handle.transform.localRotation=Quaternion.Euler(90,0,0);
            var bat=Part("Ударная часть биты",PrimitiveType.Capsule,Weapon,new Vector3(0,0,1.0f),new Vector3(.22f,.58f,.22f),wood);bat.transform.localRotation=Quaternion.Euler(90,0,0);
            Part("Лента на рукояти",PrimitiveType.Cylinder,Weapon,new Vector3(0,0,.2f),new Vector3(.115f,.14f,.115f),black).transform.localRotation=Quaternion.Euler(90,0,0);
            WeaponTip=new GameObject("Кончик биты").transform;WeaponTip.SetParent(Weapon,false);WeaponTip.localPosition=new Vector3(0,0,1.7f);
            batTrail=WeaponTip.gameObject.AddComponent<TrailRenderer>();batTrail.sharedMaterial=TrailMaterial();batTrail.time=.18f;
            batTrail.widthCurve=new AnimationCurve(new Keyframe(0,.22f),new Keyframe(1,0));batTrail.widthMultiplier=1;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.92f,0),new GradientAlphaKey(0,1)});
            batTrail.colorGradient=gradient;batTrail.emitting=false;batTrail.minVertexDistance=.025f;batTrail.numCornerVertices=2;batTrail.numCapVertices=2;
            batTrail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;batTrail.receiveShadows=false;
        }
        static Material TrailMaterial()
        {
            if(trailMaterial!=null)return trailMaterial;
            var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default");
            trailMaterial=new Material(shader){name="Белый шлейф биты",renderQueue=3000};trailMaterial.color=Color.white;
            if(trailMaterial.HasProperty("_Surface"))trailMaterial.SetFloat("_Surface",1);
            trailMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);trailMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            trailMaterial.SetInt("_ZWrite",0);trailMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");trailMaterial.SetOverrideTag("RenderType","Transparent");return trailMaterial;
        }
        void BuildShark(Material skin)
        {
            var torso=Part("Shark body",PrimitiveType.Capsule,body,new Vector3(0,1.08f,0),new Vector3(.94f,.85f,.92f),skin);
            torso.transform.localRotation=Quaternion.Euler(70,0,0);
            Part("White belly",PrimitiveType.Sphere,body,new Vector3(0,.97f,.40f),new Vector3(.70f,.60f,.31f),white);
            Part("Snout",PrimitiveType.Sphere,body,new Vector3(0,1.28f,.69f),new Vector3(.72f,.40f,.65f),skin);
            Part("Grinning jaw",PrimitiveType.Sphere,body,new Vector3(0,1.08f,.68f),new Vector3(.63f,.18f,.43f),mouth);
            for(int j=0;j<6;j++)
            {
                var tooth=Part("Tooth",PrimitiveType.Cube,body,new Vector3((j-2.5f)*.087f,1.13f,.878f),new Vector3(.06f,.095f,.04f),white);
                tooth.transform.localRotation=Quaternion.Euler(0,0,j%2==0?12:-12);
            }
            for(int j=-1;j<=1;j+=2)
            {
                Part("Eye",PrimitiveType.Sphere,body,new Vector3(j*.35f,1.49f,.46f),new Vector3(.22f,.23f,.13f),white);
                Part("Pupil",PrimitiveType.Sphere,body,new Vector3(j*.35f,1.48f,.525f),new Vector3(.09f,.12f,.035f),black);
                Fin(body,"Side fin",new Vector3(j*.48f,1.14f,-.05f),new Vector3(j*.65f,-.13f,-.28f),skin);
            }
            Fin(body,"Dorsal fin",new Vector3(0,1.40f,-.36f),new Vector3(0,.65f,-.27f),skin);
            Fin(body,"Tail left",new Vector3(0,1.03f,-.86f),new Vector3(-.44f,.25f,-.35f),skin);
            Fin(body,"Tail right",new Vector3(0,1.03f,-.86f),new Vector3(.44f,.25f,-.35f),skin);
            leftLeg=Limb("Left running leg",new Vector3(-.27f,.56f,0),skin);rightLeg=Limb("Right running leg",new Vector3(.27f,.56f,0),skin);
            foreach(var leg in new[]{leftLeg,rightLeg})
            {
                Part("Blue sneaker",PrimitiveType.Sphere,leg,new Vector3(0,-.43f,.12f),new Vector3(.35f,.25f,.60f),shoe);
                Part("Sneaker sole",PrimitiveType.Cube,leg,new Vector3(0,-.53f,.13f),new Vector3(.34f,.075f,.52f),white);
            }
        }
        void BuildElephant(Material skin)
        {
            var cactus=Skin(new Color(.31f,.60f,.34f));var ear=Skin(new Color(.65f,.48f,.60f));
            Part("Тело слона",PrimitiveType.Sphere,body,new Vector3(0,1.03f,-.12f),new Vector3(1.2f,1.15f,1.35f),skin);
            Part("Голова слона",PrimitiveType.Sphere,body,new Vector3(0,1.53f,.47f),new Vector3(.94f,.91f,.86f),skin);
            for(int side=-1;side<=1;side+=2)
            {
                Part("Большое ухо",PrimitiveType.Sphere,body,new Vector3(side*.63f,1.53f,.28f),new Vector3(.57f,.83f,.16f),ear);
                Part("Бивень",PrimitiveType.Capsule,body,new Vector3(side*.32f,1.17f,.89f),new Vector3(.12f,.27f,.12f),white).transform.localRotation=Quaternion.Euler(63,0,side*12);
            }
            // Three curved segments give the characteristic long elephant trunk.
            for(int i=0;i<4;i++)Part("Хобот",PrimitiveType.Sphere,body,new Vector3(0,1.35f-i*.20f,.89f+i*.035f),new Vector3(.28f-i*.025f,.37f,.28f-i*.02f),skin);
            for(int side=-1;side<=1;side+=2)
            {
                Part("Глаз слона",PrimitiveType.Sphere,body,new Vector3(side*.25f,1.71f,.827f),new Vector3(.2f,.22f,.08f),white);
                Part("Зрачок",PrimitiveType.Sphere,body,new Vector3(side*.25f,1.7f,.87f),new Vector3(.08f,.11f,.025f),black);
            }
            leftLeg=CactusLeg("Левая нога-кактус",new Vector3(-.36f,.65f,-.1f),cactus);
            rightLeg=CactusLeg("Правая нога-кактус",new Vector3(.36f,.65f,-.1f),cactus);
            Part("Кактус на спине",PrimitiveType.Capsule,body,new Vector3(0,1.77f,-.53f),new Vector3(.32f,.44f,.32f),cactus);
            Part("Цветок кактуса",PrimitiveType.Sphere,body,new Vector3(0,2.18f,-.53f),new Vector3(.27f,.13f,.27f),Skin(new Color(1,.48f,.49f)));
        }
        Transform CactusLeg(string name,Vector3 position,Material mat)
        {
            var root=new GameObject(name).transform;root.SetParent(body,false);root.localPosition=position;
            Part("Кактус",PrimitiveType.Capsule,root,new Vector3(0,-.2f,0),new Vector3(.27f,.32f,.27f),mat);
            Part("Песочная ступня",PrimitiveType.Sphere,root,new Vector3(0,-.48f,.12f),new Vector3(.48f,.18f,.5f),wood);
            for(int i=0;i<3;i++)Part("Иголка",PrimitiveType.Cube,root,new Vector3(.15f,-i*.16f,.01f),new Vector3(.1f,.025f,.025f),white).transform.localRotation=Quaternion.Euler(0,0,25);
            return root;
        }
        void BuildPatapim(Material skin)
        {
            var canopy=Skin(new Color(.18f,.49f,.22f));var lighter=Skin(new Color(.36f,.65f,.23f));
            Part("Древесный ствол",PrimitiveType.Cylinder,body,new Vector3(0,1.16f,0),new Vector3(.85f,.76f,.75f),skin);
            for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Part("Корень",PrimitiveType.Capsule,body,new Vector3(Mathf.Sin(a)*.4f,.37f,Mathf.Cos(a)*.36f),new Vector3(.23f,.38f,.23f),skin).transform.localRotation=Quaternion.Euler(Mathf.Cos(a)*38,0,Mathf.Sin(a)*38);}
            Part("Крона",PrimitiveType.Sphere,body,new Vector3(0,1.97f,-.1f),new Vector3(1.55f,.85f,1.25f),canopy);
            Part("Светлая крона",PrimitiveType.Sphere,body,new Vector3(.38f,2.15f,.07f),new Vector3(.96f,.65f,.94f),lighter);
            Part("Левая крона",PrimitiveType.Sphere,body,new Vector3(-.49f,1.93f,.02f),new Vector3(.88f,.66f,.95f),canopy);
            Face(body,new Vector3(0,1.4f,.37f),.22f);
            leftArm=Limb("Ветка слева",new Vector3(-.6f,1.38f,0),skin);rightArm=Limb("Ветка справа",new Vector3(.6f,1.38f,0),skin);
            Part("Листва на руке",PrimitiveType.Sphere,leftArm,new Vector3(0,-.32f,0),new Vector3(.35f,.3f,.4f),lighter);
            Part("Листва на руке",PrimitiveType.Sphere,rightArm,new Vector3(0,-.32f,0),new Vector3(.35f,.3f,.4f),canopy);
            leftLeg=Limb("Корневая нога слева",new Vector3(-.26f,.55f,0),skin);rightLeg=Limb("Корневая нога справа",new Vector3(.26f,.55f,0),skin);
            foreach(var leg in new[]{leftLeg,rightLeg})Part("Корневая ступня",PrimitiveType.Sphere,leg,new Vector3(0,-.42f,.12f),new Vector3(.43f,.17f,.55f),wood);
        }
        void BuildAssassin(Material skin)
        {
            var coffee=Skin(new Color(.25f,.13f,.08f));var ninja=Skin(new Color(.13f,.12f,.18f));var red=Skin(new Color(.71f,.18f,.23f));
            Part("Тело ниндзя",PrimitiveType.Capsule,body,new Vector3(0,.97f,0),new Vector3(.61f,.45f,.52f),ninja);
            Part("Чашка капучино",PrimitiveType.Cylinder,body,new Vector3(0,1.62f,0),new Vector3(.94f,.4f,.85f),skin);
            Part("Кофе сверху",PrimitiveType.Cylinder,body,new Vector3(0,2.025f,0),new Vector3(.83f,.025f,.75f),coffee);
            Part("Пенка",PrimitiveType.Sphere,body,new Vector3(-.1f,2.06f,.04f),new Vector3(.4f,.035f,.29f),white);
            // Open rectangular cup handle reads from either side without a torus dependency.
            foreach(var edge in new[]{new Vector3(.61f,1.9f,0),new Vector3(.77f,1.63f,0),new Vector3(.61f,1.37f,0)})Part("Ручка чашки",PrimitiveType.Capsule,body,edge,new Vector3(.13f,.18f,.15f),skin).transform.localRotation=Quaternion.Euler(0,0,edge.x>.7f?0:90);
            Part("Маска ниндзя",PrimitiveType.Sphere,body,new Vector3(0,1.65f,.405f),new Vector3(.8f,.29f,.13f),ninja);
            for(int side=-1;side<=1;side+=2){Part("Злой глаз",PrimitiveType.Sphere,body,new Vector3(side*.19f,1.69f,.467f),new Vector3(.19f,.08f,.04f),white);}
            Part("Красный пояс",PrimitiveType.Cube,body,new Vector3(0,.86f,.03f),new Vector3(.65f,.13f,.58f),red);
            leftArm=Limb("Левая рука",new Vector3(-.43f,1.08f,0),ninja);rightArm=Limb("Правая рука",new Vector3(.43f,1.08f,0),ninja);
            foreach(var arm in new[]{leftArm,rightArm}){
                Part("Рукоять клинка",PrimitiveType.Cylinder,arm,new Vector3(0,-.4f,.17f),new Vector3(.08f,.13f,.08f),wood).transform.localRotation=Quaternion.Euler(90,0,0);
                Part("Короткий клинок",PrimitiveType.Cube,arm,new Vector3(0,-.4f,.47f),new Vector3(.035f,.1f,.45f),white).transform.localRotation=Quaternion.Euler(0,0,15);
            }
            leftLeg=Limb("Левая нога",new Vector3(-.19f,.54f,0),ninja);rightLeg=Limb("Правая нога",new Vector3(.19f,.54f,0),ninja);
        }
        void BuildOctopus(){
            var blue=Skin(new Color(.10f,.38f,.9f));var pale=Skin(new Color(.4f,.72f,1));
            Part("Синяя голова",PrimitiveType.Sphere,body,new Vector3(0,1,0),new Vector3(1.2f,1.25f,1.1f),blue);
            Face(body,new Vector3(0,1.05f,.49f),.25f);
            for(int i=0;i<8;i++){
                float a=i*Mathf.PI/4;var dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                var arm=new GameObject("Щупальце").transform;arm.SetParent(body,false);arm.localPosition=dir*.37f+Vector3.up*.53f;arm.localRotation=Quaternion.LookRotation(dir);
                Part("Щупальце",PrimitiveType.Capsule,arm,new Vector3(0,-.14f,.28f),new Vector3(.22f,.38f,.22f),blue).transform.localRotation=Quaternion.Euler(48,0,0);
                Part("Кончик",PrimitiveType.Sphere,arm,new Vector3(0,-.27f,.53f),new Vector3(.3f,.23f,.3f),pale);
            }
            Part("Макушка",PrimitiveType.Sphere,body,new Vector3(0,1.61f,0),new Vector3(.65f,.18f,.6f),pale);
        }
        void BuildCrocodile(Material skin)
        {
            var belly=Skin(new Color(.75f,.77f,.38f));var wing=Skin(new Color(.41f,.46f,.32f));
            Part("Тело крокодила",PrimitiveType.Sphere,body,new Vector3(0,1.03f,-.15f),new Vector3(1.2f,.7f,1.65f),skin);
            Part("Морда крокодила",PrimitiveType.Cube,body,new Vector3(0,1.11f,.82f),new Vector3(.74f,.35f,1.05f),skin);
            Part("Нижняя челюсть",PrimitiveType.Cube,body,new Vector3(0,.91f,.86f),new Vector3(.7f,.16f,1.1f),belly);
            for(int side=-1;side<=1;side+=2){
                Part("Глаз крокодила",PrimitiveType.Sphere,body,new Vector3(side*.33f,1.39f,.47f),new Vector3(.27f,.26f,.2f),white);
                Part("Зрачок крокодила",PrimitiveType.Sphere,body,new Vector3(side*.33f,1.4f,.57f),new Vector3(.08f,.13f,.035f),black);
                for(int j=0;j<4;j++)Part("Зуб",PrimitiveType.Cube,body,new Vector3(side*.335f,1.015f,.62f+j*.2f),new Vector3(.055f,.09f,.07f),white);
                Fin(body,"Крыло бомбардировщика",new Vector3(side*.48f,1.06f,-.2f),new Vector3(side*1.08f,.03f,-.23f),wing);
                Part("Двигатель",PrimitiveType.Capsule,body,new Vector3(side*.77f,.94f,-.37f),new Vector3(.23f,.36f,.23f),wing).transform.localRotation=Quaternion.Euler(90,0,0);
            }
            Part("Хвост",PrimitiveType.Capsule,body,new Vector3(0,1.04f,-1.12f),new Vector3(.34f,.68f,.34f),skin).transform.localRotation=Quaternion.Euler(84,0,0);
            for(int j=0;j<4;j++)Fin(body,"Спинной гребень",new Vector3(0,1.32f,.2f-j*.27f),new Vector3(0,.27f,-.12f),wing);
            leftLeg=Limb("Левая лапа",new Vector3(-.42f,.63f,0),skin);rightLeg=Limb("Правая лапа",new Vector3(.42f,.63f,0),skin);
            foreach(var leg in new[]{leftLeg,rightLeg})Part("Лапа",PrimitiveType.Sphere,leg,new Vector3(0,-.43f,.15f),new Vector3(.42f,.18f,.59f),belly);
        }
        void Face(Transform p,Vector3 center,float separation)
        {
            for(int j=-1;j<=1;j+=2)
            {
                Part("Wide eye",PrimitiveType.Sphere,p,center+Vector3.right*(separation*j),new Vector3(.28f,.32f,.13f),white);
                Part("Pupil",PrimitiveType.Sphere,p,center+new Vector3(separation*j,-.01f,.065f),new Vector3(.11f,.15f,.035f),black);
            }
            Part("Smile",PrimitiveType.Sphere,p,center+new Vector3(0,-.28f,.018f),new Vector3(.32f,.13f,.08f),mouth);
        }
        void Fin(Transform parent,string name,Vector3 start,Vector3 tip,Material mat)
        {
            var a=start+Vector3.forward*.22f;var b=start-Vector3.forward*.22f;var c=start+tip;
            var vertices=new[]{a,b,c,a,b,c};
            var mesh=new Mesh{name=name,vertices=vertices,triangles=new[]{0,1,2,5,4,3}};mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        void CacheFeedbackRenderers()
        {
            var renderers=GetComponentsInChildren<Renderer>(true);
            var eligible=new List<Renderer>();foreach(var renderer in renderers)if(!(renderer is TrailRenderer))eligible.Add(renderer);
            feedbackRenderers=eligible.ToArray();
            feedbackBaseColors=new Color[feedbackRenderers.Length][];
            feedbackBlock=new MaterialPropertyBlock();
            for(int i=0;i<feedbackRenderers.Length;i++)
            {
                var materials=feedbackRenderers[i].sharedMaterials;feedbackBaseColors[i]=new Color[materials.Length];
                for(int slot=0;slot<materials.Length;slot++)feedbackBaseColors[i][slot]=materials[slot]!=null?materials[slot].color:Color.white;
            }
        }
        /// <summary>Short red tint without cloning or changing shared skin materials.</summary>
        public void FlashHit(float duration,float strength)
        {
            if(feedbackRenderers==null)CacheFeedbackRenderers();
            flashDuration=Mathf.Max(.01f,duration);
            flashRemaining=flashDuration;
            flashStrength=Mathf.Clamp01(strength);
            ApplyFeedback(flashStrength);
        }
        void ApplyFeedback(float strength)
        {
            if(feedbackRenderers==null)return;
            for(int i=0;i<feedbackRenderers.Length;i++)
            {
                var renderer=feedbackRenderers[i];if(renderer==null)continue;
                for(int slot=0;slot<feedbackBaseColors[i].Length;slot++){
                    Color baseColor=feedbackBaseColors[i][slot];Color color=Color.Lerp(baseColor,new Color(1,.16f,.14f,baseColor.a),strength);
                    feedbackBlock.Clear();feedbackBlock.SetColor(BaseColorId,color);feedbackBlock.SetColor(ColorId,color);
                    renderer.SetPropertyBlock(feedbackBlock,slot);
                }
            }
            feedbackApplied=true;
        }
        public void SetAttackReach(float worldRange)
        {
            if(imported!=null){imported.SetAttackReach(worldRange);return;}
            if(Weapon==null||WeaponTip==null)return;
            float bodyScale=Mathf.Max(.1f,Mathf.Abs(transform.lossyScale.z));
            float tip=Mathf.Max(.1f,WeaponTip.localPosition.z);
            Weapon.localScale=new Vector3(1,1,Mathf.Max(.3f,(worldRange/bodyScale-.12f)/tip));
        }
        public void PlayAttack(float duration=.32f)
        {
            if(imported!=null){imported.PlayAttack(duration);return;}
            if(Weapon==null)return;
            attackDuration=Mathf.Max(.15f,duration);attackRemaining=attackDuration;
            if(batTrail!=null){batTrail.emitting=false;batTrail.Clear();}
            TickAttack(0);
        }
        void TickAttack(float dt)
        {
            if(Weapon==null)return;
            if(attackRemaining<=0){if(batTrail!=null)batTrail.emitting=false;return;}
            attackRemaining=Mathf.Max(0,attackRemaining-dt);
            float elapsed=attackDuration-attackRemaining;
            float contact=Mathf.Min(.09f,attackDuration*.35f);
            float yaw;
            if(elapsed<contact)yaw=Mathf.Lerp(-95,0,Mathf.SmoothStep(0,1,elapsed/contact));
            else yaw=Mathf.Lerp(0,110,Mathf.SmoothStep(0,1,(elapsed-contact)/Mathf.Max(.01f,attackDuration-contact)));
            float progress=elapsed/attackDuration;
            Weapon.localPosition=new Vector3(.03f,1.15f,.12f);
            Weapon.localRotation=Quaternion.Euler(0,yaw,-5+Mathf.Sin(progress*Mathf.PI)*8);
            if(rightArm!=null)rightArm.localRotation=Quaternion.Euler(-70,yaw*.65f,-15);
            if(batTrail!=null)batTrail.emitting=progress>.03f&&progress<.86f;
            if(attackRemaining<=0){Weapon.localPosition=weaponRestPosition;Weapon.localRotation=weaponRestRotation;if(batTrail!=null)batTrail.emitting=false;}
        }
        void Update()
        {
            if(imported!=null||importedEnemy!=null){
                if(flashRemaining>0){flashRemaining=Mathf.Max(0,flashRemaining-Time.deltaTime);if(flashRemaining<=0)ResetFeedback();else ApplyFeedback(flashStrength*flashRemaining/flashDuration);}
                return;
            }
            if(Weapon!=null&&GameManager.Instance!=null&&!GameManager.Instance.IsPlaying)ResetAttack();
            else TickAttack(Time.deltaTime);
            if(isShark&&biteRemaining>0&&GameManager.Instance!=null&&GameManager.Instance.IsPlaying){
                biteRemaining=Mathf.Max(0,biteRemaining-Time.deltaTime);float t=1-biteRemaining/biteDuration;
                float lunge=t<.38f?Mathf.SmoothStep(0,1,t/.38f):1-Mathf.SmoothStep(0,1,(t-.38f)/.62f);
                body.localPosition=bodyPosition+Vector3.forward*(lunge*.65f);body.localRotation=Quaternion.Euler(lunge*12,0,0);
                if(biteRemaining<=0){body.localPosition=bodyPosition;body.localRotation=Quaternion.identity;}
            }
            if(flashRemaining<=0)return;
            flashRemaining=Mathf.Max(0,flashRemaining-Time.deltaTime);
            if(flashRemaining<=0)ResetFeedback();
            else ApplyFeedback(flashStrength*flashRemaining/flashDuration);
        }
        public void ResetFeedback()
        {
            flashRemaining=0;
            if(!feedbackApplied||feedbackRenderers==null)return;
            for(int i=0;i<feedbackRenderers.Length;i++)if(feedbackRenderers[i]!=null){feedbackRenderers[i].SetPropertyBlock(null);for(int slot=0;slot<feedbackBaseColors[i].Length;slot++)feedbackRenderers[i].SetPropertyBlock(null,slot);}
            feedbackApplied=false;
        }
        public void ResetAttack()
        {
            if(imported!=null){imported.ResetAttack();return;}
            attackRemaining=0;
            if(Weapon!=null){Weapon.localPosition=weaponRestPosition;Weapon.localRotation=weaponRestRotation;Weapon.localScale=Vector3.one;}
            if(batTrail!=null){batTrail.emitting=false;batTrail.Clear();}
        }
        void OnDisable(){ResetFeedback();ResetAttack();ResetBite();}
        float biteRemaining,biteDuration;
        public void PlayBite(float duration=.32f){if(importedEnemy!=null){importedEnemy.PlayBite(duration);return;}if(!isShark)return;biteDuration=Mathf.Max(.15f,duration);biteRemaining=biteDuration;}
        public float BiteProgress=>biteRemaining>0?1-biteRemaining/biteDuration:1;
        public void ResetBite(){if(importedEnemy!=null)importedEnemy.ResetVisual();biteRemaining=0;if(body!=null){body.localPosition=bodyPosition;body.localRotation=Quaternion.identity;}}
        public void Animate(float speed,bool attacking)
        {
            if(imported!=null){imported.Animate(speed,attacking);return;}
            if(importedEnemy!=null){importedEnemy.Animate(speed,attacking);return;}
            float intensity=Mathf.Clamp01(speed/3);
            phase+=Time.deltaTime*(3+Mathf.Abs(speed)*2.5f);
            float swing=Mathf.Sin(phase)*32*intensity;
            if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(swing,0,0);
            if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            if(biteRemaining<=0)body.localPosition=bodyPosition+Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.075f*intensity);
            if(biteRemaining<=0)body.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase)*3*intensity);
            attackBlend=Mathf.MoveTowards(attackBlend,attacking?1:0,Time.deltaTime*14);
            if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(-swing*.6f,0,-10);
            if(rightArm!=null&&!AttackActive)rightArm.localRotation=Quaternion.Euler(Mathf.Lerp(swing*.6f,-60,Weapon==null?attackBlend:0),0,-10);
            if(Weapon!=null&&!AttackActive){Weapon.localPosition=weaponRestPosition;Weapon.localRotation=weaponRestRotation;Weapon.localScale=Vector3.one;}

        }
    }
}
