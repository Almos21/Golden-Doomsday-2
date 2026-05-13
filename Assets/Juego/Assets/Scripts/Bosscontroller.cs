using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    [AddComponentMenu("Corgi Engine/Boss/Boss Controller")]
    public class BossController : MonoBehaviour
    {
        // ── Referencias ───────────────────────────────────────────────────────────

        [Header("Referencias")]
        public BossPhaseController PhaseController;
        public Animator BossAnimator;
        [Tooltip("Dejar vacío — se busca automáticamente el Character Player de la escena.")]
        public Transform PlayerTransform;

        // ── Puntos de spawn y misil ───────────────────────────────────────────────

        [Header("Puntos de Spawn de Enemigos")]
        public Transform[] EnemySpawnPoints;

        [Header("Puntos de Disparo de Misiles")]
        public Transform[] MissileSpawnPoints;

        // ── Datos por fase ────────────────────────────────────────────────────────

        [Header("Fases (índice 0 = Fase 1, índice 3 = Fase 4)")]
        public BossPhaseData[] Phases = new BossPhaseData[4];

        // ── Movimiento ────────────────────────────────────────────────────────────

        [Header("Movimiento")]
        public float WalkSpeed = 1.5f;
        public float WalkIntervalMin = 4f;
        public float WalkIntervalMax = 8f;
        public float WalkDuration = 1.5f;
        public string WalkingAnimParam = "Walking";

        [Header("Límites de Movimiento")]
        public float MinX = -10f;
        public float MaxX = 10f;

        [Header("Flip de Sprite")]
        public bool FlipSpriteOnWalk = true;
        public Transform SpriteTransform;

        // ── Muerte ────────────────────────────────────────────────────────────────

        [Header("Muerte")]
        public string DeathAnimTrigger = "Death";
        public float DeathDestroyDelay = 3f;

        // ── Object Pool ───────────────────────────────────────────────────────────

        [Header("Object Pool")]
        public int EnemyPoolSizePerPrefab = 5;
        public int MissilePoolSize = 6;

        // ── Animación de ataque ───────────────────────────────────────────────────

        [Header("Animación de Ataque")]
        [Tooltip("Duración en segundos de la animación de ataque. El idle se pausa este tiempo y luego vuelve.")]
        public float AttackAnimationDuration = 1.2f;

        // ── Estado interno ────────────────────────────────────────────────────────

        protected int _currentPhase = 0;
        protected bool _isDead = false;

        protected Coroutine _spawnCoroutine;
        protected Coroutine _missileCoroutine;
        protected Coroutine _walkCoroutine;

        protected Dictionary<GameObject, Queue<GameObject>> _enemyPools
            = new Dictionary<GameObject, Queue<GameObject>>();
        protected Queue<GameObject> _missilePool = new Queue<GameObject>();

        protected List<GameObject> _activeEnemies = new List<GameObject>();
        protected List<GameObject> _activeMissiles = new List<GameObject>();

        // ─────────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────────

        protected virtual void Start()
        {
            if (PhaseController == null)
                PhaseController = GetComponent<BossPhaseController>();

            // Auto-buscar el player si no fue asignado en el inspector
            if (PlayerTransform == null)
                PlayerTransform = FindPlayerTransform();

            if (PlayerTransform == null)
                Debug.LogWarning("[BossController] No se encontró al jugador. Asigná PlayerTransform en el inspector.");
            else
                Debug.Log($"[BossController] Player encontrado: {PlayerTransform.name} en {PlayerTransform.position}");

            InitializePools();
            StartWalkCycle();
            OnPhaseChanged(0);
        }

        protected virtual Transform FindPlayerTransform()
        {
            // Buscar el componente Character de tipo Player — este es el que realmente se mueve
            Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
            foreach (Character c in characters)
                if (c.CharacterType == Character.CharacterTypes.Player)
                {
                    Debug.Log($"[BossController] Character Player encontrado: {c.name} en {c.transform.position}");
                    return c.transform;
                }

            return null;
        }

        protected virtual void Update()
        {
            if (_isDead) return;
            CheckPhaseChange();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Fases
        // ─────────────────────────────────────────────────────────────────────────

        protected virtual void CheckPhaseChange()
        {
            if (PhaseController == null) return;

            int newPhase = PhaseController.CurrentPhase;

            if (newPhase >= 5 && !_isDead)
            {
                TriggerDeath();
                return;
            }

            int newPhaseIndex = newPhase - 1;
            if (newPhaseIndex != _currentPhase)
                OnPhaseChanged(newPhaseIndex);
        }

        protected virtual void OnPhaseChanged(int newPhaseIndex)
        {
            _currentPhase = newPhaseIndex;
            StopAttackCoroutines();

            BossPhaseData data = CurrentPhaseData();
            if (data == null) return;

            if (BossAnimator != null && !string.IsNullOrEmpty(data.PhaseTransitionAnimTrigger))
                BossAnimator.SetTrigger(data.PhaseTransitionAnimTrigger);

            SetIdleActive(data, true);

            _spawnCoroutine = StartCoroutine(SpawnEnemiesLoop());
            _missileCoroutine = StartCoroutine(MissileAttackLoop());
        }

        protected virtual BossPhaseData CurrentPhaseData()
        {
            if (Phases == null || _currentPhase < 0 || _currentPhase >= Phases.Length)
                return null;
            return Phases[_currentPhase];
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Idle — pausar y reanudar
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Activa o desactiva el bool de idle de la fase actual.
        /// Al desactivar, apaga todos los idle bools para evitar conflictos.
        /// </summary>
        protected virtual void SetIdleActive(BossPhaseData data, bool active)
        {
            if (BossAnimator == null || data == null || string.IsNullOrEmpty(data.IdleAnimParam)) return;

            if (data.IdleAnimIsTrigger)
            {
                // Si es trigger solo se dispara al activar, no hay nada que apagar
                if (active) BossAnimator.SetTrigger(data.IdleAnimParam);
            }
            else
            {
                if (!active)
                {
                    // Apagar el idle de la fase actual
                    BossAnimator.SetBool(data.IdleAnimParam, false);
                }
                else
                {
                    // Apagar todos los demás idle bools antes de encender el actual
                    foreach (BossPhaseData phase in Phases)
                    {
                        if (phase == null || string.IsNullOrEmpty(phase.IdleAnimParam)) continue;
                        if (!phase.IdleAnimIsTrigger)
                            BossAnimator.SetBool(phase.IdleAnimParam, false);
                    }
                    BossAnimator.SetBool(data.IdleAnimParam, true);
                }
            }
        }

        /// <summary>
        /// Apaga el idle, dispara el trigger de ataque, espera AttackAnimationDuration
        /// y vuelve a encender el idle. Usar con yield return en las coroutines de ataque.
        /// </summary>
        protected virtual IEnumerator PlayAttackAnimation(BossPhaseData data, string attackTrigger)
        {
            if (BossAnimator == null || string.IsNullOrEmpty(attackTrigger))
                yield break;

            // Pausar idle
            SetIdleActive(data, false);

            // Disparar animación de ataque
            BossAnimator.SetTrigger(attackTrigger);

            // Esperar que termine
            yield return new WaitForSeconds(AttackAnimationDuration);

            // Reanudar idle (si seguimos en la misma fase y no estamos muertos)
            if (!_isDead && CurrentPhaseData() == data)
                SetIdleActive(data, true);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Spawn de enemigos con parábola
        // ─────────────────────────────────────────────────────────────────────────

        protected virtual IEnumerator SpawnEnemiesLoop()
        {
            while (!_isDead)
            {
                yield return new WaitForSeconds(CurrentPhaseData()?.SpawnInterval ?? 5f);

                BossPhaseData data = CurrentPhaseData();
                if (data == null || data.Enemies == null || data.Enemies.Length == 0) continue;

                // Pausa idle → animación de ataque → reanuda idle
                yield return StartCoroutine(PlayAttackAnimation(data, data.SpawnAttackAnimTrigger));

                for (int i = 0; i < data.EnemiesPerWave; i++)
                {
                    Transform spawnPoint = EnemySpawnPoints != null && EnemySpawnPoints.Length > 0
                        ? EnemySpawnPoints[Random.Range(0, EnemySpawnPoints.Length)]
                        : transform;

                    GameObject prefab = data.GetWeightedRandomPrefab();
                    if (prefab == null) continue;

                    SpawnEnemyWithParabola(prefab, spawnPoint.position, data);
                }
            }
        }

        protected virtual void SpawnEnemyWithParabola(GameObject prefab, Vector3 origin, BossPhaseData data)
        {
            GameObject enemy = GetFromEnemyPool(prefab);
            enemy.transform.position = origin;
            enemy.transform.SetParent(null);
            enemy.SetActive(true);
            _activeEnemies.Add(enemy);

            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float dirX = Random.value > 0.5f ? 1f : -1f;
                rb.linearVelocity = new Vector2(dirX * data.EnemyLaunchForceX, data.EnemyLaunchForceY);
            }

            StartCoroutine(WatchEnemyAndReturn(enemy, prefab));
        }

        protected virtual IEnumerator WatchEnemyAndReturn(GameObject enemy, GameObject prefab)
        {
            yield return null;
            while (enemy != null && enemy.activeSelf)
                yield return new WaitForSeconds(0.5f);

            if (enemy != null)
            {
                _activeEnemies.Remove(enemy);
                ReturnToEnemyPool(prefab, enemy);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Ataque de misiles
        // ─────────────────────────────────────────────────────────────────────────

        protected virtual IEnumerator MissileAttackLoop()
        {
            while (!_isDead)
            {
                yield return new WaitForSeconds(CurrentPhaseData()?.MissileInterval ?? 7f);

                BossPhaseData data = CurrentPhaseData();
                if (data == null || data.MissilePrefab == null) continue;

                // Pausa idle → animación de ataque → reanuda idle
                yield return StartCoroutine(PlayAttackAnimation(data, data.MissileAttackAnimTrigger));

                if (MissileSpawnPoints != null)
                    foreach (Transform point in MissileSpawnPoints)
                    {
                        if (point == null) continue;
                        FireMissile(data, point.position);
                    }
            }
        }

        protected virtual void FireMissile(BossPhaseData data, Vector3 origin)
        {
            GameObject missile = GetFromMissilePool(data.MissilePrefab);
            missile.transform.position = origin;
            missile.transform.SetParent(null);

            Vector2 direction = Vector2.right;
            if (PlayerTransform != null)
                direction = ((Vector2)(PlayerTransform.position - origin)).normalized;

            BossProjectile projectile = missile.GetComponent<BossProjectile>();
            if (projectile != null)
            {
                projectile.Direction = direction;
                projectile.Speed = data.MissileSpeed;
            }
            else
            {
                Rigidbody2D rb = missile.GetComponent<Rigidbody2D>();
                if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                    rb.linearVelocity = direction * data.MissileSpeed;
            }

            missile.SetActive(true);
            _activeMissiles.Add(missile);

            StartCoroutine(WatchMissileAndReturn(missile));
        }

        protected virtual IEnumerator WatchMissileAndReturn(GameObject missile)
        {
            yield return null;
            while (missile != null && missile.activeSelf)
                yield return new WaitForSeconds(0.2f);

            if (missile != null)
            {
                _activeMissiles.Remove(missile);
                ReturnToMissilePool(missile);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Movimiento
        // ─────────────────────────────────────────────────────────────────────────

        protected virtual void StartWalkCycle()
        {
            if (_walkCoroutine != null) StopCoroutine(_walkCoroutine);
            _walkCoroutine = StartCoroutine(WalkCycleLoop());
        }

        protected virtual IEnumerator WalkCycleLoop()
        {
            while (!_isDead)
            {
                yield return new WaitForSeconds(Random.Range(WalkIntervalMin, WalkIntervalMax));
                if (_isDead) yield break;

                // Re-buscar player por si no estaba al inicio
                if (PlayerTransform == null)
                    PlayerTransform = FindPlayerTransform();

                if (PlayerTransform == null) continue;

                float distToPlayer = Mathf.Abs(PlayerTransform.position.x - transform.position.x);
                if (distToPlayer < 1f) continue;

                if (BossAnimator != null) BossAnimator.SetBool(WalkingAnimParam, true);
                yield return StartCoroutine(WalkTowardPlayer());
                if (BossAnimator != null) BossAnimator.SetBool(WalkingAnimParam, false);
            }
        }

        protected virtual IEnumerator WalkTowardPlayer()
        {
            if (PlayerTransform == null) yield break;

            // Fijar dirección UNA vez al inicio — no recalcular cada frame
            float rawDiff = PlayerTransform.position.x - transform.position.x;
            float dirX = Mathf.Sign(rawDiff);

            Debug.Log($"[BossController] Caminando hacia player. Boss X:{transform.position.x:F1} Player X:{PlayerTransform.position.x:F1} dirX:{dirX}");

            if (dirX == 0f) yield break;

            float elapsed = 0f;
            while (elapsed < WalkDuration && !_isDead)
            {
                bool atLeftLimit = dirX < 0 && transform.position.x <= MinX;
                bool atRightLimit = dirX > 0 && transform.position.x >= MaxX;

                if (atLeftLimit || atRightLimit) yield break;

                Vector3 newPos = transform.position + new Vector3(dirX * WalkSpeed * Time.deltaTime, 0f, 0f);
                newPos.x = Mathf.Clamp(newPos.x, MinX, MaxX);
                transform.position = newPos;

                if (FlipSpriteOnWalk)
                {
                    Transform target = SpriteTransform != null ? SpriteTransform : transform;
                    Vector3 scale = target.localScale;
                    scale.x = Mathf.Abs(scale.x) * (dirX < 0 ? -1f : 1f);
                    target.localScale = scale;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Muerte
        // ─────────────────────────────────────────────────────────────────────────

        protected virtual void TriggerDeath()
        {
            _isDead = true;
            StopAttackCoroutines();
            if (_walkCoroutine != null) StopCoroutine(_walkCoroutine);

            // Apagar idle antes de la animación de muerte
            BossPhaseData data = CurrentPhaseData();
            if (data != null) SetIdleActive(data, false);

            foreach (GameObject e in _activeEnemies) if (e != null) { e.SetActive(false); Destroy(e); }
            foreach (GameObject m in _activeMissiles) if (m != null) { m.SetActive(false); Destroy(m); }
            _activeEnemies.Clear();
            _activeMissiles.Clear();

            if (BossAnimator != null && !string.IsNullOrEmpty(DeathAnimTrigger))
                BossAnimator.SetTrigger(DeathAnimTrigger);

            StartCoroutine(DestroyAfterDelay());
        }

        protected virtual IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(DeathDestroyDelay);
            Destroy(gameObject);
        }

        protected virtual void StopAttackCoroutines()
        {
            if (_spawnCoroutine != null) { StopCoroutine(_spawnCoroutine); _spawnCoroutine = null; }
            if (_missileCoroutine != null) { StopCoroutine(_missileCoroutine); _missileCoroutine = null; }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Object Pool
        // ─────────────────────────────────────────────────────────────────────────

        protected virtual void InitializePools()
        {
            HashSet<GameObject> seen = new HashSet<GameObject>();
            GameObject firstMissilePrefab = null;

            foreach (BossPhaseData phase in Phases)
            {
                if (phase == null) continue;

                if (phase.Enemies != null)
                {
                    foreach (EnemySpawnEntry entry in phase.Enemies)
                    {
                        if (entry == null || entry.Prefab == null || seen.Contains(entry.Prefab)) continue;
                        seen.Add(entry.Prefab);

                        Queue<GameObject> q = new Queue<GameObject>();
                        for (int i = 0; i < EnemyPoolSizePerPrefab; i++)
                            q.Enqueue(CreatePooledInstance(entry.Prefab));
                        _enemyPools[entry.Prefab] = q;
                    }
                }

                if (phase.MissilePrefab != null && firstMissilePrefab == null)
                    firstMissilePrefab = phase.MissilePrefab;
            }

            if (firstMissilePrefab != null)
                for (int i = 0; i < MissilePoolSize; i++)
                    _missilePool.Enqueue(CreatePooledInstance(firstMissilePrefab));
        }

        protected virtual GameObject CreatePooledInstance(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            instance.SetActive(false);
            return instance;
        }

        protected virtual GameObject GetFromEnemyPool(GameObject prefab)
        {
            if (_enemyPools.TryGetValue(prefab, out Queue<GameObject> q) && q.Count > 0)
                return q.Dequeue();
            Debug.LogWarning($"[BossController] Pool vacío para {prefab.name}. Creando instancia extra.");
            return Instantiate(prefab);
        }

        protected virtual void ReturnToEnemyPool(GameObject prefab, GameObject instance)
        {
            instance.SetActive(false);
            instance.transform.SetParent(transform);
            if (_enemyPools.TryGetValue(prefab, out Queue<GameObject> q))
                q.Enqueue(instance);
            else
                Destroy(instance);
        }

        protected virtual GameObject GetFromMissilePool(GameObject prefab)
        {
            if (_missilePool.Count > 0) return _missilePool.Dequeue();
            Debug.LogWarning("[BossController] Pool de misiles vacío. Creando instancia extra.");
            return Instantiate(prefab);
        }

        protected virtual void ReturnToMissilePool(GameObject instance)
        {
            instance.SetActive(false);
            instance.transform.SetParent(transform);
            _missilePool.Enqueue(instance);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (EnemySpawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (Transform t in EnemySpawnPoints)
                {
                    if (t == null) continue;
                    Gizmos.DrawWireSphere(t.position, 0.25f);
                    UnityEditor.Handles.Label(t.position + Vector3.up * 0.4f, "Enemy Spawn");
                }
            }

            if (MissileSpawnPoints != null)
            {
                Gizmos.color = Color.red;
                foreach (Transform t in MissileSpawnPoints)
                {
                    if (t == null) continue;
                    Gizmos.DrawWireSphere(t.position, 0.25f);
                    UnityEditor.Handles.Label(t.position + Vector3.up * 0.4f, "Missile Point");
                }
            }

            Gizmos.color = Color.yellow;
            float y = transform.position.y;
            Gizmos.DrawLine(new Vector3(MinX, y - 2f, 0f), new Vector3(MinX, y + 2f, 0f));
            Gizmos.DrawLine(new Vector3(MaxX, y - 2f, 0f), new Vector3(MaxX, y + 2f, 0f));
            UnityEditor.Handles.Label(new Vector3(MinX, y + 2.2f, 0f), "Min X");
            UnityEditor.Handles.Label(new Vector3(MaxX, y + 2.2f, 0f), "Max X");
        }
#endif
    }
}