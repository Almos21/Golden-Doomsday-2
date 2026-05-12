using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Prefab del enemigo")]
        public GameObject Prefab;

        [Tooltip("Peso de spawn relativo. Mayor = sale más seguido. Ej: 1 = normal, 0.5 = mitad de veces, 2 = doble.")]
        [Range(0.1f, 5f)]
        public float SpawnWeight = 1f;
    }

    [System.Serializable]
    public class BossPhaseData
    {
        [Header("Identificación")]
        public string PhaseName = "Fase 1";

        [Header("Spawn de Enemigos")]
        [Tooltip("Enemigos que puede soltar en esta fase, cada uno con su propio peso de aparición.")]
        public EnemySpawnEntry[] Enemies;

        [Tooltip("Cantidad de enemigos por oleada")]
        public int EnemiesPerWave = 2;

        [Tooltip("Segundos entre oleadas de enemigos")]
        public float SpawnInterval = 5f;

        [Tooltip("Fuerza horizontal de la parábola al soltar enemigos")]
        public float EnemyLaunchForceX = 3f;

        [Tooltip("Fuerza vertical de la parábola al soltar enemigos")]
        public float EnemyLaunchForceY = 8f;

        [Header("Ataque de Misiles")]
        [Tooltip("Prefab del misil que se dispara en esta fase")]
        public GameObject MissilePrefab;

        [Tooltip("Segundos entre ataques de misil")]
        public float MissileInterval = 7f;

        [Tooltip("Velocidad inicial del misil")]
        public float MissileSpeed = 10f;

        [Header("Animaciones")]
        [Tooltip("Nombre del trigger del Animator para el ataque de spawn de esta fase")]
        public string SpawnAttackAnimTrigger = "SpawnAttack";

        [Tooltip("Nombre del trigger del Animator para el ataque de misil de esta fase")]
        public string MissileAttackAnimTrigger = "MissileAttack";

        [Tooltip("Nombre del trigger del Animator para la transición a esta fase")]
        public string PhaseTransitionAnimTrigger = "PhaseTransition";

        [Tooltip("Nombre del parámetro bool/trigger de Idle en el Animator para esta fase. Ej: 'IdleFase1'")]
        public string IdleAnimParam = "Idle";

        [Tooltip("Si es true, IdleAnimParam se trata como Trigger. Si es false, como Bool.")]
        public bool IdleAnimIsTrigger = false;

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Elige un prefab al azar respetando los pesos de cada entrada.
        /// </summary>
        public GameObject GetWeightedRandomPrefab()
        {
            if (Enemies == null || Enemies.Length == 0) return null;

            float totalWeight = 0f;
            foreach (EnemySpawnEntry entry in Enemies)
                if (entry != null && entry.Prefab != null)
                    totalWeight += entry.SpawnWeight;

            if (totalWeight <= 0f) return null;

            float roll = Random.Range(0f, totalWeight);
            float accumulated = 0f;

            foreach (EnemySpawnEntry entry in Enemies)
            {
                if (entry == null || entry.Prefab == null) continue;
                accumulated += entry.SpawnWeight;
                if (roll <= accumulated)
                    return entry.Prefab;
            }

            // Fallback: último prefab válido
            for (int i = Enemies.Length - 1; i >= 0; i--)
                if (Enemies[i] != null && Enemies[i].Prefab != null)
                    return Enemies[i].Prefab;

            return null;
        }
    }
}