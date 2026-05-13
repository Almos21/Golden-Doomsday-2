using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
    [AddComponentMenu("Corgi Engine/Character/Abilities/Character Frog")]
    public class Character_Frog : CharacterAbility
    {
        public override string HelpBoxText()
        {
            return "Transforms the character into a FROG (key 3). " +
                   "While transformed: jump height is boosted and the tongue ability is available. " +
                   "Shares a fuel resource with CharacterJetpackManaged. " +
                   "Assign TransformationStartFeedback and TransformationEndFeedback for VFX.";
        }

        [Header("Transformation Identification")]
        [Tooltip("Nombre único para identificar esta transformación.")]
        public string TransformationAlias = "Rana";

        [Header("Transformation")]
        [Tooltip("Cooldown before fuel starts refilling after the transformation ends (seconds)")]
        public float TransformationRefuelCooldown = 1f;

        [Tooltip("How fast the fuel refills (multiplier, 1 = real-time)")]
        public float RefuelSpeed = 0.5f;

        [Tooltip("Minimum fuel required to activate the transformation again (0–1 normalized)")]
        [Range(0f, 1f)]
        public float MinimumFuelRequirement = 0.2f;

        [Header("Frog — Jump Boost")]
        [Tooltip("Multiplier applied to the character's JumpHeight while transformed (e.g. 1.8 = 80% higher)")]
        public float FrogJumpMultiplier = 1.8f;

        [Header("Shared Fuel — Jetpack")]
        [Tooltip("Drag the CharacterJetpackManaged component from this character here.")]
        public CharacterJetpackManaged SharedJetpack;

        [Header("Feedbacks")]
        [Tooltip("GameObject activated when the transformation begins")]
        public GameObject TransformationStartFeedback;

        [Tooltip("GameObject activated when the transformation ends")]
        public GameObject TransformationEndFeedback;

        [Header("Debug")]
        [MMReadOnly]
        public bool IsTransformed = false;

        // ── internal state ────────────────────────────────────────────────
        protected float _transformationStoppedAt = -999f;
        protected float _originalJumpHeight = -1f;
        protected CharacterJump _characterJump;

        // ── animator parameter names ──────────────────────────────────────
        protected const string _transformingAnimationParameterName = "Transforming";
        protected const string _frogIdleAnimationParameterName = "FrogIdle";
        protected const string _frogJumpAnimationParameterName = "FrogJump";
        protected const string _frogTongueAnimationParameterName = "FrogTongue";

        protected int _transformingAnimationParameter;
        protected int _frogIdleAnimationParameter;
        protected int _frogJumpAnimationParameter;
        protected int _frogTongueAnimationParameter;

        protected bool _isTongueActive = false;

        // ─────────────────────────────────────────────────────────────────
        #region Properties

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

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Initialization

        protected override void Initialization()
        {
            base.Initialization();

            _characterJump = _character.GetComponent<CharacterJump>();

            if (SharedJetpack != null)
                SharedJetpack.ExternalFuelControl = true;

            if (TransformationStartFeedback != null)
                TransformationStartFeedback.SetActive(false);
            if (TransformationEndFeedback != null)
                TransformationEndFeedback.SetActive(false);

            IsTransformed = false;
            _isTongueActive = false;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Input

        protected override void HandleInput()
        {
            if (!IsTransformed)
            {
                if (Input.GetKeyDown(KeyCode.Alpha3))
                    TransformationStart();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TransformationStop();
                return;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region ProcessAbility

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (IsTransformed && SharedJetpack != null &&
                _movement.CurrentState == CharacterStates.MovementStates.Jetpacking)
            {
                TransformationStop();
                return;
            }

            BurnFuel();
            Refuel();

            if (IsTransformed && !FuelLeft)
                TransformationStop();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Transformation On / Off

        public virtual void TransformationStart()
        {
            if (!AbilityAuthorized) return;
            if (!HasEnoughFuel) return;
            if (_condition.CurrentState != CharacterStates.CharacterConditions.Normal) return;

            if (SharedJetpack != null &&
                _movement.CurrentState == CharacterStates.MovementStates.Jetpacking)
                return;

            IsTransformed = true;

            ApplyJumpBoost();
            PlayAbilityStartFeedbacks();
            TriggerStartFeedbackObject();

            MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.ButtonActivation,
                MMCharacterEvent.Moments.Start);
        }

        public virtual void TransformationStop()
        {
            if (!IsTransformed) return;

            if (_isTongueActive)
                TongueStop();

            IsTransformed = false;
            _transformationStoppedAt = Time.time;

            RemoveJumpBoost();
            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
            TriggerEndFeedbackObject();

            MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.ButtonActivation,
                MMCharacterEvent.Moments.End);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Jump Boost

        protected virtual void ApplyJumpBoost()
        {
            if (_characterJump == null) return;

            if (_originalJumpHeight < 0f)
                _originalJumpHeight = _characterJump.JumpHeight;

            _characterJump.JumpHeight = _originalJumpHeight * FrogJumpMultiplier;
        }

        protected virtual void RemoveJumpBoost()
        {
            if (_characterJump == null) return;
            if (_originalJumpHeight < 0f) return;

            _characterJump.JumpHeight = _originalJumpHeight;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Tongue

        public virtual void TongueStart()
        {
            if (!IsTransformed) return;
            if (_isTongueActive) return;

            _isTongueActive = true;

            // ── TODO: tongue logic goes here ──────────────────────────────
        }

        public virtual void TongueStop()
        {
            if (!_isTongueActive) return;

            _isTongueActive = false;

            // ── TODO: tongue cleanup goes here ────────────────────────────
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Fuel

        protected virtual void BurnFuel()
        {
            if (SharedJetpack == null || SharedJetpack.JetpackUnlimited) return;
            if (!IsTransformed) return;

            SharedJetpack.JetpackFuelDurationLeft =
                Mathf.Max(0f, SharedJetpack.JetpackFuelDurationLeft - Time.deltaTime);

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
                SharedJetpack.JetpackFuelDurationLeft = Mathf.Min(
                    SharedJetpack.JetpackFuelDurationLeft + Time.deltaTime * RefuelSpeed,
                    SharedJetpack.JetpackFuelDuration
                );
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

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Feedbacks

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

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Animator

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(_transformingAnimationParameterName,
                AnimatorControllerParameterType.Bool, out _transformingAnimationParameter);

            RegisterAnimatorParameter(_frogIdleAnimationParameterName,
                AnimatorControllerParameterType.Bool, out _frogIdleAnimationParameter);

            RegisterAnimatorParameter(_frogJumpAnimationParameterName,
                AnimatorControllerParameterType.Bool, out _frogJumpAnimationParameter);

            RegisterAnimatorParameter(_frogTongueAnimationParameterName,
                AnimatorControllerParameterType.Bool, out _frogTongueAnimationParameter);
        }

        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, _transformingAnimationParameter, IsTransformed,
                _character._animatorParameters, _character.PerformAnimatorSanityChecks);

            if (!IsTransformed)
            {
                MMAnimatorExtensions.UpdateAnimatorBool(
                    _animator, _frogIdleAnimationParameter, false,
                    _character._animatorParameters, _character.PerformAnimatorSanityChecks);
                MMAnimatorExtensions.UpdateAnimatorBool(
                    _animator, _frogJumpAnimationParameter, false,
                    _character._animatorParameters, _character.PerformAnimatorSanityChecks);
                MMAnimatorExtensions.UpdateAnimatorBool(
                    _animator, _frogTongueAnimationParameter, false,
                    _character._animatorParameters, _character.PerformAnimatorSanityChecks);
                return;
            }

            bool tongue = _isTongueActive;
            bool jumping = (_movement.CurrentState == CharacterStates.MovementStates.Jumping ||
                            _movement.CurrentState == CharacterStates.MovementStates.DoubleJumping ||
                            _movement.CurrentState == CharacterStates.MovementStates.Falling);
            bool idle = !tongue && !jumping;

            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, _frogTongueAnimationParameter, tongue,
                _character._animatorParameters, _character.PerformAnimatorSanityChecks);
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, _frogJumpAnimationParameter, jumping && !tongue,
                _character._animatorParameters, _character.PerformAnimatorSanityChecks);
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, _frogIdleAnimationParameter, idle,
                _character._animatorParameters, _character.PerformAnimatorSanityChecks);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────
        #region Reset

        public override void ResetAbility()
        {
            base.ResetAbility();

            if (_isTongueActive) TongueStop();
            RemoveJumpBoost();

            IsTransformed = false;
            _isTongueActive = false;

            if (TransformationStartFeedback != null)
                TransformationStartFeedback.SetActive(false);
            if (TransformationEndFeedback != null)
                TransformationEndFeedback.SetActive(false);

            if (_animator != null)
            {
                MMAnimatorExtensions.UpdateAnimatorBool(
                    _animator, _transformingAnimationParameter, false,
                    _character._animatorParameters, _character.PerformAnimatorSanityChecks);
                MMAnimatorExtensions.UpdateAnimatorBool(
                    _animator, _frogIdleAnimationParameter, false,
                    _character._animatorParameters, _character.PerformAnimatorSanityChecks);
                MMAnimatorExtensions.UpdateAnimatorBool(
                    _animator, _frogJumpAnimationParameter, false,
                    _character._animatorParameters, _character.PerformAnimatorSanityChecks);
                MMAnimatorExtensions.UpdateAnimatorBool(
                    _animator, _frogTongueAnimationParameter, false,
                    _character._animatorParameters, _character.PerformAnimatorSanityChecks);
            }
        }

        #endregion
    }
}