using UnityEngine;
namespace Brainrot {
 public enum OctopusPhase {Approach,Aiming,Jumping,Attached,Dropping,Retreat}
 public sealed class OctopusAttack:MonoBehaviour {
  EnemyController enemy;EnemySpawner owner;LineRenderer marker;Material markerMaterial;
  Vector3 target,start;float clock,duration;bool attackJump;
  public OctopusPhase Phase{get;private set;}
  public Vector3 Target=>target;
  public void Initialize(EnemyController e,EnemySpawner spawner){enemy=e;owner=spawner;Release();Phase=OctopusPhase.Approach;if(marker==null){
   var go=new GameObject("Красная точка приземления");go.transform.SetParent(transform,false);marker=go.AddComponent<LineRenderer>();marker.useWorldSpace=true;marker.loop=true;marker.positionCount=48;marker.widthMultiplier=.09f;
   markerMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));markerMaterial.color=new Color(1,.05f,.04f);marker.sharedMaterial=markerMaterial;
  }marker.enabled=false;}
  public void Tick(float dt){
   var player=GameManager.Instance.Player;var t=GameBalance.Current.Octopus;
   if(Phase==OctopusPhase.Approach){Vector3 delta=player.transform.position-transform.position;delta.y=0;
    if(delta.magnitude<=Mathf.Max(4,t.AimDistance)){target=player.transform.position;target.y=0;Phase=OctopusPhase.Aiming;clock=Mathf.Max(.3f,t.AimSeconds);DrawTarget();}
    else {transform.position+=delta.normalized*GameBalance.Current.BlueOctopus.Speed*dt;if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(delta);}
   }else if(Phase==OctopusPhase.Aiming){clock-=dt;if(clock<=0)StartJump(target,true);}
   else if(Phase==OctopusPhase.Jumping||Phase==OctopusPhase.Dropping){
    clock+=dt;float progress=Mathf.Clamp01(clock/duration);transform.position=Vector3.Lerp(start,target,progress)+Vector3.up*Mathf.Sin(progress*Mathf.PI)*(attackJump?t.JumpHeight:2);
    if(progress>=1){marker.enabled=false;enemy.PhysicsBody.SetColliderEnabled(true);
     Vector3 delta=player.transform.position-target;delta.y=0;
     if(attackJump&&delta.magnitude<=t.LandingRadius&&player.BeginCapture(this)){Phase=OctopusPhase.Attached;enemy.PhysicsBody.SetColliderEnabled(false);FollowHead();}
     else Phase=OctopusPhase.Retreat;
    }
   }else if(Phase==OctopusPhase.Attached){if(!player.IsCapturedBy(this)){Escape();return;}FollowHead();}
   else if(Phase==OctopusPhase.Retreat){Vector3 away=transform.position;away.y=0;if(away.sqrMagnitude<.01f)away=enemy.HomePosition;away.Normalize();transform.position+=away*Mathf.Max(1,t.RetreatSpeed)*dt;if(new Vector2(transform.position.x,transform.position.z).magnitude>=27)owner.Despawn(enemy);}
  }
  void DrawTarget(){marker.enabled=true;for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;marker.SetPosition(i,target+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*GameBalance.Current.Octopus.LandingRadius+Vector3.up*.04f);}}
  void FollowHead(){var player=GameManager.Instance.Player;transform.position=player.transform.position+Vector3.up*1.6f;transform.rotation=player.transform.rotation;}
  void StartJump(Vector3 destination,bool attacking){start=transform.position;target=destination;clock=0;duration=attacking?Mathf.Max(.4f,GameBalance.Current.Octopus.JumpSeconds):.4f;attackJump=attacking;Phase=attacking?OctopusPhase.Jumping:OctopusPhase.Dropping;enemy.PhysicsBody.SetColliderEnabled(false);}
  public void Escape(){Release();Vector3 drop=IslandMap.ClampToLand(transform.position+GameManager.Instance.Player.transform.forward*2);drop.y=0;StartJump(drop,false);}
  public void Interrupt(){Release();if(marker!=null)marker.enabled=false;Phase=OctopusPhase.Retreat;}
  void Release(){if(GameManager.Instance!=null&&GameManager.Instance.Player!=null)GameManager.Instance.Player.EndCapture(this);if(enemy!=null&&enemy.PhysicsBody!=null)enemy.PhysicsBody.SetColliderEnabled(true);}
  void OnDisable(){Release();if(marker!=null)marker.enabled=false;}
  void OnDestroy(){if(markerMaterial!=null)Destroy(markerMaterial);}
 }
}
