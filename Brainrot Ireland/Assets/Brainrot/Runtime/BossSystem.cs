using System;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Independent deterministic bags; inspecting a wave never consumes spawning RNG.</summary>
    public sealed class BossWaveSchedule
    {
        readonly int seed;
        public BossWaveSchedule(int seed=0){this.seed=seed;}
        public IslandBossKind ForWave(int wave){
            if(!BossSystem.IsBossWave(wave))throw new ArgumentOutOfRangeException(nameof(wave));
            if(wave==5)return IslandBossKind.Shark;if(wave==10)return IslandBossKind.Octopus;if(wave==15)return IslandBossKind.Orcaladon;
            int index=(wave-20)/5,cycle=index/3;var bag=Shuffle(cycle);
            var previous=cycle>0?Shuffle(cycle-1)[2]:IslandBossKind.Orcaladon;
            // Swapping only the first two leaves the last item stable for the next bag.
            if(bag[0]==previous){var swap=bag[0];bag[0]=bag[1];bag[1]=swap;}
            return bag[index%3];
        }
        IslandBossKind[] Shuffle(int cycle){
            int hash;unchecked{hash=seed^(cycle*73856093)^19349663;}
            var random=new System.Random(hash);var bag=new[]{IslandBossKind.Shark,IslandBossKind.Octopus,IslandBossKind.Orcaladon};
            for(int i=2;i>0;i--){int j=random.Next(i+1);var item=bag[i];bag[i]=bag[j];bag[j]=item;}return bag;
        }
    }
    /// <summary>Wave-owned boss reference; the old timed MiniBoss/King system stays disabled.</summary>
    public sealed class BossSystem : MonoBehaviour
    {
        BossWaveSchedule schedule=new BossWaveSchedule(Environment.TickCount);
        public EnemyController CurrentBoss {get;private set;}
        public IslandBossKind SelectedKind {get;private set;}
        public int Wave {get;private set;}
        public static bool IsBossWave(int wave)=>wave>=5&&wave%5==0;
        public void PrepareWave(int wave){if(wave==Wave)return;Wave=wave;if(IsBossWave(wave))SelectedKind=schedule.ForWave(wave);CurrentBoss=null;}
        public void Register(EnemyController enemy){CurrentBoss=enemy;}
        public void Defeated(EnemyController enemy){if(CurrentBoss==enemy)CurrentBoss=null;}
        public void ResetRun(){CurrentBoss?.BossEncounter?.ResetEncounter();CurrentBoss=null;Wave=0;schedule=new BossWaveSchedule(Environment.TickCount);}
        public void Tick(float dt) { } // The active enemy owns one and only one encounter tick.
    }
}
