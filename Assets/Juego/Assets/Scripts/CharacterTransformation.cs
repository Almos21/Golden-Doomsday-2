using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Add this component to a character and it'll be able to transform by pressing a button.
    /// While transformed, the character can break special objects on contact.
    /// This ability shares a fuel resource with CharacterJetpack — they cannot be used simultaneously.
    /// 
    /// Animator parameters: Transforming (bool)
    /// 
    /// SETUP:
    /// - Assign TransformationStartFeedback: a child GameObject that holds your start animation/VFX
    /// - Assign TransformationEndFeedback: a child GameObject that holds your end animation/VFX
    /// - Both feedback objects are activated/deactivated automatically
    /// - Wire up the shared fuel via the SharedFuel field (same CharacterJetpack component on this character)
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/Abilities/Character Transformation")]
    public class CharacterTransformation : CharacterAbility
    {
        public override string HelpBoxText()
        {
            return "Allows the character to transform on button press. While transformed, contact with BreakableByTransformation objects will destroy them. " +
                   "Shares a fuel resource with CharacterJetpack — they cannot be active simultaneously. " +
                   "Assign TransformationStartFeedback and TransformationEndFeedback to GameObjects that hold your transformation animations/VFX.";
        }

        // ─────────────────────────────────────────────
        //  Inspector fields
        // ─────────────────────────────────────────────

        [Header("Transformation")]

        /// How long the transformation lasts at maximum fuel (seconds)
        [Tooltip("How long the transformation lasts at maximum fuel (seconds)")]
        public float TransformationDuration = 5f;

        /// Cooldown before fuel starts refilling after the transformation ends (seconds)
        [Tooltip("Cooldown before fuel starts refilling after the transformation ends (seconds)")]
        public float TransformationRefuelCooldown = 1f;

        /// How fast the fuel refills (multiplier, 1 = real-time)
        [Tooltip("How fast the fuel refills (multiplier, 1 = real-time)")]
        public float RefuelSpeed = 0.5f;

        /// Minimum fuel required in the tank before the transformation can be activated again
        [Tooltip("Minimum fuel required to activate the transformation again (0–1 normalized)")]
        [Range(0f, 1f)]
        public float MinimumFuelRequirement = 0.2f;

        [Header("Shared Fuel — Jetpack")]

        /// Reference to the CharacterJetpack on this character.
        /// Both abilities draw from the same fuel pool: JetpackFuelDurationLeft / JetpackFuelDuration.
        [Tooltip("Drag the CharacterJetpack component from this character here to share fuel and prevent simultaneous use.")]
        public CharacterJetpack SharedJetpack;

        [Header("Feedbacks")]

        /// GameObject activated at the START of the transformation (put your animator/VFX here)
        [Tooltip("GameObject activated when the transformation begins")]
        public GameObject TransformationStartFeedback;

        /// GameObject activated at the END of the transformation (put your animator/VFX here)
        [Tooltip("GameObject activated when the transformation ends")]
        public GameObject TransformationEndFeedback;

        [Header("Debug")]

        /// Whether the character is currently transformed
        [MMReadOnly]
        public bool IsTransformed = false;

        // ─────────────────────────────────────────────
        //  Private state
        // ─────────────────────────────────────────────

        protected float _transformationStoppedAt = -999f;

        // Animator parameter
        protected const string _transformingAnimationParameterName = "Transforming";
        protected int _transformingAnimationParameter;

        // ─────────────────────────────────────────────
        //  Properties
        // ─────────────────────────────────────────────

        /// True when there is enough shared fuel to start a transformation
        public virtual bool HasEnoughFuel
        {
            get
            {
                if (SharedJetpack == null) return true; // no shared fuel configured → unlimited
                if (SharedJetpack.JetpackUnlimited) return true;
                float normalized = SharedJetpack.JetpackFuelDurationLeft / SharedJetpack.JetpackFuelDuration;
                return normalized >= MinimumFuelRequirement;
            }
        }

        /// True as long as the shared fuel pool has any remaining fuel
        public virtual bool FuelLeft
        {
            get
            {
                if (SharedJetpack == null) return true;
                if (SharedJetpack.JetpackUnlimited) return true;
                return SharedJetpack.JetpackFuelDurationLeft > 0f;
            }
        }

        // ─────────────────────────────────────────────
        //  Initialization
        // ─────────────────────────────────────────────

        protected override void Initialization()
        {
            base.Initialization();

            // Make sure feedback objects start hidden
            if (TransformationStartFeedback != null)
                TransformationStartFeedback.SetActive(false);
            if (TransformationEndFeedback != null)
                TransformationEndFeedback.SetActive(false);

            IsTransformed = false;
        }

        // ─────────────────────────────────────────────
        //  Input
        // ─────────────────────────────────────────────

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

        // ─────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────

        /// <summary>Activates the transformation.</summary>
        public virtual void TransformationStart()
        {
            if (!AbilityAuthorized) return;
            if (!HasEnoughFuel) return;
            if (_condition.CurrentState != CharacterStates.CharacterConditions.Normal) return;

            // Prevent simultaneous use with the jetpack
            if (SharedJetpack != null &&
                _movement.CurrentState == CharacterStates.MovementStates.Jetpacking)
                return;

            IsTransformed = true;
            // We don't override movement state — transformation is a condition overlay, not a movement state

            PlayAbilityStartFeedbacks();
            TriggerStartFeedbackObject();

            MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.ButtonActivation,
                MMCharacterEvent.Moments.Start);
        }

        /// <summary>Deactivates the transformation.</summary>
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

        // ─────────────────────────────────────────────
        //  Process (every frame)
        // ─────────────────────────────────────────────

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            BurnFuel();
            Refuel();

            // Force-stop if fuel runs out while transformed
            if (IsTransformed && !FuelLeft)
                TransformationStop();

            // Force-stop if jetpack activates while transformed (check via shared jetpack's owner)
            if (IsTransformed && SharedJetpack != null)
            {
                CharacterStates.MovementStates currentMove = _movement.CurrentState;
                if (currentMove == CharacterStates.MovementStates.Jetpacking)
                    TransformationStop();
            }
        }

        // ─────────────────────────────────────────────
        //  Fuel
        // ─────────────────────────────────────────────

        /// <summary>Drains shared fuel while the transformation is active.</summary>
        protected virtual void BurnFuel()
        {
            if (SharedJetpack == null || SharedJetpack.JetpackUnlimited) return;
            if (!IsTransformed) return;

            SharedJetpack.JetpackFuelDurationLeft -= Time.deltaTime;
            SharedJetpack.JetpackFuelDurationLeft =
                Mathf.Max(0f, SharedJetpack.JetpackFuelDurationLeft);

            // Sync jetpack's GUI bar
            UpdateSharedFuelBar();
        }

        /// <summary>
        /// Refuels after the cooldown.
        /// NOTE: Both this ability and CharacterJetpack call Refuel independently.
        /// To avoid double-refueling, the Refuel logic here only runs when NEITHER ability is active.
        /// The jetpack's own Refuel() still runs on its side — if you want a single source of truth,
        /// simply disable the Refuel block in CharacterJetpack and let only this script handle it,
        /// or vice-versa.
        /// </summary>
        protected virtual void Refuel()
        {
            if (SharedJetpack == null || SharedJetpack.JetpackUnlimited) return;

            // Neither ability should be active
            if (IsTransformed) return;
            if (_movement.CurrentState == CharacterStates.MovementStates.Jetpacking) return;

            // Wait for cooldown
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

        // ─────────────────────────────────────────────
        //  Feedback helpers
        // ─────────────────────────────────────────────

        /// <summary>Activates the start feedback object and auto-deactivates it after a frame.</summary>
        protected virtual void TriggerStartFeedbackObject()
        {
            if (TransformationStartFeedback != null)
            {
                TransformationStartFeedback.SetActive(false); // reset first so animators re-trigger
                TransformationStartFeedback.SetActive(true);
            }
            if (TransformationEndFeedback != null)
                TransformationEndFeedback.SetActive(false);
        }

        /// <summary>Activates the end feedback object.</summary>
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

        // ─────────────────────────────────────────────
        //  Reset / Death
        // ─────────────────────────────────────────────

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

        // ─────────────────────────────────────────────
        //  Animator
        // ─────────────────────────────────────────────

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
