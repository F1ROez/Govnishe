using UnityEngine;
namespace Brainrot {
 /// <summary>A pooled whole-body tumble, with solid collisions and bounded corpse lifetime.</summary>
 public sealed class EnemyPhysicsBody:MonoBehaviour {
  Rigidbody body;CapsuleCollider capsule;EnemyController enemy;Object surface;
  float remaining,nextTransfer;
  public bool IsPhysical {get;private set;}
  public Rigidbody Rigidbody=>body;
  void Awake(){
   enemy=GetComponent<EnemyController>();capsule=gameObject.AddComponent<CapsuleCollider>();capsule.radius=.55f;capsule.height=2;capsule.center=Vector3.up;
   var bounce=capsule.material;if(bounce!=null){bounce.bounciness=.35f;bounce.dynamicFriction=.5f;bounce.staticFriction=.55f;}surface=bounce;
   body=gameObject.AddComponent<Rigidbody>();body.mass=2.5f;body.linearDamping=.2f;body.angularDamping=.5f;
   body.interpolation=RigidbodyInterpolation.Interpolate;body.isKinematic=true;body.constraints=RigidbodyConstraints.FreezeRotation;
  }
  public void ResetBody(){
   if(body==null)return;if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
   body.collisionDetectionMode=CollisionDetectionMode.Discrete;body.isKinematic=true;body.constraints=RigidbodyConstraints.FreezeRotation;
   IsPhysical=false;remaining=0;nextTransfer=0;
  }
  public void Launch(Vector3 velocity,bool dead){
   var t=GameBalance.Current.Bodies;remaining=dead?Mathf.Max(1,t.CorpseSeconds):Mathf.Max(.3f,t.TumbleSeconds);
   IsPhysical=true;body.isKinematic=false;body.constraints=RigidbodyConstraints.None;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
   velocity.y=Mathf.Max(velocity.y,Mathf.Max(0,t.UpSpeed));
   body.maxAngularVelocity=Mathf.Max(1,t.Spin);body.linearVelocity=Vector3.ClampMagnitude(velocity,22);body.angularVelocity=Random.onUnitSphere*Mathf.Max(0,t.Spin);
   body.WakeUp();
  }
  void Update(){
   if(!IsPhysical||GameManager.Instance==null||!GameManager.Instance.IsPlaying)return;
   remaining-=Time.deltaTime;
   if(remaining>0&&transform.position.y>-12)return;
   if(enemy.Alive){var pos=transform.position;pos.y=0;float yaw=transform.eulerAngles.y;ResetBody();transform.position=pos;transform.rotation=Quaternion.Euler(0,yaw,0);enemy.OnRecovered();}
   else GameManager.Instance.Spawner.ReleaseCorpse(enemy);
  }
  void OnCollisionEnter(Collision collision){
   if(!IsPhysical||Time.time<nextTransfer)return;
   var other=collision.collider.GetComponentInParent<EnemyController>();if(other==null||other==enemy||!other.Alive||other.PhysicsBody.IsPhysical)return;
   float speed=collision.relativeVelocity.magnitude;if(speed<2)return;
   Vector3 direction=other.transform.position-transform.position;direction.y=0;if(direction.sqrMagnitude<.001f)direction=transform.forward;
   nextTransfer=Time.time+.2f;other.Tumble(direction.normalized*Mathf.Min(10,speed*Mathf.Clamp01(GameBalance.Current.Bodies.ChainPush)));
  }
  void OnDisable(){ResetBody();}
  void OnDestroy(){if(surface!=null)Destroy(surface);}
 }
}
