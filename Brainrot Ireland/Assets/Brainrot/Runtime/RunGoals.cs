using System;
using UnityEngine;

namespace Brainrot
{
    [Flags]
    public enum TutorialActions {None=0,Move=1,Attack=2,Dash=4,Heavy=8,All=15}

    /// <summary>Actions are remembered in any order; hints never ask to repeat an action already performed.</summary>
    public sealed class TutorialProgress
    {
        public TutorialActions Actions {get;private set;}
        public bool Complete=>Actions==TutorialActions.All;
        public void Reset(){Actions=TutorialActions.None;}
        public void Observe(bool moved,bool attacked,bool dashed,bool heavy)
        {
            if(moved)Actions|=TutorialActions.Move;
            if(attacked)Actions|=TutorialActions.Attack;
            if(dashed)Actions|=TutorialActions.Dash;
            if(heavy)Actions|=TutorialActions.Heavy;
        }
        public string Hint(bool touch,bool charging=false,bool readyToRelease=false)
        {
            if((Actions&TutorialActions.Move)==0)return touch?"ДВИГАЙ ДЖОЙСТИК — ОБОЙДИ ОСТРОВ":"WASD — ПОДВИГАЙСЯ ПО ОСТРОВУ";
            if((Actions&TutorialActions.Attack)==0)return touch?"НАЖМИ «УДАР» — УДАРЬ БИТОЙ":"НАЖМИ И ОТПУСТИ ЛКМ — УДАР БИТОЙ";
            if((Actions&TutorialActions.Dash)==0)return touch?"ДВИГАЙСЯ И НАЖМИ «РЫВОК», ЧТОБЫ УКЛОНИТЬСЯ":"ДВИГАЙСЯ И НАЖМИ ПРОБЕЛ — РЫВОК";
            if((Actions&TutorialActions.Heavy)==0){
                if(charging&&readyToRelease)return touch?"ОТПУСТИ «УДАР» — СИЛЬНЫЙ ВЗМАХ!":"ОТПУСТИ ЛКМ — СИЛЬНЫЙ ВЗМАХ!";
                return touch?"УДЕРЖИ «УДАР» ДО ПОЛНОГО ЗАМАХА И ОТПУСТИ":"УДЕРЖИ ЛКМ ДО ПОЛНОГО ЗАМАХА И ОТПУСТИ";
            }
            return "";
        }
    }

    [Flags]
    public enum RunChallenge {None=0,FiftyDefeated=1,WaveFive=2,CleanWave=4,WaveTen=8}

    /// <summary>A restored partial wave cannot award the no-damage challenge; the next complete wave can.</summary>
    public sealed class RunChallengeProgress
    {
        int previousWave,damageAtStart;
        bool cleanEligible;
        public void Reset(int wave,int damageEvents,bool restored)
        {previousWave=Mathf.Max(1,wave);damageAtStart=Mathf.Max(0,damageEvents);cleanEligible=!restored;}
        public bool IsCleanCandidate(int damageEvents)=>cleanEligible&&damageEvents==damageAtStart;
        public RunChallenge ObserveCompletedWave(bool cleared,int damageEvents)=>cleared&&IsCleanCandidate(damageEvents)?RunChallenge.CleanWave:RunChallenge.None;
        public RunChallenge Observe(int wave,int kills,int damageEvents)
        {
            wave=Mathf.Max(1,wave);damageEvents=Mathf.Max(0,damageEvents);RunChallenge result=RunChallenge.None;
            if(kills>=50)result|=RunChallenge.FiftyDefeated;
            if(wave>=5)result|=RunChallenge.WaveFive;
            if(wave>=10)result|=RunChallenge.WaveTen;
            if(damageEvents<damageAtStart)cleanEligible=false;
            if(wave!=previousWave){
                if(wave==previousWave+1&&cleanEligible&&damageEvents==damageAtStart)result|=RunChallenge.CleanWave;
                previousWave=wave;damageAtStart=damageEvents;cleanEligible=true;
            }
            return result;
        }
        public static string Title(RunChallenge challenge)
        {
            if((challenge&RunChallenge.WaveTen)!=0)return "ДЕСЯТАЯ ВОЛНА";
            if((challenge&RunChallenge.WaveFive)!=0)return "ПЯТАЯ ВОЛНА";
            if((challenge&RunChallenge.FiftyDefeated)!=0)return "50 ПОБЕЖДЁННЫХ";
            if((challenge&RunChallenge.CleanWave)!=0)return "ВОЛНА БЕЗ УРОНА";
            return "";
        }
    }

