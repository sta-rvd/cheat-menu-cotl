﻿using HarmonyLib;
using Lamb.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using src.Extensions;

namespace cheat_menu;

internal class CultUtils {
    public static bool IsInGame(){
        return SaveAndLoad.Loaded;
    }

    public static void PlayNotification(string message){
        if(NotificationCentre.Instance){
            NotificationCentre.Instance.PlayGenericNotification(message);
        }
    }

    public static void AddInventoryItem(InventoryItem.ITEM_TYPE type, int amount){
        Inventory.AddItem(type, amount, false);
    }

    public static float CalculateCurrentFaith()
    {
        float totalFaith = 0f;
        foreach (ThoughtData thoughtData in CultFaithManager.Thoughts)
        {
            int index = 0;
            float thoughtFaith = -1;
            while (index <= thoughtData.Quantity)
            {
                if(index == 0)
                {
                    thoughtFaith += thoughtData.Modifier;
                } else
                {
                    thoughtFaith += thoughtData.StackModifier;
                }
                index += 1;
            }
            totalFaith += thoughtFaith;
        }
        return totalFaith;
    }

    public static float GetCurrentFaith()
    {
        return CultFaithManager.CurrentFaith;
    }

    public static ThoughtData HasThought(FollowerInfo follower, Thought thoughtType)
    {
        foreach(var thought in follower.Thoughts)
        {
            if(thought.ThoughtType == thoughtType)
            {
                return thought;
            }
        }
        return null;
    }

    public static void RenameCult(Action<string> onNameConfirmed = null)
    {
        UICultNameMenuController cultNameMenuInstance = MonoSingleton<UIManager>.Instance.CultNameMenuTemplate.Instantiate<UICultNameMenuController>();
        cultNameMenuInstance.Show(true);

        cultNameMenuInstance.OnNameConfirmed = delegate (string newName)
        {
            DataManager.Instance.CultName = newName;
        };
        if (onNameConfirmed != null)
        {
            cultNameMenuInstance.OnNameConfirmed = (Action<string>)Action.Combine(cultNameMenuInstance.OnNameConfirmed, onNameConfirmed);
        }
        cultNameMenuInstance.OnHide = delegate () { };
        cultNameMenuInstance.OnHidden = delegate () { };
    }

    public static void TurnFollowerYoung(FollowerInfo follower){
        var thisFollower = CultUtils.GetFollowerFromInfo(follower);
        thisFollower.RemoveCursedState(Thought.OldAge);
        thisFollower.Brain.ClearThought(Thought.OldAge);
        follower.Age = 0;
        follower.OldAge = false;
        thisFollower.Brain.CheckChangeState();
        DataManager.Instance.Followers_Elderly_IDs.Remove(follower.ID);        
        if(thisFollower.Outfit.CurrentOutfit == FollowerOutfitType.Old){
            thisFollower.SetOutfit(FollowerOutfitType.Follower, false, Thought.None);
        }
    }

    public static void TurnFollowerOld(FollowerInfo follower){
        Follower thisFollower = CultUtils.GetFollowerFromInfo(follower);
        CultFaithManager.RemoveThought(Thought.OldAge);
        thisFollower.Brain.ApplyCurseState(Thought.OldAge);
    }

    public static FollowerInfo GetFollowerInfo(Follower follower)
    {
        return follower.Brain._directInfoAccess;
    }

    public static Follower GetFollowerFromInfo(FollowerInfo follower)
    {
        return FollowerManager.FindFollowerByID(follower.ID);
    }


    public static void SetFollowerIllness(FollowerInfo follower, float value)
    {
        follower.Illness = UnityEngine.Mathf.Clamp(value, 0f, 100f);
    }

    public static void ClearAllThoughts()
    {
        CultFaithManager.Thoughts.Clear();
        CultFaithManager.GetFaith(0f, 0f, true, NotificationBase.Flair.Positive, "Cleared follower thoughts!", -1);
    }

    public static void ClearAndAddPositiveFollowerThought()
    {
        CultFaithManager.Thoughts.Clear();
        foreach(var follower in DataManager.Instance.Followers){
            CultFaithManager.AddThought(Thought.TestPositive, follower.ID, 999);
        }
        ThoughtData data = FollowerThoughts.GetData(Thought.TestPositive);
        CultFaithManager.GetFaith(0f, data.Modifier, true, NotificationBase.Flair.Positive, "Cleared follower thoughts and added positive test thougtht!", -1);
    }

    public static void SetFollowerHunger(FollowerInfo follower, float value)
    {
        follower.Satiation = UnityEngine.Mathf.Clamp(value, 0f, 100f);
    }

    public static Follower GetFollower(FollowerInfo followerInfo)
    {
        return FollowerManager.FindFollowerByID(followerInfo.ID);
    }

    public static void KillFollower(Follower follower, bool withNotification = false, bool withAnimation = false)
    {
        NotificationCentre.NotificationType notifType = withNotification ? NotificationCentre.NotificationType.Died : NotificationCentre.NotificationType.None;
        follower.Die(notifType, false, 1, "dead", null, true);
    }

    //Similar to the revive that is performed by ritual but makes sure they aren't ill / hungry
    public static void ReviveFollower(FollowerInfo follower)
    {
        if (DataManager.Instance.Followers_Dead_IDs.Contains(follower.ID))
        {
            DataManager.Instance.Followers_Dead.Remove(follower);
            DataManager.Instance.Followers_Dead_IDs.Remove(follower.ID);

            Follower revivedFollower = FollowerManager.CreateNewFollower(follower, PlayerFarming.Instance.transform.position, false);
            if (follower.Age > follower.LifeExpectancy)
            {
                follower.LifeExpectancy = follower.Age + UnityEngine.Random.Range(20, 30);
            }
            else
            {
                follower.LifeExpectancy += UnityEngine.Random.Range(20, 30);
            }
            revivedFollower.Brain.ResetStats();
            FollowerInfo revivedFollowerInfo = GetFollowerInfo(revivedFollower);
        }
    }

    public static void SpawnFollower(FollowerRole role)
    {
        Follower follower = FollowerManager.CreateNewFollower(PlayerFarming.Location, PlayerFarming.Instance.transform.position, false);
        follower.Brain.Info.FollowerRole = role;
        follower.Brain.Info.Outfit = FollowerOutfitType.Follower;
        follower.SetOutfit(FollowerOutfitType.Follower, false, Thought.None);

        if (role == FollowerRole.Worker)
        {
            follower.Brain.Info.WorkerPriority = WorkerPriority.Rubble;
            follower.Brain.Stats.WorkerBeenGivenOrders = true;
            follower.Brain.CheckChangeTask();
        }

        FollowerInfo newFollowerInfo = GetFollowerInfo(follower);
        SetFollowerIllness(newFollowerInfo, 0f);
        SetFollowerHunger(newFollowerInfo, 100f);
    }

    public static void ModifyFaith(float value, String notifMessage, bool shouldNotify = true)
    {
        NotificationBase.Flair flair = NotificationBase.Flair.Positive;
        float currentFaithValue = CultUtils.GetCurrentFaith();
        if (currentFaithValue > value)
        {
            flair = NotificationBase.Flair.Negative;
        }
        float currentFollowerFaith = CalculateCurrentFaith();
        float staticFaith = currentFaithValue < value ? value - currentFollowerFaith : currentFollowerFaith - value;


        NotificationData data = shouldNotify ? new NotificationData(notifMessage, 0f, -1, flair, new string[] { }) : null;

        CultFaithManager.StaticFaith = staticFaith;
        CultFaithManager.Instance.BarController.SetBarSize(value / 85f, true, true, data);
    }
}

