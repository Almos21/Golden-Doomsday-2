using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cinematica : MonoBehaviour
{
    
    void Start()
    {
        StartCoroutine(CambiarEscena()) ;
    }

    IEnumerator CambiarEscena()
    {
       
        yield return new WaitForSeconds(113);
        SceneManager.LoadScene(1);
    }
}