    /// <summary>Compact contextual guidance and optional goals. No gameplay pause, XP or stat rewards.</summary>
    public sealed class RunGoals:MonoBehaviour
    {
        GameManager game;
        readonly TutorialProgress tutorial=new TutorialProgress();
        readonly RunChallengeProgress challenges=new RunChallengeProgress();
        Vector3 previousPosition;
        float travelled,previousHeavyCooldown;
        int shownKills=-1,shownWave=-1,shownMask=-1,shownBest=-1;
        bool tutorialWasCompleted;
        int recentMask;float completionUntil;
        static readonly RunChallenge[] cardOrder={RunChallenge.FiftyDefeated,RunChallenge.WaveFive,RunChallenge.CleanWave,RunChallenge.WaveTen};
        public int EarnedThisRunMask {get;private set;}
        public void RestoreRunProgress(int mask){EarnedThisRunMask=mask&15;}
        public string CompletedThisRunSummary {
            get {
                var text=new System.Text.StringBuilder();
                foreach(var challenge in cardOrder)if((EarnedThisRunMask&(int)challenge)!=0){if(text.Length>0)text.Append("  ·  ");text.Append(RunChallengeProgress.Title(challenge));}
                return text.Length>0?text.ToString():"НОВЫЕ ИСПЫТАНИЯ ЖДУТ В СЛЕДУЮЩЕМ ЗАБЕГЕ";
            }
        }
        public string GoalText {get;private set;}="";
        public string TutorialText {get;private set;}="";
        public string WaveEventText {get;private set;}="";
        public bool TutorialActive=>!SaveSystem.Data.TutorialCompleted;

