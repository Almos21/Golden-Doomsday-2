using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using UnityEngine.UI;

public class CambioIcono : MonoBehaviour
{
    public GameObject player1;
    public GameObject player2;
    public Sprite iconoSué;
    public Sprite iconoArmadillo;
    public Sprite iconoGuacamaya;
    public Image UIImage;
    public bool guaca=false;

    private void Start()
    {
        UIImage = GameObject.Find("Icono").GetComponent<Image>();
    }
    void Update()
    {
        if (player1.activeInHierarchy && !player2.activeInHierarchy)
        {
            if (guaca == true)
            {
                UIImage.sprite = iconoGuacamaya;
            }
            else
            {
                UIImage.sprite = iconoSué;
            }
        }
        else if (player2.activeInHierarchy && !player1.activeInHierarchy)
        {

            UIImage.sprite = iconoArmadillo;
        }
    }
    
}
