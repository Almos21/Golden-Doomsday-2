using UnityEngine;

using UnityEngine;

public class Moneda : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Suma al score del GameManager
            GameManager.instance.score++;

            // Destruye la moneda
            Destroy(gameObject);
        }
    }
}