        public void Initialize(GameManager owner){game=owner;ResetRun(true);}
        public void ResetRun(bool restored=false)
        {
            tutorial.Reset();travelled=0;shownKills=shownWave=shownMask=shownBest=-1;EarnedThisRunMask=recentMask=0;completionUntil=0;
            tutorialWasCompleted=SaveSystem.Data.TutorialCompleted;
            var player=game!=null?game.Player:null;
            previousPosition=player!=null?player.transform.position:Vector3.zero;
            previousHeavyCooldown=player!=null&&player.Combat!=null?player.Combat.HeavyCooldownRemaining:0;
            challenges.Reset(game!=null?game.Wave:1,player!=null?player.DamageTakenCount:0,restored);
            RefreshText();
        }
        public void SkipTutorial()
        {
            if(!SaveSystem.Data.TutorialCompleted){SaveSystem.Data.TutorialCompleted=true;SaveSystem.Save();}
            tutorialWasCompleted=true;TutorialText="";
        }
        public void ReplayTutorial()
        {
            SaveSystem.Data.TutorialCompleted=false;SaveSystem.Save();
            tutorial.Reset();travelled=0;tutorialWasCompleted=false;
            var player=game!=null?game.Player:null;
            previousPosition=player!=null?player.transform.position:Vector3.zero;
            previousHeavyCooldown=player!=null&&player.Combat!=null?player.Combat.HeavyCooldownRemaining:0;
            RefreshText();
        }
        public void Tick(float dt)
        {
            if(game==null||!game.IsPlaying||game.Player==null||dt<=0)return;
            var player=game.Player;var combat=player.Combat;
            float cooldown=combat!=null?combat.HeavyCooldownRemaining:0;
            travelled+=Mathf.Min(2,Vector3.Distance(previousPosition,player.transform.position));previousPosition=player.transform.position;
            if(!SaveSystem.Data.TutorialCompleted){
                tutorial.Observe(travelled>=1.5f,combat!=null&&combat.IsAttacking,player.IsDashing,cooldown>previousHeavyCooldown+.2f);
                if(tutorial.Complete){SaveSystem.Data.TutorialCompleted=true;SaveSystem.Save();tutorialWasCompleted=true;game.Notify("ОСНОВЫ ОСВОЕНЫ! ТЕПЕРЬ ПОБЕЙ СВОЙ РЕКОРД.",3);}
            }
            previousHeavyCooldown=cooldown;
            var earned=challenges.Observe(game.Wave,game.Kills,player.DamageTakenCount);
            var waves=game.Spawner!=null?game.Spawner.Waves:null;
            earned|=challenges.ObserveCompletedWave(waves!=null&&waves.Resting&&!waves.WaitingForClear,player.DamageTakenCount);
            EarnedThisRunMask|=(int)earned;
            int newly=(int)earned&~SaveSystem.Data.UnlockedChallengeMask;
            bool save=false;
            if(newly!=0){SaveSystem.Data.UnlockedChallengeMask|=newly;recentMask=newly;completionUntil=Time.unscaledTime+4;save=true;GameAudio.PlayChallenge();}
            if(game.Wave>SaveSystem.Data.BestWave){SaveSystem.Data.BestWave=game.Wave;save=true;}
            if(save)SaveSystem.Save();
            RefreshText();
        }
        /// <summary>Stable priorities, at most three cards. Completion stays visible briefly before the next goal takes its place.</summary>
        public ChallengeCardData GetCard(int slot)
        {
            if(game==null||slot<0||slot>=3)return default;
            int mask=SaveSystem.Data.UnlockedChallengeMask,visibleIndex=0;
            foreach(var challenge in cardOrder){
                bool complete=(mask&(int)challenge)!=0;
                bool celebrating=complete&&(recentMask&(int)challenge)!=0&&Time.unscaledTime<completionUntil;
                if(complete&&!celebrating)continue;
                if(visibleIndex++==slot)return BuildCard(challenge,complete);
            }
            if(visibleIndex>0)return default;
            // A completed collection remains readable instead of leaving a mysterious empty heading.
            if(slot==0)return new ChallengeCardData {Id=16,Title="МАСТЕР ОСТРОВА",Detail="ВСЕ ИСПЫТАНИЯ ВЫПОЛНЕНЫ",Current=1,Target=1,Complete=true,Icon="MenuRecords"};
            return default;
        }
        ChallengeCardData BuildCard(RunChallenge challenge,bool complete)
        {
            var data=new ChallengeCardData {Id=(int)challenge,Title=RunChallengeProgress.Title(challenge),Complete=complete,Icon="MenuRecords"};
            switch(challenge){
                case RunChallenge.FiftyDefeated:data.Current=Mathf.Min(50,game.Kills);data.Target=50;data.Detail="ПОБЕДИ 50 ПРОТИВНИКОВ";data.Icon="HeavyIcon";break;
                case RunChallenge.WaveFive:data.Current=Mathf.Min(5,game.Wave);data.Target=5;data.Detail="ДОБЕРИСЬ ДО ПЯТОЙ ВОЛНЫ";break;
                case RunChallenge.WaveTen:data.Current=Mathf.Min(10,game.Wave);data.Target=10;data.Detail="ДОБЕРИСЬ ДО ДЕСЯТОЙ ВОЛНЫ";break;
                default:
                    bool eligible=game.Player!=null&&challenges.IsCleanCandidate(game.Player.DamageTakenCount);
                    data.Current=eligible&&game.Spawner!=null?Mathf.Min(99,Mathf.FloorToInt(game.Spawner.WaveCompletion*100)):0;
                    data.Target=100;data.Detail=eligible?"ЗАВЕРШИ ВОЛНУ БЕЗ УРОНА":"ПОПРОБУЙ В СЛЕДУЮЩЕЙ ВОЛНЕ";data.Icon="Heart";data.Percent=true;break;
            }
            if(complete)data.Current=data.Target;
            return data;
        }
        void RefreshText()
        {
            if(game==null)return;
            int kills=game.Kills,wave=game.Wave,mask=SaveSystem.Data.UnlockedChallengeMask,best=SaveSystem.Data.BestWave;
            if(kills!=shownKills||wave!=shownWave||mask!=shownMask||best!=shownBest){
                shownKills=kills;shownWave=wave;shownMask=mask;shownBest=best;
                if((mask&(int)RunChallenge.FiftyDefeated)==0)GoalText="ЦЕЛЬ: 50 ПОБЕД — "+Mathf.Min(50,kills)+" / 50";
                else if((mask&(int)RunChallenge.WaveFive)==0)GoalText="ЦЕЛЬ: ДОБЕРИСЬ ДО ВОЛНЫ 5 — "+wave+" / 5";
                else if((mask&(int)RunChallenge.CleanWave)==0)GoalText="ЦЕЛЬ: ПРОЙДИ ВОЛНУ БЕЗ УРОНА";
                else if((mask&(int)RunChallenge.WaveTen)==0)GoalText="ЦЕЛЬ: ДОБЕРИСЬ ДО ВОЛНЫ 10 — "+wave+" / 10";
                else GoalText="ЛИЧНЫЙ РЕКОРД: ВОЛНА "+best+" · СЛЕДУЮЩИЙ РУБЕЖ: "+((wave/5+1)*5);
                var plan=WaveChallenges.ForWave(wave);WaveEventText=plan.Title;
            }
            var player=game.Player;var combat=player!=null?player.Combat:null;
            bool touch=Application.isMobilePlatform;
            if(player!=null&&player.IsCaptured)TutorialText=touch?"БЫСТРО НАЖИМАЙ «УДАР», ЧТОБЫ СБРОСИТЬ ОСЬМИНОГА!":"БЫСТРО НАЖИМАЙ ЛКМ, ЧТОБЫ СБРОСИТЬ ОСЬМИНОГА!";
            else if(tutorialWasCompleted||SaveSystem.Data.TutorialCompleted)TutorialText="";
            else if(combat!=null&&!combat.HeavyReady&&(tutorial.Actions&(TutorialActions.Move|TutorialActions.Attack|TutorialActions.Dash))==(TutorialActions.Move|TutorialActions.Attack|TutorialActions.Dash))TutorialText="СИЛЬНЫЙ УДАР ВОССТАНАВЛИВАЕТСЯ — УКЛОНЯЙСЯ";
            else TutorialText=tutorial.Hint(touch,combat!=null&&combat.IsCharging,combat!=null&&combat.ChargeProgress>=1);
        }
    }
}
