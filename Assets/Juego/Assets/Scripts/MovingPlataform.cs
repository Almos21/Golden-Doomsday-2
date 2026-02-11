using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform puntoA;
    public Transform puntoB;
    public float velocidad = 2f;

    private Vector3 objetivo;

    void Start()
    {
        objetivo = puntoB.position;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, objetivo) < 0.1f)
        {
            objetivo = objetivo == puntoA.position ? puntoB.position : puntoA.position;
        }
    }
    void OnDrawGizmos() //esto es para ver el movimiento y la direccion a la que van las plataformas
    {
        if (puntoA != null && puntoB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(puntoA.position, puntoB.position);
            Gizmos.DrawWireSphere(puntoA.position, 0.3f);
            Gizmos.DrawWireSphere(puntoB.position, 0.3f);
        }
    }
}