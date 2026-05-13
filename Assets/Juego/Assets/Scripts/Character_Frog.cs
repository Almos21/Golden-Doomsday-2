using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
    [AddComponentMenu("Corgi Engine/Character/Abilities/Character Frog")]
    public class Character_Frog : CharacterAbility
    {
        public override string HelpBoxText()
        {
            return "Frog transformation (key 3). Fully independent and safe with other abilities.";
        }

        [Header("Frog Settings")]
        public float FrogJumpMultiplier = 1.8f;

        [Header("Fuel (Optional)")]
        public CharacterJetpackManaged SharedJetpack;

        [Range(0f, 1f)]
        public float MinimumFuelRequirement = 0.2f;

        [Header("Feedbacks")]
        public GameObject TransformationStartFeedback;
        public GameObject TransformationEndFeedback;

        [Header("Debug")]
        [MMReadOnly]
        public bool IsTransformed = false;

        protected CharacterJump _characterJump;
        protected float _originalJumpHeight = -1f;
        protected float _lastTransformStop = -999f;

        // 🔥 referencia a otra transformación (ej: oso)
        protected CharacterTransformation _otherTransformation;

        // ✅ PARAMETRO NUEVO (clave)
        protected const string _frogAnimationParameterName = "FrogTransforming";
        protected int _frogAnimationParameter;

        #region Initialization

        protected override void Initialization()
        {
            base.Initialization();

            _characterJump = _character.GetComponent<CharacterJump>();

            // Buscar otra transformación en el personaje
            _otherTransformation = _character.GetComponent<CharacterTransformation>();

            IsTransformed = false;

            if (TransformationStartFeedback != null) TransformationStartFeedback.SetActive(false);
            if (TransformationEndFeedback != null) TransformationEndFeedback.SetActive(false);
        }

        #endregion

        #region Input

        protected override void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (IsTransformed)
                    StopFrog();
                else
                    StartFrog();
            }
        }

        #endregion

        #region ProcessAbility

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            // ❌ cancelar si empieza a volar
            if (IsTransformed && _movement.CurrentState == CharacterStates.MovementStates.Flying)
            {
                StopFrog();
                return;
            }

            if (IsTransformed)
            {
                HandleFuel();
            }
        }

        #endregion

        #region Frog Logic

        public virtual void StartFrog()
        {
            if (!AbilityAuthorized) return;

            if (_condition.CurrentState != CharacterStates.CharacterConditions.Normal)
                return;

            // ❌ bloquear si el oso está activo
            if (_otherTransformation != null && _otherTransformation.IsTransformed)
                return;

            // ❌ respetar estados importantes
            if (_movement.CurrentState == CharacterStates.MovementStates.Flying ||
                _movement.CurrentState == CharacterStates.MovementStates.Dashing ||
                _movement.CurrentState == CharacterStates.MovementStates.Gripping)
            {
                return;
            }

            // ❌ revisar fuel mínimo
            if (SharedJetpack != null && !SharedJetpack.JetpackUnlimited)
            {
                float normalized = SharedJetpack.JetpackFuelDurationLeft / SharedJetpack.JetpackFuelDuration;
                if (normalized < MinimumFuelRequirement)
                    return;
            }

            IsTransformed = true;

            ApplyJumpBoost();

            PlayAbilityStartFeedbacks();
            TriggerStartFeedbackObject();
        }

        public virtual void StopFrog()
        {
            if (!IsTransformed) return;

            IsTransformed = false;
            _lastTransformStop = Time.time;

            RemoveJumpBoost();

            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
            TriggerEndFeedbackObject();
        }

        #endregion

        #region Fuel

        protected virtual void HandleFuel()
        {
            if (SharedJetpack == null) return;
            if (SharedJetpack.JetpackUnlimited) return;

            SharedJetpack.JetpackFuelDurationLeft =
                Mathf.Max(0f, SharedJetpack.JetpackFuelDurationLeft - Time.deltaTime);

            // si se queda sin fuel
            if (SharedJetpack.JetpackFuelDurationLeft <= 0f)
            {
                StopFrog();
            }
        }

        #endregion

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

        #region Animator

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(
                _frogAnimationParameterName,
                AnimatorControllerParameterType.Bool,
                out _frogAnimationParameter
            );
        }

        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator,
                _frogAnimationParameter,
                IsTransformed,
                _character._animatorParameters,
                _character.PerformAnimatorSanityChecks
            );
        }

        #endregion

        #region Reset

        public override void ResetAbility()
        {
            base.ResetAbility();

            StopFrog();

            if (_animator != null)
            {
                MMAnimatorExtensions.UpdateAnimatorBool(
                    _animator,
                    _frogAnimationParameter,
                    false,
                    _character._animatorParameters,
                    _character.PerformAnimatorSanityChecks
                );
            }
        }

        #endregion
    }
}
