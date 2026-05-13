using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Agregar este componente al prefab del misil del jefe.
    /// Se mueve solo en la dirección indicada, hace daño al tocar al jugador y desaparece.
    /// No requiere configuración de Rigidbody2D — se mueve por Transform.
    /// </summary>
    [AddComponentMenu("Corgi Engine/Boss/Boss Projectile")]
    public class BossProjectile : MonoBehaviour
    {
        [Header("Daño")]
        [Tooltip("Daño que hace al tocar al jugador")]
        public int Damage = 10;

        [Tooltip("Fuerza de knockback aplicada al jugador al recibir el impacto")]
        public float KnockbackForce = 5f;

        [Header("Vida útil")]
        [Tooltip("Segundos antes de desaparecer si no toca nada")]
        public float Lifetime = 6f;

        [Tooltip("Layer del jugador para detectar colisión")]
        public LayerMask PlayerLayer;

        // Seteado por BossController al disparar
        [HideInInspector] public Vector2 Direction;
        [HideInInspector] public float Speed;

        protected float _spawnTime;

        private void OnEnable()
        {
            _spawnTime = Time.time;
        }

        private void Update()
        {
            // Mover en la dirección fijada al disparar
            transform.position += (Vector3)(Direction * Speed * Time.deltaTime);

            // Rotar el sprite para que apunte hacia donde va
            if (Direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // Autodestruirse por tiempo
            if (Time.time - _spawnTime >= Lifetime)
                Disappear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Verificar si es el jugador por layer o por componente Character
            Character character = other.GetComponentInParent<Character>();
            if (character == null) return;
            if (character.CharacterType != Character.CharacterTypes.Player) return;

            // Aplicar daño
            Health health = character.GetComponent<Health>();
            if (health != null)
            {
                Vector3 knockbackDir = new Vector3(Direction.x, KnockbackForce, 0f);
                health.Damage(Damage, gameObject, 0.1f, 0.5f, knockbackDir);
            }

            Disappear();
        }

        protected virtual void Disappear()
        {
            // Desactivar en lugar de destruir para que el pool del BossController lo recupere
            gameObject.SetActive(false);
        }
    }
}
