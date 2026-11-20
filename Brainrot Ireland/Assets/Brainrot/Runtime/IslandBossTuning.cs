using System;
using UnityEngine;
namespace Brainrot
{
    public enum IslandBossKind { Shark,Octopus,Orcaladon }
    /// <summary>New wave bosses only. Legacy timer/king settings remain migration data.</summary>
    [Serializable] public sealed class IslandBossTuning
    {
        public BossFoeTuning Shark=new BossFoeTuning();
        public BossFoeTuning Octopus=NewOctopus();
        public BossFoeTuning Orcaladon=NewOrcaladon();
        public BossFoeTuning For(IslandBossKind kind)=>kind==IslandBossKind.Octopus?Octopus:kind==IslandBossKind.Orcaladon?Orcaladon:Shark;
        static BossFoeTuning NewOctopus()=>new BossFoeTuning{Health=900,Damage=25,Size=2.25f,Speed=3,PhaseThreshold=.5f,ScalingStartWave=10,AreaRadius=3.5f,CaptureRange=3.8f,CaptureSeconds=2.5f};
        static BossFoeTuning NewOrcaladon()=>new BossFoeTuning{Health=1300,Damage=30,Size=2,Speed=2.1f,PhaseThreshold=.4f,ScalingStartWave=15,Recovery=2,AreaRadius=9,ShockwaveSpeed=9,ChargeSpeed=14,ChargeWidth=2};
        public void Ensure(){if(Shark==null)Shark=new BossFoeTuning();if(Octopus==null)Octopus=NewOctopus();if(Orcaladon==null)Orcaladon=NewOrcaladon();}
    }
    [Serializable] public sealed class BossFoeTuning
    {
        [Min(1),Tooltip("Здоровье первой встречи с этим боссом.")]public float Health=650;
        [Min(0),Tooltip("Базовый урон; отдельные атаки используют множитель.")]public float Damage=22;
        [Range(1,4),Tooltip("Масштаб относительно обычной модели. Физический корень не масштабируется.")]public float Size=2.5f;
        [Min(.1f),Tooltip("Скорость обычного сближения, м/с.")]public float Speed=4.4f;
        [Range(.4f,8),Tooltip("Передышка перед выбором новой атаки, с.")]public float AttackInterval=1.4f;
        [Range(.3f,4),Tooltip("Основная читаемая подготовка атаки, с.")]public float Windup=1;
        [Range(.3f,5),Tooltip("Окно контратаки после сильной серии, с.")]public float Recovery=1.1f;
        [Range(.5f,5),Tooltip("Оглушение от столкновения либо освобождения игрока, с.")]public float StunSeconds=2;
        [Range(.1f,.9f),Tooltip("Доля HP для второй фазы.")]public float PhaseThreshold=.5f;
        [Range(0,1),Tooltip("Умеренный рост HP каждые пять волн; ограничен потолком.")]public float HealthGrowth=.35f;
        [Range(0,.2f),Tooltip("Рост урона каждые пять волн; суммарный множитель не выше1,65.")]public float DamageGrowth=.04f;
        [Range(1,5),Tooltip("Предельный множитель здоровья. Рост плавно приближается к потолку.")]public float ScalingCap=3;
        [Min(5),Tooltip("Волна первой встречи, от которой считается рост характеристик.")]public int ScalingStartWave=5;
        [Min(1),Tooltip("Скорость прямолинейного тарана, м/с.")]public float ChargeSpeed=17;
        [Range(.3f,2),Tooltip("Длительность одного разбега, с.")]public float ChargeSeconds=.85f;
        [Range(.4f,3),Tooltip("Подготовка тарана, с.")]public float ChargeWindup=1.1f;
        [Range(.1f,.8f),Tooltip("До старта тарана направление фиксируется за это время, с.")]public float ChargeLockSeconds=.28f;
        [Range(5,25),Tooltip("Дальность сегмента тяжёлого тарана, м.")]public float ChargeDistance=15;
        [Range(.5f,4),Tooltip("Полуширина предупреждения тяжёлого тарана, м.")]public float ChargeWidth=2;
        [Range(2,8),Tooltip("Дальность укуса гигантской акулы, м.")]public float BiteRange=4.8f;
        [Range(.3f,2),Tooltip("Подготовка быстрого укуса, с.")]public float BiteWindup=.45f;
        [Range(.5f,3),Tooltip("Подготовка финального укуса, с.")]public float FinalBiteWindup=.85f;
        [Range(3,15),Tooltip("Максимальный радиус волны от удара кроссовком, м.")]public float StompRadius=9;
        [Range(3,12),Tooltip("Скорость расширения ударной волны, м/с.")]public float ShockwaveSpeed=7;
        [Range(1,5),Tooltip("Длительность акульего водоворота, с.")]public float SpinSeconds=2.4f;
        [Range(1,6),Tooltip("Опасный радиус водоворота, м.")]public float SpinRadius=3.6f;
        [Range(1,8),Tooltip("Высота прыжка босса, м; визуальная поза не меняет коллайдер.")]public float JumpHeight=4;
        [Range(.8f,3),Tooltip("Предупреждение о месте приземления прыжка, с.")]public float JumpWindup=1.5f;
        [Range(2,14),Tooltip("Радиус основного удара по площади, м.")]public float AreaRadius=3.5f;
        [Range(.7f,3),Tooltip("Радиус щупальца/чернильной лужи, м.")]public float HazardRadius=1.65f;
        [Range(2,10),Tooltip("Время жизни чернильной лужи, с.")]public float HazardSeconds=5;
        [Range(2,7),Tooltip("Дальность захвата осьминога, м.")]public float CaptureRange=3.8f;
        [Range(1,5),Tooltip("Максимальное удержание захватом осьминога, с.")]public float CaptureSeconds=2.5f;
        [Range(2,8),Tooltip("Дальность кулачной серии Оркаладона, м.")]public float MeleeRange=5;
        [Range(.3f,2),Tooltip("Подготовка быстрого удара кулаком, с.")]public float ComboWindup=.45f;
        [Range(.6f,3),Tooltip("Подготовка финального двойного удара, с.")]public float FinalWindup=.9f;
        [Range(2,8),Tooltip("Дальность захвата Оркаладона, м.")]public float GrabRange=5;
        [Range(.8f,4),Tooltip("Максимальное удержание перед броском, с.")]public float GrabSeconds=1.8f;
        [Range(0,20),Tooltip("Сила отбрасывания мощной атакой, м/с.")]public float Knockback=12;
        float Steps(int wave)=>Mathf.Max(0,(wave-Mathf.Max(5,ScalingStartWave))/5f);
        public float HealthAt(int wave){float cap=Mathf.Clamp(ScalingCap,1,5);return Mathf.Max(1,Health)*(cap<=1?1:1+(cap-1)*(1-Mathf.Exp(-Mathf.Max(0,HealthGrowth)*Steps(wave)/(cap-1))));}
        public float DamageAt(int wave)=>Mathf.Max(0,Damage)*(1+.65f*(1-Mathf.Exp(-Mathf.Max(0,DamageGrowth)*Steps(wave)/.65f)));
        public float TempoAt(int wave)=>1+Mathf.Min(.25f,Steps(wave)*.02f);
    }
}
