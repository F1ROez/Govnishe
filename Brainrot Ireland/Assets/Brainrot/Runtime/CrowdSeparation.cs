using System;
using System.Collections.Generic;
using UnityEngine;
namespace Brainrot
{
    [Serializable] public sealed class CrowdTuning
    {
        [Tooltip("Раздвигать живых наземных врагов, сохраняя траектории особых атак.")]public bool Enabled=true;
        [Range(0,.5f),Tooltip("Дополнительный зазор между телами врагов, м.")]public float Padding=.12f;
        [Range(.5f,12),Tooltip("Максимальная скорость мягкого раздвижения, м/с.")]public float MaxCorrectionSpeed=4.5f;
        [Range(0,.6f),Tooltip("Дополнительный зазор вокруг игрока, м.")]public float PlayerClearance=.15f;
        [Range(1,3),Tooltip("Число ограниченных проходов раздвижения за кадр.")]public int Iterations=2;
        [Range(4,32),Tooltip("Максимум соседей, проверяемых для одного врага за проход.")]public int MaxNeighbours=16;
        [Range(1,8),Tooltip("Максимум одновременно готовящихся или выполняемых укусов. Остальные акулы продолжают подходить, оставляя окно для ответа.")]public int MaxSimultaneousMelee=3;
        [Range(1,12),Tooltip("Максимум одновременно видимых обычных предупреждений укуса. Особые атаки показываются всегда.")]public int MaxMeleeWarnings=6;
    }

    /// <summary>Fixed-capacity spatial hash. No lists, colliders, or allocations are created during Resolve.</summary>
    public sealed class CrowdSeparation
    {
        const int Capacity=100,Buckets=256;
        readonly int[] heads=new int[Buckets],next=new int[Capacity],cellX=new int[Capacity],cellZ=new int[Capacity];
        readonly EnemyController[] enemies=new EnemyController[Capacity];
        readonly Vector2[] positions=new Vector2[Capacity],corrections=new Vector2[Capacity];
        readonly float[] radii=new float[Capacity];
        readonly bool[] movable=new bool[Capacity];
        int count;
        public int LastNeighbourChecks{get;private set;}
        static int Hash(int x,int z){unchecked{return ((x*73856093)^(z*19349663))&(Buckets-1);}}
        public static Vector2 DiskCorrection(Vector2 first,Vector2 second,float minimum,int firstId,int secondId)
        {
            Vector2 delta=first-second;float square=delta.sqrMagnitude;
            if(square>=minimum*minimum||minimum<=0)return Vector2.zero;
            if(square>.000001f){float distance=Mathf.Sqrt(square);return delta*((minimum-distance)/distance);}
            // Coincident centres must not choose the same direction: each pair gets opposite normals.
            int low=Mathf.Min(firstId,secondId),high=Mathf.Max(firstId,secondId);uint seed;
            unchecked{seed=(uint)(low*73856093)^(uint)(high*19349663);}
            float angle=(seed&1023)*(Mathf.PI*2/1024f);var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            return direction*(firstId<secondId?minimum:-minimum);
        }
        public void Resolve(IList<EnemyController> active,Vector3 playerPosition,float playerRadius,float dt,CrowdTuning tuning)
        {
            LastNeighbourChecks=0;
            if(tuning==null||!tuning.Enabled||dt<=0)return;
            int iterations=Mathf.Clamp(tuning.Iterations,1,3),limit=Mathf.Clamp(tuning.MaxNeighbours,4,32);
            float padding=Mathf.Clamp(tuning.Padding,0,.5f),maximum=Mathf.Clamp(tuning.MaxCorrectionSpeed,.5f,12)*Mathf.Min(dt,.1f)/iterations;
            var player=new Vector2(playerPosition.x,playerPosition.z);
            for(int pass=0;pass<iterations;pass++){
                count=0;float largest=.25f;
                for(int i=0;i<active.Count&&count<Capacity;i++){
                    var enemy=active[i];if(enemy==null||!enemy.CrowdParticipant)continue;
                    enemies[count]=enemy;positions[count]=new Vector2(enemy.transform.position.x,enemy.transform.position.z);
                    radii[count]=enemy.CrowdRadius;movable[count]=enemy.CrowdMovable;largest=Mathf.Max(largest,radii[count]);count++;
                }
                if(count==0)return;
                float cellSize=largest*2+padding;
                for(int i=0;i<Buckets;i++)heads[i]=-1;
                for(int i=0;i<count;i++){
                    cellX[i]=Mathf.FloorToInt(positions[i].x/cellSize);cellZ[i]=Mathf.FloorToInt(positions[i].y/cellSize);
                    int hash=Hash(cellX[i],cellZ[i]);next[i]=heads[hash];heads[hash]=i;
                }
                for(int i=0;i<count;i++){
                    corrections[i]=Vector2.zero;if(!movable[i])continue;
                    Vector2 sum=Vector2.zero;int checkedCount=0,overlaps=0;
                    for(int dz=-1;dz<=1&&checkedCount<limit;dz++)for(int dx=-1;dx<=1&&checkedCount<limit;dx++){
                        int x=cellX[i]+dx,z=cellZ[i]+dz;
                        for(int j=heads[Hash(x,z)];j>=0&&checkedCount<limit;j=next[j]){
                            checkedCount++;LastNeighbourChecks++;
                            if(j==i||cellX[j]!=x||cellZ[j]!=z)continue;
                            var away=DiskCorrection(positions[i],positions[j],radii[i]+radii[j]+padding,enemies[i].GetInstanceID(),enemies[j].GetInstanceID());
                            if(away.sqrMagnitude<=.0000001f)continue;
                            sum+=away*(movable[j]?.5f:1);overlaps++;
                        }
                    }
                    // Averaging makes a dense group soft instead of amplifying its speed with neighbour count.
                    if(overlaps>1)sum/=Mathf.Sqrt(overlaps);
                    sum=Vector2.ClampMagnitude(sum,maximum);
                    Vector2 candidate=positions[i]+sum;
                    var playerAway=DiskCorrection(candidate,player,radii[i]+Mathf.Max(.1f,playerRadius)+Mathf.Clamp(tuning.PlayerClearance,0,.6f),enemies[i].GetInstanceID(),0);
                    corrections[i]=Vector2.ClampMagnitude(sum+playerAway,maximum);
                }
                for(int i=0;i<count;i++)if(movable[i]&&corrections[i].sqrMagnitude>.0000001f)
                    enemies[i].ApplyCrowdCorrection(new Vector3(corrections[i].x,0,corrections[i].y));
            }
        }
    }
}
