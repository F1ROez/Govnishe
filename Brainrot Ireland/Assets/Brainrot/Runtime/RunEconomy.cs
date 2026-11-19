using System;
using System.Collections.Generic;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Attempt-owned coins, receipts and upgrade ranks. No disk access or cross-attempt bonuses.</summary>
    public sealed class RunEconomy
    {
        readonly int[] ranks=new int[4];
        readonly HashSet<int> claimed=new HashSet<int>();
        readonly HashSet<int> batch=new HashSet<int>();
        readonly List<int> offers=new List<int>(3);
        readonly bool[] sold=new bool[4];
        readonly System.Collections.ObjectModel.ReadOnlyCollection<int> offerView;
        System.Random offerRandom;
        public RunEconomy(){offerView=offers.AsReadOnly();}
        public IReadOnlyList<int> OfferIds=>offerView;
        public int OfferWave{get;private set;}
        public string RunId{get;private set;}="";
        public bool Active{get;private set;}
        public int Balance{get;private set;}
        public int Earned{get;private set;}
        public bool Begin(string id){
            if(string.IsNullOrEmpty(id)||id.Length>64)return false;
            RunId=id;Active=true;Balance=Earned=0;Array.Clear(ranks,0,ranks.Length);claimed.Clear();batch.Clear();
            unchecked{int seed=17;foreach(char letter in id)seed=seed*31+letter;offerRandom=new System.Random(seed);}
            offers.Clear();Array.Clear(sold,0,sold.Length);OfferWave=0;return true;
        }
        /// <summary>The result keeps its earned total; every spendable benefit is discarded.</summary>
        public void Complete(){Active=false;Balance=0;Array.Clear(ranks,0,ranks.Length);claimed.Clear();batch.Clear();offers.Clear();Array.Clear(sold,0,sold.Length);OfferWave=0;}
        /// <summary>Sample once per cleared wave. Reopening or purchasing cannot reroll stock.</summary>
        public void EnsureOffers(int wave){
            if(!Active||wave<1||wave<=OfferWave)return;
            OfferWave=wave;offers.Clear();Array.Clear(sold,0,sold.Length);
            var available=new int[MetaProgression.Catalog.Length];int count=0;
            for(int id=0;id<available.Length;id++)if(Rank(id)<MetaProgression.Catalog[id].MaxRank)available[count++]=id;
            for(int i=0;i<Math.Min(3,count);i++){
                int chosen=offerRandom.Next(i,count),swap=available[i];available[i]=available[chosen];available[chosen]=swap;offers.Add(available[i]);
            }
        }
        public bool IsOfferSold(int id)=>id>=0&&id<sold.Length&&sold[id];
        public bool CanBuyOffer(int id)=>Active&&offers.Contains(id)&&!IsOfferSold(id)&&Cost(id)>0&&Balance>=Cost(id);
        public bool TryBuyOffer(int id){if(!CanBuyOffer(id)||!Buy(id))return false;sold[id]=true;return true;}
        public int Rank(int id)=>Active&&id>=0&&id<ranks.Length?ranks[id]:0;
        public int Cost(int id){
            if(!Active||id<0||id>=MetaProgression.Catalog.Length||Rank(id)>=MetaProgression.Catalog[id].MaxRank)return 0;
            return Mathf.CeilToInt(MetaProgression.Catalog[id].BaseCost*Mathf.Pow(1.65f,Rank(id)));
        }
        public bool Buy(int id){
            int price=Cost(id);if(price<=0||Balance<price)return false;
            Balance-=price;ranks[id]++;return true;
        }
        public bool IsClaimed(int id)=>claimed.Contains(id);
        public bool AwardBatch(int[] ids,int[] amounts){
            if(!Active||ids==null||amounts==null||ids.Length==0||ids.Length!=amounts.Length)return false;
            batch.Clear();
            for(int i=0;i<ids.Length;i++)if(ids[i]<=0||amounts[i]!=1||claimed.Contains(ids[i])||!batch.Add(ids[i])){batch.Clear();return false;}
            int credited=(int)Math.Min(MetaProgression.MaximumCoins-(long)Balance,batch.Count);
            foreach(int id in batch)claimed.Add(id);batch.Clear();
            Balance+=credited;Earned=(int)Math.Min(MetaProgression.MaximumCoins,(long)Earned+credited);return true;
        }
        public bool SetBalanceForTesting(int amount){if(!Active)return false;Balance=Mathf.Clamp(amount,0,MetaProgression.MaximumCoins);return true;}
    }
}
