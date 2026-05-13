using System.Collections;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    public class BossGemHitbox : MonoBehaviour
    {
        [Header("Referencia al controlador de fases")]
        public BossPhaseController PhaseController;

        [Header("Cooldown")]
        public float HitCooldown = 10f;

        [Header("Color de impacto")]
        public Color HitColor = new Color(1f, 0.15f, 0.15f); // rojo

        protected SpriteRenderer _spriteRenderer;
        protected Color _originalColor;
        protected Collider2D _collider;
        protected bool _onCooldown = false;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();

            if (_spriteRenderer != null)
                _originalColor = _spriteRenderer.color;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_onCooldown) return;

            if (other.gameObject.name.Contains("TongueTip"))
            {
                if (PhaseController != null)
                {
                    PhaseController.CurrentPhase += 1;
                    Debug.Log("FASE AUMENTADA A: " + PhaseController.CurrentPhase);
                }

                StartCoroutine(HitCooldownRoutine());
            }
        }

        protected IEnumerator HitCooldownRoutine()
        {
            _onCooldown = true;

            // Desactivar collider y cambiar color
            if (_collider != null) _collider.enabled = false;
            if (_spriteRenderer != null) _spriteRenderer.color = HitColor;

            yield return new WaitForSeconds(HitCooldown);

            // Restaurar
            if (_collider != null) _collider.enabled = true;
            if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;

            _onCooldown = false;
        }
    }
}