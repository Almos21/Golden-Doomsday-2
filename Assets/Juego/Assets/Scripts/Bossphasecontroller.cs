using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Script de control de fases del jefe.
    /// Completar por el compañero encargado de la lógica de transición de fases.
    /// BossController lee CurrentPhase cada frame para ajustar su comportamiento.
    /// </summary>
    public class BossPhaseController : MonoBehaviour
    {
        [Header("Fase Actual")]
        [Tooltip("Fase actual del jefe. 1-4 = fases de combate. 5 = muerte.")]
        [Range(1, 5)]
        public int CurrentPhase = 1;
    }
}