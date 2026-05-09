using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
    [AddComponentMenu("Corgi Engine/Character/Abilities/Character Transformation")]
    public class CharacterTransformation : CharacterAbility
    {
        public override string HelpBoxText()
        {
            return "Allows the character to transform on button press. While transformed, contact with BreakableByTransformation objects will destroy them. " +
                   "Shares a fuel resource with CharacterJetpackManaged — they cannot be active simultaneously. " +
                   "Assign TransformationStartFeedback and TransformationEndFeedback to GameObjects that hold your transformation animations/VFX.";
        }

        [Header("Transformation Identification")]
        [Tooltip("Nombre único para identificar esta transformación (ej: Oso).")]
        public string TransformationAlias = "Oso";

        [Header("Transformation")]
        [Tooltip("Cooldown before fuel starts refilling after the transformation ends (seconds)")]
        public float TransformationRefuelCooldown = 1f;

        [Tooltip("How fast the fuel refills (multiplier, 1 = real-time)")]
        public float RefuelSpeed = 0.5f;

        [Tooltip("Minimum fuel required to activate the transformation again (0–1 normalized)")]
        [Range(0f, 1f)]
        public float MinimumFuelRequirement = 0.2f;

        [Header("Shared Fuel — Jetpack")]
        [Tooltip("Drag the CharacterJetpackManaged component from this character here to share fuel and prevent simultaneous use.")]
        public CharacterJetpackManaged SharedJetpack;

        [Header("Feedbacks")]
        [Tooltip("GameObject activated when the transformation begins")]
        public GameObject TransformationStartFeedback;

        [Tooltip("GameObject activated when the transformation ends")]
        public GameObject TransformationEndFeedback;

        [Header("Debug")]
        [MMReadOnly]
        public bool IsTransformed = false;

        protected float _transformationStoppedAt = -999f;
        protected const string _transformingAnimationParameterName = "Transforming";
        protected int _transformingAnimationParameter;

        /// <summary>
        /// True si hay suficiente fuel para activar la transformación (usa MinimumFuelRequirement normalizado)
        /// </summary>
        public virtual bool HasEnoughFuel
        {
            get
            {
                if (SharedJetpack == null) return true;
                if (SharedJetpack.JetpackUnlimited) return true;
                float normalized = SharedJetpack.JetpackFuelDurationLeft / SharedJetpack.JetpackFuelDuration;
                return normalized >= MinimumFuelRequirement;
            }
        }

        /// <summary>
        /// True si queda cualquier cantidad de fuel
        /// </summary>
        public virtual bool FuelLeft
        {
            get
            {
                if (SharedJetpack == null) return true;
                if (SharedJetpack.JetpackUnlimited) return true;
                return SharedJetpack.JetpackFuelDurationLeft > 0f;
            }
        }

        /// <summary>
        /// Inicialización: tomamos control exclusivo del refuel del jetpack
        /// </summary>
        protected override void Initialization()
        {
            base.Initialization();

            // Ceder control del refuel al CharacterTransformation para evitar conflictos
            if (SharedJetpack != null)
                SharedJetpack.ExternalFuelControl = true;

            if (TransformationStartFeedback != null)
                TransformationStartFeedback.SetActive(false);
            if (TransformationEndFeedback != null)
                TransformationEndFeedback.SetActive(false);

            IsTransformed = false;
        }

        /// <summary>
        /// Detecta input del jugador para activar/desactivar la transformación
        /// </summary>
        protected override void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (IsTransformed)
                    TransformationStop();
                else
                    TransformationStart();
            }
        }

        /// <summary>
        /// Activa la transformación si se cumplen las condiciones
        /// </summary>
        public virtual void TransformationStart()
        {
            if (!AbilityAuthorized) return;
            if (!HasEnoughFuel) return;
            if (_condition.CurrentState != CharacterStates.CharacterConditions.Normal) return;

            // No transformarse si está volando con el jetpack
            if (SharedJetpack != null &&
                _movement.CurrentState == CharacterStates.MovementStates.Jetpacking)
                return;

            IsTransformed = true;

            PlayAbilityStartFeedbacks();
            TriggerStartFeedbackObject();

            MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.ButtonActivation,
                MMCharacterEvent.Moments.Start);
        }

        /// <summary>
        /// Desactiva la transformación
        /// </summary>
        public virtual void TransformationStop()
        {
            if (!IsTransformed) return;

            IsTransformed = false;
            _transformationStoppedAt = Time.time;

            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
            TriggerEndFeedbackObject();

            MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.ButtonActivation,
                MMCharacterEvent.Moments.End);
        }

        /// <summary>
        /// Corre cada frame: gestiona conflictos con el jetpack, quema fuel y recarga
        /// </summary>
        public override void ProcessAbility()
        {
            base.ProcessAbility();

            // Si está transformado y activa el jetpack, cancelar transformación primero
            if (IsTransformed && SharedJetpack != null &&
                _movement.CurrentState == CharacterStates.MovementStates.Jetpacking)
            {
                TransformationStop();
                return; // No quemar fuel este frame
            }

            BurnFuel();
            Refuel();

            // Detener transformación si se quedó sin fuel
            if (IsTransformed && !FuelLeft)
                TransformationStop();
        }

        /// <summary>
        /// Quema fuel mientras la transformación está activa.
        /// CharacterJetpackManaged.Refuel() está bloqueado, así que no hay conflicto.
        /// </summary>
        protected virtual void BurnFuel()
        {
            if (SharedJetpack == null || SharedJetpack.JetpackUnlimited) return;
            if (!IsTransformed) return;

            SharedJetpack.JetpackFuelDurationLeft =
                Mathf.Max(0f, SharedJetpack.JetpackFuelDurationLeft - Time.deltaTime);

            UpdateSharedFuelBar();
        }

        /// <summary>
        /// Recarga el fuel cuando no hay transformación activa ni jetpack en uso.
        /// Es la única fuente de refuel — CharacterJetpackManaged.Refuel() está deshabilitado.
        /// </summary>
        protected virtual void Refuel()
        {
            if (SharedJetpack == null || SharedJetpack.JetpackUnlimited) return;
            if (IsTransformed) return;
            if (_movement.CurrentState == CharacterStates.MovementStates.Jetpacking) return;
            if (Time.time - _transformationStoppedAt < TransformationRefuelCooldown) return;

            if (SharedJetpack.JetpackFuelDurationLeft < SharedJetpack.JetpackFuelDuration)
            {
                SharedJetpack.JetpackFuelDurationLeft = Mathf.Min(
                    SharedJetpack.JetpackFuelDurationLeft + Time.deltaTime * RefuelSpeed,
                    SharedJetpack.JetpackFuelDuration
                );
                UpdateSharedFuelBar();
            }
        }

        /// <summary>
        /// Actualiza la barra de fuel en la UI (reutiliza la barra del jetpack)
        /// </summary>
        protected virtual void UpdateSharedFuelBar()
        {
            if (!Application.isPlaying) return;
            if (SharedJetpack == null) return;
            if (!GUIManager.HasInstance) return;
            if (_character.CharacterType != Character.CharacterTypes.Player) return;

            GUIManager.Instance.UpdateJetpackBar(
                SharedJetpack.JetpackFuelDurationLeft,
                0f,
                SharedJetpack.JetpackFuelDuration,
                _character.PlayerID);
        }

        protected virtual void TriggerStartFeedbackObject()
        {
            if (TransformationStartFeedback != null)
            {
                TransformationStartFeedback.SetActive(false);
                TransformationStartFeedback.SetActive(true);
            }
            if (TransformationEndFeedback != null)
                TransformationEndFeedback.SetActive(false);
        }

        protected virtual void TriggerEndFeedbackObject()
        {
            if (TransformationEndFeedback != null)
            {
                TransformationEndFeedback.SetActive(false);
                TransformationEndFeedback.SetActive(true);
            }
            if (TransformationStartFeedback != null)
                TransformationStartFeedback.SetActive(false);
        }

        public override void ResetAbility()
        {
            base.ResetAbility();
            IsTransformed = false;

            if (TransformationStartFeedback != null)
                TransformationStartFeedback.SetActive(false);
            if (TransformationEndFeedback != null)
                TransformationEndFeedback.SetActive(false);

            if (_animator != null)
            {
                MMAnimatorExtensions.UpdateAnimatorBool(
                    _animator,
                    _transformingAnimationParameter,
                    false,
                    _character._animatorParameters,
                    _character.PerformAnimatorSanityChecks);
            }
        }

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(
                _transformingAnimationParameterName,
                AnimatorControllerParameterType.Bool,
                out _transformingAnimationParameter);
        }

        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator,
                _transformingAnimationParameter,
                IsTransformed,
                _character._animatorParameters,
                _character.PerformAnimatorSanityChecks);
        }
    }
}