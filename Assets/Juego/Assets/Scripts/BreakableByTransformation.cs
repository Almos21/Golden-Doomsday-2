using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
    [AddComponentMenu("Corgi Engine/Environment/Breakable By Transformation")]
    public class BreakableByTransformation : MonoBehaviour
    {
        [Header("Breakable Settings")]
        [Tooltip("If true, only Player-type characters can break this object")]
        public bool PlayerOnly = true;

        [Tooltip("Prefab instantiated at destruction position (leave empty for none)")]
        public GameObject DestructionFeedback;

        [Tooltip("Sound played when the object breaks")]
        public AudioClip DestructionSound;

        [Tooltip("Delay before Destroy() is called, useful when feedback has animations")]
        public float DestructionDelay = 0f;

        [Header("Debug")]
        [MMReadOnly]
        public bool AlreadyBroken = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBreak(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryBreak(collision.gameObject);
        }

        protected virtual void TryBreak(GameObject other)
        {
            if (AlreadyBroken) return;

            Character character = other.GetComponentInParent<Character>();
            if (character == null) return;

            if (PlayerOnly && character.CharacterType != Character.CharacterTypes.Player) return;

            CharacterTransformation transformation = character.GetComponent<CharacterTransformation>();
            if (transformation == null) return;

            if (!transformation.IsTransformed) return;

            Break(character);
        }

        protected virtual void Break(Character breakingCharacter)
        {
            AlreadyBroken = true;

            if (DestructionFeedback != null)
                Instantiate(DestructionFeedback, transform.position, transform.rotation);

            if (DestructionSound != null)
            {
                MMSoundManagerSoundPlayEvent.Trigger(
                    DestructionSound,
                    MMSoundManager.MMSoundManagerTracks.Sfx,
                    transform.position);
            }

            DisableVisuals();
            Destroy(gameObject, DestructionDelay);
        }

        protected virtual void DisableVisuals()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                r.enabled = false;
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
                c.enabled = false;
        }
    }
}
