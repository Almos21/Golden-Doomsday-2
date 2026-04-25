using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
    public class BreakableByTransformation : MonoBehaviour
    {
        [Header("Settings")]
        public string TargetTransformationAlias = "Oso";

        // Usamos OnTrigger para mayor sensibilidad en Corgi Engine
        protected virtual void OnTriggerStay2D(Collider2D collider)
        {
            CheckAndBreak(collider.gameObject);
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            CheckAndBreak(collision.gameObject);
        }

        protected virtual void CheckAndBreak(GameObject PotentialPlayer)
        {
            // Buscamos al personaje
            Character character = PotentialPlayer.GetComponentInParent<Character>();
            if (character == null) return;

            // Buscamos la habilidad
            CharacterTransformation transformation = character.FindAbility<CharacterTransformation>();

            if (transformation != null)
            {
                // DEBUG: Descomenta la línea de abajo para ver en consola si detecta al personaje
                // Debug.Log($"Tocado por: {character.name}. Transformed: {transformation.IsTransformed}. Alias: {transformation.TransformationAlias}");

                if (transformation.IsTransformed && transformation.TransformationAlias == TargetTransformationAlias)
                {
                    ExecuteBreak();
                }
            }
        }

        protected virtual void ExecuteBreak()
        {
            // Desactivamos colisionadores inmediatamente
            foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;

            // Desactivamos visuales
            if (GetComponent<Renderer>() != null) GetComponent<Renderer>().enabled = false;
            foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;

            Debug.Log("¡OBJETO DESTRUIDO POR OSO!");
            Destroy(gameObject, 0.1f);
        }
    }
}