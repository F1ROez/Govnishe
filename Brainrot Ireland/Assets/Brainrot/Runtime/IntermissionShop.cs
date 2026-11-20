using UnityEngine;
namespace Brainrot
{
    /// <summary>World-owned market and fresh-key, cancellable hold input. No round timer starts a wave.</summary>
    public sealed class IntermissionShop:MonoBehaviour
    {
        GameManager game;IslandMarketStall model;bool worldRest,holdArmed,focused=true;float hold,retry;
        GameState previousState;
        readonly Collider[] blockers=new Collider[32];
        public bool Available=>game!=null&&game.HasRun&&WorldRest()&&model!=null&&model.Amount>=.999f;
        public bool CanInteract{
            get{
                if(!Available||game.Player==null||game.Player.Health<=0||(game.State!=GameState.Playing&&game.State!=GameState.RunShop))return false;
                var delta=game.Player.transform.position-model.transform.position;delta.y=0;
                float radius=Mathf.Max(1,GameBalance.Current.Intermission.InteractionRadius);
                return delta.sqrMagnitude<=radius*radius;
            }
        }
        public float StartHoldProgress=>Mathf.Clamp01(hold/Mathf.Max(.2f,GameBalance.Current.Intermission.StartHoldSeconds));
        public Vector3 WorldPosition=>model!=null?model.transform.position+Vector3.up*2.7f:Vector3.zero;
        public void Initialize(GameManager manager){
            if(model!=null)return;game=manager;previousState=game.State;
            model=new GameObject("Островная торговая лавка").AddComponent<IslandMarketStall>();model.transform.SetParent(transform,false);model.Build();
        }
        bool WorldRest()=>game!=null&&game.Spawner!=null&&game.Spawner.Waves.Resting&&!game.Spawner.Waves.WaitingForClear&&game.Spawner.Active.Count==0;
        void Update(){Tick(Time.unscaledDeltaTime,GameInput.InteractDown,GameInput.StartWaveHeld);}
        public void Tick(float delta,bool interactDown,bool startHeld){
            if(game==null||model==null)return;
            bool rest=game.HasRun&&WorldRest();
            if(rest&&!worldRest){
                hold=0;holdArmed=!startHeld;retry=0;
                TryPlace();
            }
            if(!rest&&worldRest){model.SetVisible(false);ResetHold();}
            worldRest=rest;
            if(rest&&!model.TargetVisible){retry-=Mathf.Max(0,delta);if(retry<=0){retry=.5f;TryPlace();}}
            if(game.State!=previousState){ResetHold();previousState=game.State;}
            bool playing=focused&&game.IsIntermission;
            model.TickPresentation(playing||!rest?Mathf.Max(0,delta):0,GameBalance.Current.Intermission.SpawnSeconds);
            if(!playing){ResetHold();return;}
            if(!startHeld){hold=0;holdArmed=true;}
            if(interactDown&&CanInteract){ResetHold();game.OpenRunShop();return;}
            if(startHeld&&holdArmed){
                hold+=Mathf.Max(0,delta);
                if(StartHoldProgress>=1){ResetHold();game.StartNextWave();}
            }
        }
        void TryPlace(){
            if(game.Player==null||!game.IsIntermission)return;
            Vector3 position;
            if(!TryFindPlacement(GameBalance.Current.Intermission.Position,game.Player.transform.position,Blocked,out position))return;
            var facing=game.Player.transform.position-position;facing.y=0;
            model.transform.SetPositionAndRotation(position,facing.sqrMagnitude>.01f?Quaternion.LookRotation(facing):Quaternion.identity);
            model.SetVisible(true);GameAudio.PlayMarketAppear();
        }
        bool Blocked(Vector3 point){
            int count=Physics.OverlapBoxNonAlloc(point+Vector3.up*1.5f,new Vector3(2.4f,1.4f,2.4f),blockers,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
            if(count>=blockers.Length)return true;
            for(int i=0;i<count;i++){
                var collider=blockers[i];blockers[i]=null;
                if(collider==null||collider.transform.IsChildOf(model.transform)||collider.bounds.max.y<=.3f)continue;
                return true;
            }
            return false;
        }
        public static bool TryFindPlacement(Vector3 preferred,Vector3 player,System.Func<Vector3,bool> blocked,out Vector3 chosen){
            preferred.y=0;preferred=IslandMap.ClampToLand(preferred,6);
            for(int pass=0;pass<4;pass++)for(int index=0;index<(pass==0?1:16);index++){
                float angle=index*Mathf.PI*.125f+.29f*pass;
                var point=pass==0?preferred:new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(4+pass*3);
                var distance=point-player;distance.y=0;
                if(!IslandMap.IsWalkable(point,5)||distance.sqrMagnitude<25||blocked!=null&&blocked(point))continue;
                chosen=point;return true;
            }
            chosen=Vector3.zero;return false;
        }
        public void ResetHold(){hold=0;holdArmed=false;}
        void OnApplicationFocus(bool value){focused=value;ResetHold();}
        void OnApplicationPause(bool paused){if(paused)ResetHold();}
        void OnDisable(){ResetHold();if(model!=null)model.SetVisible(false);}
    }
}
