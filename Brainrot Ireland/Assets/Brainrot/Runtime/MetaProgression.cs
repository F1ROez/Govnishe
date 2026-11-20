using System;
namespace Brainrot
{
    public sealed class ShopUpgrade
    {
        public readonly int Id,MaxRank,BaseCost;
        public readonly string Title,Description;
        public ShopUpgrade(int id,string title,string description,int baseCost,int maxRank=5)
        {Id=id;Title=title;Description=description;BaseCost=baseCost;MaxRank=maxRank;}
    }
    /// <summary>Shared catalogue and compatibility facade; the actual economy belongs to the current attempt.</summary>
    public static class MetaProgression
    {
        public const int MaximumCoins=1000000000,MaximumReceipts=16384;
        public static readonly ShopUpgrade[] Catalog={
            new ShopUpgrade(0,"КРЕПКАЯ БИТА","+5 урона до конца забега за ранг",30),
            new ShopUpgrade(1,"ЗАПАС ПРОЧНОСТИ","+15 здоровья до конца забега за ранг",50),
            new ShopUpgrade(2,"ЛЁГКИЕ НОГИ","+0,3 скорости до конца забега за ранг",45),
            new ShopUpgrade(3,"БЫСТРЫЙ РЫВОК","−0,12 с перезарядки до конца забега",60),
            new ShopUpgrade(4,"СКВОЗНОЙ БРОСОК","Бумеранг проходит сквозь врагов до конца забега",40,1)
        };
        static RunEconomy Current=>GameManager.Instance!=null?GameManager.Instance.Economy:null;
        public static int Rank(int id)=>Current!=null?Current.Rank(id):0;
        public static int Cost(int id)=>Current!=null?Current.Cost(id):0;
        public static bool Buy(int id)=>GameManager.Instance!=null&&GameManager.Instance.TryBuyRunUpgrade(id);
        public static void Apply(PlayerStats stats){
            if(stats==null)return;stats.Damage+=Rank(0)*5;stats.MaxHealth+=Rank(1)*15;
            stats.MoveSpeed+=Rank(2)*.3f;stats.DashCooldown=UnityEngine.Mathf.Max(.25f,stats.DashCooldown-Rank(3)*.12f);
        }
        public static bool BeginRun(string runId)=>Current!=null&&Current.Begin(runId);
        static bool Matches(string runId)=>Current!=null&&Current.Active&&Current.RunId==runId;
        public static int RunEarned(string runId)=>Current!=null&&Current.RunId==runId?Current.Earned:0;
        public static bool IsClaimed(string runId,int dropId)=>Matches(runId)&&Current.IsClaimed(dropId);
        public static bool Award(string runId,int dropId,int amount)=>AwardBatch(runId,new[]{dropId},new[]{amount});
        public static bool AwardBatch(string runId,int[] ids,int[] amounts)=>Matches(runId)&&Current.AwardBatch(ids,amounts);
        /// <summary>Old permanent-money fields are migration input only, never active progression.</summary>
        internal static void Sanitize(SaveData data){
            data.Coins=0;data.UpgradeRanks=Array.Empty<int>();data.CoinRunId="";data.CoinRunEarned=0;data.ClaimedCoinIds=Array.Empty<int>();
        }
    }
}
