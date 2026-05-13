using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Attach to the tip child GameObject (the one with the BoxCollider2D trigger).
    /// Relays OnTriggerEnter2D up to the FrogTongue parent.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TongueTip : MonoBehaviour
    {
        protected FrogTongue _tongue;

        protected void Awake()
        {
            _tongue = GetComponentInParent<FrogTongue>();
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        protected void OnTriggerEnter2D(Collider2D other)
        {
            if (_tongue == null) return;

            // Ignore any collider that belongs to the frog itself
            if (other.transform.IsChildOf(_tongue.transform.parent)) return;

            _tongue.OnTipCollision(other);
        }
    }
}