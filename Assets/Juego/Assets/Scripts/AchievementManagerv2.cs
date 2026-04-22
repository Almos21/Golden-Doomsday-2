using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;


public class AchievementManagerv2
{
    [SerializeField]
    private MMAchievementList list;
    [SerializeField]
    private List<GameObject> achievementsv2;

    void Start()
    {
        HideAchievementsv2();
    }

    public void Update()
    {
        UpdateAchievementv2State();
    }

    public void HideAchievementsv2()
    {
        foreach (GameObject achievementv2 in achievementsv2)
        {
            achievementv2.SetActive(false);
        }
    }

    public void UpdateAchievementv2State()
    {
        int index = 0;  
        foreach (MMAchievement achievement in MMAchievementManager.AchievementsList)
        {
            if(index<achievementsv2.Count)
            {
                achievementsv2[index++].SetActive(achievement.UnlockedStatus);
            }
        }
    }
}
