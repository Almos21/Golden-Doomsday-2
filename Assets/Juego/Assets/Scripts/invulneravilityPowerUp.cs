using MoreMountains.CorgiEngine;
using System.Collections;
using UnityEngine;
using static sueInvulnerability;

public class invulneravilityPowerUp : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            sueInvulnerability fx = other.GetComponent<sueInvulnerability>();

            if (fx != null)
            {
                fx.ActivateInvulnerability(3f);
                DeactivatePickup();
            }
        }
    }

    void DeactivatePickup()
    {
        gameObject.SetActive(false);
    }
}
