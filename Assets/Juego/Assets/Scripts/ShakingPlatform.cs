using System.Collections;
using UnityEngine;

/// <summary>
/// Plataforma que tiembla al contacto con el Player,
/// desactiva su collider a los 10 segundos y cae antes de desactivarse.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ShakingPlatform : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("Intensidad del temblor del sprite")]
    public float shakeIntensity = 0.05f;

    [Tooltip("Velocidad del temblor")]
    public float shakeSpeed = 30f;

    [Header("Timing")]
    [Tooltip("Segundos hasta que el collider se desactiva y la plataforma cae")]
    public float timeBeforeFall = 10f;

    [Tooltip("Tiempo que tarda en caer/desaparecer antes de desactivarse")]
    public float fallDuration = 1.5f;

    [Header("Fall Settings")]
    [Tooltip("Velocidad de caída cuando el collider se desactiva")]
    public float fallSpeed = 5f;

    // Referencias internas
    private BoxCollider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private Transform _spriteTransform;   // Sólo el sprite se mueve
    private Vector3 _spriteOriginalLocalPos;

    private bool _triggered = false;
    private Coroutine _shakeCoroutine;

    void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Si el SpriteRenderer está en un hijo, usamos ese Transform para el shake.
        // Si está en el mismo GameObject, creamos un hijo vacío que lo contenga.
        if (_spriteRenderer != null)
        {
            _spriteTransform = _spriteRenderer.transform;
            _spriteOriginalLocalPos = _spriteTransform.localPosition;
        }
        else
        {
            Debug.LogWarning("[ShakingPlatform] No se encontró SpriteRenderer en el GameObject ni en sus hijos.");
        }
    }

    // ── Detección de contacto ────────────────────────────────────────────────

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!_triggered && col.gameObject.CompareTag("Player"))
            ActivatePlatform();
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!_triggered && col.CompareTag("Player"))
            ActivatePlatform();
    }

    // ── Lógica principal ─────────────────────────────────────────────────────

    void ActivatePlatform()
    {
        _triggered = true;
        _shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        // --- FASE 1: Temblar durante timeBeforeFall segundos ---
        while (elapsed < timeBeforeFall)
        {
            elapsed += Time.deltaTime;

            // Temblor sólo en el sprite (offset senoidal en X e Y)
            float offsetX = Mathf.Sin(elapsed * shakeSpeed)        * shakeIntensity;
            float offsetY = Mathf.Sin(elapsed * shakeSpeed * 1.3f) * shakeIntensity * 0.5f;
            _spriteTransform.localPosition = _spriteOriginalLocalPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // Restaurar posición del sprite antes de caer
        _spriteTransform.localPosition = _spriteOriginalLocalPos;

        // --- FASE 2: Desactivar collider y caer ---
        _collider.enabled = false;

        float fallElapsed = 0f;
        while (fallElapsed < fallDuration)
        {
            fallElapsed += Time.deltaTime;

            // Mover todo el GameObject hacia abajo
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            // Fade opcional del sprite
            if (_spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, fallElapsed / fallDuration);
                Color c = _spriteRenderer.color;
                c.a = alpha;
                _spriteRenderer.color = c;
            }

            yield return null;
        }

        // --- FASE 3: Desactivar el GameObject (NO se destruye) ---
        gameObject.SetActive(false);
    }

    // ── Utilidad: resetear la plataforma ─────────────────────────────────────

    /// <summary>
    /// Llama este método para reutilizar la plataforma (p.ej. al reaparecer el nivel).
    /// </summary>
    public void ResetPlatform()
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);

        _triggered = false;
        _collider.enabled = true;

        if (_spriteRenderer != null)
        {
            Color c = _spriteRenderer.color;
            c.a = 1f;
            _spriteRenderer.color = c;
            _spriteTransform.localPosition = _spriteOriginalLocalPos;
        }

        gameObject.SetActive(true);
    }
}
