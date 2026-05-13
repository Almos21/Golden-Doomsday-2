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

        [Header("Tongue")]
        public float TongueSpeed = 20f;
        public float MaxTongueLength = 5f;
        public float TongueWidth = 0.15f;
        public Color TongueColor = new Color(1f, 0.4f, 0.6f); // rosado

        [Header("Fuel (Optional)")]
        public CharacterJetpackManaged SharedJetpack;

        [Range(0f, 1f)]
        public float MinimumFuelRequirement = 0.2f;

        [Header("Feedbacks")]
        public GameObject TransformationStartFeedback;
        public GameObject TransformationEndFeedback;

        [Header("Debug")]
        [MMReadOnly] public bool IsTransformed = false;
        [MMReadOnly] public bool TongueActive = false;

        protected CharacterJump _characterJump;
        protected float _originalJumpHeight = -1f;
        protected float _lastTransformStop = -999f;

        protected CharacterTransformation _otherTransformation;

        protected const string _frogAnimationParameterName = "FrogTransforming";
        protected int _frogAnimationParameter;

        // ── Lengua ──────────────────────────────────────────
        protected float _currentLength = 0f;
        protected bool _extending = false;

        // LineRenderer para dibujar la lengua
        protected LineRenderer _lineRenderer;

        // Trigger collider en la punta
        protected GameObject _tongueTip;
        protected CircleCollider2D _tipCollider;

        #region Initialization

        protected override void Initialization()
        {
            base.Initialization();

            _characterJump = _character.GetComponent<CharacterJump>();
            _otherTransformation = _character.GetComponent<CharacterTransformation>();

            IsTransformed = false;

            CreateTongueVisuals();

            if (TransformationStartFeedback != null) TransformationStartFeedback.SetActive(false);
            if (TransformationEndFeedback != null) TransformationEndFeedback.SetActive(false);
        }

        void CreateTongueVisuals()
        {
            // ── LineRenderer ────────────────────────────────
            GameObject lineObj = new GameObject("TongueLine");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;

            _lineRenderer = lineObj.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = TongueWidth;
            _lineRenderer.endWidth = TongueWidth;
            _lineRenderer.useWorldSpace = true;

            // Material sin textura, color sólido
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = TongueColor;
            _lineRenderer.endColor = TongueColor;
            _lineRenderer.sortingOrder = 5;

            _lineRenderer.enabled = false;

            // ── Tip con trigger collider ─────────────────────
            _tongueTip = new GameObject("TongueTip");
            _tongueTip.transform.SetParent(transform);
            _tongueTip.transform.localPosition = Vector3.zero;
            _tongueTip.layer = gameObject.layer;

            _tipCollider = _tongueTip.AddComponent<CircleCollider2D>();
            _tipCollider.radius = TongueWidth * 2f;
            _tipCollider.isTrigger = true;

            _tongueTip.SetActive(false);
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

            if (IsTransformed && Input.GetMouseButtonDown(0))
            {
                if (!TongueActive)
                    StartTongue();
            }
        }

        #endregion

        #region ProcessAbility

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (IsTransformed && _movement.CurrentState == CharacterStates.MovementStates.Flying)
            {
                StopFrog();
                return;
            }

            if (IsTransformed)
            {
                HandleFuel();
            }

            if (TongueActive)
            {
                UpdateTongue();
            }
        }

        #endregion

        #region Frog Logic

        public virtual void StartFrog()
        {
            if (!AbilityAuthorized) return;

            if (_condition.CurrentState != CharacterStates.CharacterConditions.Normal)
                return;

            if (_otherTransformation != null && _otherTransformation.IsTransformed)
                return;

            if (_movement.CurrentState == CharacterStates.MovementStates.Flying ||
                _movement.CurrentState == CharacterStates.MovementStates.Dashing ||
                _movement.CurrentState == CharacterStates.MovementStates.Gripping)
            {
                return;
            }

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
            UpdateAnimator(); // Forzamos actualización de animación al cambiar estado
        }

        public virtual void StopFrog()
        {
            if (!IsTransformed) return;

            IsTransformed = false;
            _lastTransformStop = Time.time;

            StopTongue();
            RemoveJumpBoost();
            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
            TriggerEndFeedbackObject();
            UpdateAnimator(); // Forzamos actualización de animación al cambiar estado
        }

        #endregion

        #region Tongue

        void StartTongue()
        {
            TongueActive = true;
            _extending = true;
            _currentLength = 0f;

            _lineRenderer.enabled = true;
            _tongueTip.SetActive(true);
        }

        void StopTongue()
        {
            TongueActive = false;
            _extending = false;
            _currentLength = 0f;

            if (_lineRenderer != null)
                _lineRenderer.enabled = false;

            if (_tongueTip != null)
                _tongueTip.SetActive(false);
        }

        void UpdateTongue()
        {
            float dir = _character.IsFacingRight ? 1f : -1f;

            // ── Extender / retraer ────────────────────────
            if (_extending)
            {
                _currentLength += TongueSpeed * Time.deltaTime;
                if (_currentLength >= MaxTongueLength)
                {
                    _currentLength = MaxTongueLength;
                    _extending = false; // empieza a retroceder
                }
            }
            else
            {
                _currentLength -= TongueSpeed * Time.deltaTime;
                if (_currentLength <= 0f)
                {
                    StopTongue();
                    return;
                }
            }

            // ── Posiciones ────────────────────────────────
            Vector3 origin = transform.position;
            Vector3 tip = origin + new Vector3(dir * _currentLength, 0f, 0f);

            // ── LineRenderer ──────────────────────────────
            _lineRenderer.SetPosition(0, origin);
            _lineRenderer.SetPosition(1, tip);

            // ── Tip collider ──────────────────────────────
            _tongueTip.transform.position = tip;
        }

        #endregion

        #region Fuel

        protected virtual void HandleFuel()
        {
            if (SharedJetpack == null) return;
            if (SharedJetpack.JetpackUnlimited) return;

            SharedJetpack.JetpackFuelDurationLeft =
                Mathf.Max(0f, SharedJetpack.JetpackFuelDurationLeft - Time.deltaTime);

            if (SharedJetpack.JetpackFuelDurationLeft <= 0f)
                StopFrog();
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
            // Apagamos el de fin por si estaba activo
            if (TransformationEndFeedback != null)
                TransformationEndFeedback.SetActive(false);

            if (TransformationStartFeedback != null)
            {
                TransformationStartFeedback.SetActive(false);
                TransformationStartFeedback.SetActive(true);
            }
        }

        protected virtual void TriggerEndFeedbackObject()
        {
            // Apagamos el de inicio
            if (TransformationStartFeedback != null)
                TransformationStartFeedback.SetActive(false);

            if (TransformationEndFeedback != null)
            {
                TransformationEndFeedback.SetActive(false);
                TransformationEndFeedback.SetActive(true);
            }
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
        }

        #endregion
    }
}