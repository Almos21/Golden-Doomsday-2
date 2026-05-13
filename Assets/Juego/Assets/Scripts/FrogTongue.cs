using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Attach to a child GameObject of the frog character.
    /// Requires:
    ///   - SpriteRenderer on THIS GameObject (sprite pivoted at LEFT edge)
    ///   - TongueTip child with BoxCollider2D trigger
    /// Orientation is set externally by Character_Frog before calling Extend().
    /// </summary>
    public class FrogTongue : MonoBehaviour
    {
        [Header("Stretch Settings")]
        [Tooltip("Maximum distance the tongue can reach (world units)")]
        public float MaxDistance = 3f;

        [Tooltip("Speed at which the tongue extends and retracts (world units/second)")]
        public float TravelSpeed = 10f;

        [Header("References")]
        [Tooltip("SpriteRenderer on THIS GameObject — sprite must be pivoted at its LEFT edge")]
        public SpriteRenderer TongueRenderer;

        [Tooltip("BoxCollider2D trigger child — the tip")]
        public BoxCollider2D TipCollider;

        // ── public state ──────────────────────────────────────────────────
        [HideInInspector] public bool IsExtending = false;
        [HideInInspector] public bool IsRetracting = false;
        [HideInInspector] public bool IsOut = false;

        // ── events ────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when the tip collider touches something.
        /// Subscribe in Character_Frog or any other system that needs to react.
        /// The Collider2D argument is whatever was hit.
        /// </summary>
        public System.Action<Collider2D> OnTipHit;

        // ── internal ──────────────────────────────────────────────────────
        protected float _currentLength = 0f;
        protected float _spriteNativeWidth;

        // ─────────────────────────────────────────────────────────────────
        protected void Awake()
        {
            if (TongueRenderer == null)
                TongueRenderer = GetComponent<SpriteRenderer>();

            _spriteNativeWidth = (TongueRenderer != null && TongueRenderer.sprite != null)
                ? TongueRenderer.sprite.bounds.size.x
                : 1f;

            SetLength(0f);
            gameObject.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────
        protected void Update()
        {
            if (IsExtending)
            {
                _currentLength += TravelSpeed * Time.deltaTime;

                if (_currentLength >= MaxDistance)
                {
                    _currentLength = MaxDistance;
                    IsExtending = false;
                    IsOut = true;
                    StartRetracting();          // auto-retract after reaching max
                }

                SetLength(_currentLength);
            }
            else if (IsRetracting)
            {
                _currentLength -= TravelSpeed * Time.deltaTime;

                if (_currentLength <= 0f)
                {
                    _currentLength = 0f;
                    IsRetracting = false;
                    IsOut = false;
                    SetLength(0f);
                    gameObject.SetActive(false);
                }
                else
                {
                    SetLength(_currentLength);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        /// <summary>Begin extending. Character_Frog must set the rotation BEFORE calling this.</summary>
        public void Extend()
        {
            if (IsExtending || IsRetracting) return;

            _currentLength = 0f;
            IsExtending = true;
            IsRetracting = false;
            IsOut = false;

            gameObject.SetActive(true);
            SetLength(0f);
        }

        /// <summary>Force retraction from outside (e.g. transformation cancelled mid-tongue).</summary>
        public void ForceRetract()
        {
            if (!gameObject.activeSelf) return;
            StartRetracting();
        }

        // ─────────────────────────────────────────────────────────────────
        protected void StartRetracting()
        {
            IsExtending = false;
            IsRetracting = true;
            IsOut = false;
        }

        // ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// Scales the sprite on X to match the desired length,
        /// then repositions the tip collider at the far end.
        /// </summary>
        protected void SetLength(float length)
        {
            if (TongueRenderer == null) return;

            float scaleX = (length <= 0f || _spriteNativeWidth <= 0f)
                ? 0.0001f
                : length / _spriteNativeWidth;

            // Only touch the X scale — Y and Z stay as authored
            transform.localScale = new Vector3(scaleX,
                                               transform.localScale.y,
                                               transform.localScale.z);

            // Tip collider stays at the right edge of the sprite in LOCAL space.
            // Because TipCollider is a child, its localPosition is in the
            // tongue's own local coords (before scale is applied), so we put
            // it at x = _spriteNativeWidth and the scale does the rest.
            if (TipCollider != null)
                TipCollider.transform.localPosition =
                    new Vector3(_spriteNativeWidth, 0f, 0f);
        }

        // ─────────────────────────────────────────────────────────────────
        // Called by TongueTip relay
        public void OnTipCollision(Collider2D other)
        {
            // Notify any subscribers with the raw collider — they decide what to do
            OnTipHit?.Invoke(other);

            // Always retract on contact
            StartRetracting();
        }
    }
}