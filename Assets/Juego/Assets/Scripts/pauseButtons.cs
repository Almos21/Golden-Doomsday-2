using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine.SceneManagement;

public class pauseButtons : MonoBehaviour
{

    [SerializeField] GameObject PauseSplash;
    public void pauseButton()   {

        Time.timeScale = 0;
        PauseSplash.SetActive(true);
        
    }

    public void resumeButton()
    {
        PauseSplash.SetActive(false);
        Time.timeScale = 1;
    }

    public void homeButton()
    {
        MMSceneLoadingManager.LoadScene("homeMenu");
        Time.timeScale = 1;
    }

    public void reStartButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;
    }

}
