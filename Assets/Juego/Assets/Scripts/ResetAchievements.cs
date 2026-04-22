using MoreMountains.Tools;
using UnityEngine;
using System.Collections;


public class ResetAchievements : MonoBehaviour
{
    public void ResetAchievementsMethod()
    {
        MMAchievementManager.ResetAchievements("AchievementListV2");
    }
}
