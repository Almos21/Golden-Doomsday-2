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
                   "Shares a fuel resource with CharacterJetpack — they cannot be active simultaneously. " +
                   "Assign TransformationStartFeedback and TransformationEndFeedback to GameObjects that hold your transformation animations/VFX.";
        }

        [Header("Transformation Identification")]
        [Tooltip("Nombre único para identificar esta transformación (ej: Oso). El objeto rompible debe tener este mismo nombre.")]
        public string TransformationAlias = "Oso";

        [Header("Transformation Settings")]
        [Tooltip("How long the transformation lasts at maximum fuel (seconds)")]
        public float TransformationDuration = 5f;

        [Tooltip("Cooldown before fuel starts refilling after the transformation ends (seconds)")]
        public float TransformationRefuelCooldown = 1f;

        [Tooltip("How fast the fuel refills (multiplier, 1 = real-time)")]
        public float RefuelSpeed = 0.5f;

        [Tooltip("Minimum fuel required to activate the transformation again (0–1 normalized)")]
        [Range(0f, 1f)]
        public float MinimumFuelRequirement = 0.2f;

        [Header("Shared Fuel — Jetpack")]
        [Tooltip("Drag the CharacterJetpack component from this character here to share fuel and prevent simultaneous use.")]
        public CharacterJetpack SharedJetpack;

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

        public virtual bool FuelLeft
        {
            get
            {
                if (SharedJetpack == null) return true;
                if (SharedJetpack.JetpackUnlimited) return true;
                return SharedJetpack.JetpackFuelDurationLeft > 0f;
            }
        }

        protected override void Initialization()
        {
            base.Initialization();

            if (TransformationStartFeedback != null)
                TransformationStartFeedback.SetActive(false);
            if (TransformationEndFeedback != null)
                TransformationEndFeedback.SetActive(false);

            IsTransformed = false;
        }

        protected override void HandleInput()
        {
            // Puedes cambiar KeyCode.Alpha1 por el botón que prefieras
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (IsTransformed)
                    TransformationStop();
                else
                    TransformationStart();
            }
        }

        public virtual void TransformationStart()
        {
            if (!AbilityAuthorized) return;
            if (!HasEnoughFuel) return;
            if (_condition.CurrentState != CharacterStates.CharacterConditions.Normal) return;

            if (SharedJetpack != null &&
                _movement.CurrentState == CharacterStates.MovementStates.Jetpacking)
                return;

            IsTransformed = true;

            PlayAbilityStartFeedbacks();
            TriggerStartFeedbackObject();

            MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.ButtonActivation,
                MMCharacterEvent.Moments.Start);
        }

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

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            BurnFuel();
            Refuel();

            if (IsTransformed && !FuelLeft)
                TransformationStop();

            if (IsTransformed && SharedJetpack != null)
            {
                CharacterStates.MovementStates currentMove = _movement.CurrentState;
                if (currentMove == CharacterStates.MovementStates.Jetpacking)
                    TransformationStop();
            }
        }

        protected virtual void BurnFuel()
        {
            if (SharedJetpack == null || SharedJetpack.JetpackUnlimited) return;
            if (!IsTransformed) return;

            SharedJetpack.JetpackFuelDurationLeft -= Time.deltaTime;
            SharedJetpack.JetpackFuelDurationLeft =
                Mathf.Max(0f, SharedJetpack.JetpackFuelDurationLeft);

            UpdateSharedFuelBar();
        }

        protected virtual void Refuel()
        {
            if (SharedJetpack == null || SharedJetpack.JetpackUnlimited) return;

            if (IsTransformed) return;
            if (_movement.CurrentState == CharacterStates.MovementStates.Jetpacking) return;

            if (Time.time - _transformationStoppedAt < TransformationRefuelCooldown) return;

            if (SharedJetpack.JetpackFuelDurationLeft < SharedJetpack.JetpackFuelDuration)
            {
                SharedJetpack.JetpackFuelDurationLeft +=
                    Time.deltaTime * RefuelSpeed;

                SharedJetpack.JetpackFuelDurationLeft =
                    Mathf.Min(SharedJetpack.JetpackFuelDurationLeft,
                              SharedJetpack.JetpackFuelDuration);

                UpdateSharedFuelBar();
            }
        }

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