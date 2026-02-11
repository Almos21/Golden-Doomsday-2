using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenúInicial : MonoBehaviour
{
    
    public void MenúPrincipal()
    {
        SceneManager.LoadScene(0);
    }
    public void Nivel1()
    {
        SceneManager.LoadScene(1);
        
    }
    public void Nivel2()
    {
        SceneManager.LoadScene(2);
    }
    public void Cinemática()
    {
        SceneManager.LoadScene(2);
    }
    public void Salir()
    {
        Debug.Log("Salir...");
        Application.Quit();
        
    }
    public void Pausar()
    {
        Time.timeScale = 0;
    }
    public void Despausar()
    {
        Time.timeScale = 1;
    }


}
