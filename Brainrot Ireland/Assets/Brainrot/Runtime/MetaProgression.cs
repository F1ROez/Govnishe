using System;
using System.Collections.Generic;
using UnityEngine;
namespace Brainrot
{
    public sealed class ShopUpgrade
    {
        public readonly int Id,MaxRank,BaseCost;
        public readonly string Title,Description;
        public ShopUpgrade(int id,string title,string description,int baseCost,int maxRank=5)
        {Id=id;Title=title;Description=description;BaseCost=baseCost;MaxRank=maxRank;}
    }
    /// <summary>Permanent wallet and finite upgrades. Persist each transaction before confirming its result.</summary>
    public static class MetaProgression
    {
        public const int MaximumCoins=1000000000,MaximumReceipts=16384;
        public static readonly ShopUpgrade[] Catalog={
            new ShopUpgrade(0,"КРЕПКАЯ БИТА","+5 урона за уровень",30),
            new ShopUpgrade(1,"ЗАПАС ПРОЧНОСТИ","+15 максимального здоровья за уровень",50),
            new ShopUpgrade(2,"ЛЁГКИЕ НОГИ","+0,3 к скорости движения за уровень",45),
            new ShopUpgrade(3,"БЫСТРЫЙ РЫВОК","−0,12 с перезарядки рывка за уровень",60)
        };
        public static int Rank(int id)
        {
            if(id<0||id>=Catalog.Length)return 0;
            var ranks=SaveSystem.Data.UpgradeRanks;
            return ranks!=null&&id<ranks.Length?Mathf.Clamp(ranks[id],0,Catalog[id].MaxRank):0;
        }
        public static int Cost(int id)
        {
            if(id<0||id>=Catalog.Length||Rank(id)>=Catalog[id].MaxRank)return 0;
            return Mathf.CeilToInt(Catalog[id].BaseCost*Mathf.Pow(1.65f,Rank(id)));
        }
        public static bool Buy(int id)
        {
            if(id<0||id>=Catalog.Length)return false;
            var data=SaveSystem.Data;int rank=Rank(id),cost=Cost(id);
            if(rank>=Catalog[id].MaxRank||cost<=0||data.Coins<cost)return false;
            int before=data.Coins;var old=data.UpgradeRanks;
            var upgraded=new int[Catalog.Length];if(old!=null)Array.Copy(old,upgraded,Math.Min(old.Length,upgraded.Length));
            upgraded[id]=rank+1;data.UpgradeRanks=upgraded;data.Coins-=cost;
            if(SaveSystem.TrySave())return true;
            data.Coins=before;data.UpgradeRanks=old;return false;
        }
        /// <summary>Apply once when creating base stats for a new run, never on live shop purchase.</summary>
        public static void Apply(PlayerStats stats)
        {
            if(stats==null)return;
            stats.Damage+=Rank(0)*5;stats.MaxHealth+=Rank(1)*15;
            stats.MoveSpeed+=Rank(2)*.3f;stats.DashCooldown=Mathf.Max(.25f,stats.DashCooldown-Rank(3)*.12f);
        }
        public static bool BeginRun(string runId)
        {
            if(!ValidRunId(runId))return false;
            var data=SaveSystem.Data;if(data.CoinRunId==runId)return true;
            string before=data.CoinRunId;var old=data.ClaimedCoinIds;int earned=data.CoinRunEarned;
            data.CoinRunId=runId;data.ClaimedCoinIds=Array.Empty<int>();data.CoinRunEarned=0;
            if(SaveSystem.TrySave())return true;
            data.CoinRunId=before;data.ClaimedCoinIds=old;data.CoinRunEarned=earned;return false;
        }
        public static int RunEarned(string runId)=>SaveSystem.Data.CoinRunId==runId?SaveSystem.Data.CoinRunEarned:0;
        static int[] cachedIds;
        static readonly HashSet<int> claimedLookup=new HashSet<int>();
        public static bool IsClaimed(string runId,int dropId)
        {
            var data=SaveSystem.Data;if(data.CoinRunId!=runId||data.ClaimedCoinIds==null)return false;
            if(!ReferenceEquals(cachedIds,data.ClaimedCoinIds)){cachedIds=data.ClaimedCoinIds;claimedLookup.Clear();foreach(int id in cachedIds)claimedLookup.Add(id);}
            return claimedLookup.Contains(dropId);
        }
        public static bool Award(string runId,int dropId,int amount)=>AwardBatch(runId,new[]{dropId},new[]{amount});
        public static bool AwardBatch(string runId,int[] ids,int[] amounts)
        {
            var data=SaveSystem.Data;
            if(!ValidRunId(runId)||data.CoinRunId!=runId||ids==null||amounts==null||ids.Length==0||ids.Length!=amounts.Length)return false;
            var old=data.ClaimedCoinIds??Array.Empty<int>();if(old.Length+ids.Length>MaximumReceipts)return false;
            var unique=new HashSet<int>();long total=0;
            for(int i=0;i<ids.Length;i++){
                if(ids[i]<1||amounts[i]<1||amounts[i]>10000||IsClaimed(runId,ids[i])||!unique.Add(ids[i]))return false;
                total+=amounts[i];
            }
            int before=data.Coins,earned=data.CoinRunEarned;var next=new int[old.Length+ids.Length];Array.Copy(old,next,old.Length);Array.Copy(ids,0,next,old.Length,ids.Length);
            data.ClaimedCoinIds=next;data.Coins=(int)Math.Min(MaximumCoins,(long)Math.Max(0,data.Coins)+total);
            data.CoinRunEarned=(int)Math.Min(MaximumCoins,(long)data.CoinRunEarned+(data.Coins-before));
            if(SaveSystem.TrySave())return true;
            data.Coins=before;data.ClaimedCoinIds=old;data.CoinRunEarned=earned;return false;
        }
        static bool ValidRunId(string id)=>!string.IsNullOrEmpty(id)&&id.Length<=64;
        internal static void Sanitize(SaveData data)
        {
            data.Coins=Mathf.Clamp(data.Coins,0,MaximumCoins);
            data.CoinRunEarned=Mathf.Clamp(data.CoinRunEarned,0,MaximumCoins);
            var ranks=new int[Catalog.Length];for(int i=0;i<ranks.Length;i++)ranks[i]=data.UpgradeRanks!=null&&i<data.UpgradeRanks.Length?Mathf.Clamp(data.UpgradeRanks[i],0,Catalog[i].MaxRank):0;
            data.UpgradeRanks=ranks;
            if(!ValidRunId(data.CoinRunId)){data.CoinRunId="";data.CoinRunEarned=0;data.ClaimedCoinIds=Array.Empty<int>();return;}
            var ids=new List<int>();var unique=new HashSet<int>();
            if(data.ClaimedCoinIds!=null)foreach(int id in data.ClaimedCoinIds){if(ids.Count>=MaximumReceipts)break;if(id>0&&unique.Add(id))ids.Add(id);}
            data.ClaimedCoinIds=ids.ToArray();
        }
    }
}
