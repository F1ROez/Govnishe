using UnityEngine;

namespace Brainrot
{
    public readonly struct WaveChallengePlan
    {
        public readonly string Title, Description;
        public readonly float SpawnIntervalMultiplier;
        public readonly int SpawnSectors;
        public WaveChallengePlan(string title,string description,float multiplier,int sectors)
        {Title=title;Description=description;SpawnIntervalMultiplier=multiplier;SpawnSectors=sectors;}
    }

    /// <summary>Announced, bounded wave variations. Enemy damage and unlocked species never change invisibly.</summary>
    public static class WaveChallenges
    {
        public static WaveChallengePlan ForWave(int number)
        {
            if(number<4)return new WaveChallengePlan("","",1,0);
            switch((number-4)%4)
            {
                case 0:return new WaveChallengePlan("ПРИЛИВ","Враги выходят из океана немного чаще.",.90f,0);
                case 1:return new WaveChallengePlan("ТРИ БЕРЕГА","Противники наступают с трёх участков берега.",1,3);
                case 2:return new WaveChallengePlan("ВСТРЕЧНЫЕ ВОЛНЫ","Следите за двумя противоположными берегами.",.95f,2);
                default:return new WaveChallengePlan("ТИХАЯ ВОДА","Появления реже — время перевести дух.",1.12f,0);
            }
        }

        public static float SpawnAngle(int wave,float roll)
        {
            float unit=Mathf.Repeat(float.IsNaN(roll)||float.IsInfinity(roll)?0:roll,1);
            int sectors=ForWave(wave).SpawnSectors;
            if(sectors<=0)return unit*Mathf.PI*2;
            float selected=unit*sectors;
            int sector=Mathf.Min(sectors-1,Mathf.FloorToInt(selected));
            float spread=(selected-sector-.5f)*.44f;
            float rotation=Mathf.Repeat(Mathf.Max(1,wave)*2.39996323f,Mathf.PI*2);
            return Mathf.Repeat(rotation+sector*Mathf.PI*2/sectors+spread,Mathf.PI*2);
        }
    }
}
