using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.CorgiEngine;

namespace MoreMountains.CorgiEngine
{
    public class CharacterSwimTransformation : CharacterAbility
    {
        [Header("Swim Settings")]
        public float SwimFuelConsumption = 5f;
        public CharacterFly SwimFlyAbility;

        [Header("Shared Fuel")]
        public CharacterJetpackManaged SharedJetpack;

        [Header("Feedbacks")]
        public GameObject SwimStartFeedback;
        public GameObject SwimEndFeedback;
        public GameObject OutOfFuelFeedback;

        [Header("Agua — Layer Detection")]
        public string WaterLayerName = "Water";

        [Header("Muerte")]
        public bool KillOnEmptyFuel = true;

        [MMReadOnly]
        public bool IsSwimming = false;

        protected int _waterLayer;
        protected bool _initialized = false;

        protected override void Initialization()
        {
            base.Initialization();
            _waterLayer = LayerMask.NameToLayer(WaterLayerName);

            CleanStates();
            _initialized = true;
        }

        public override void ResetAbility()
        {
            base.ResetAbility();
            CleanStates();
        }

        protected virtual void CleanStates()
        {
            IsSwimming = false;

            if (SwimFlyAbility != null)
            {
                SwimFlyAbility.AlwaysFlying = false;
                SwimFlyAbility.PermitAbility(false);
            }

            if (_controller != null)
            {
                _controller.SetForce(Vector2.zero);
                _controller.SetHorizontalForce(0f);
                _controller.SetVerticalForce(0f);
                _controller.GravityActive(true);
            }

            if (_movement != null)
            {
                _movement.ChangeState(CharacterStates.MovementStates.Idle);
            }

            SetFeedback(SwimStartFeedback, false);
            SetFeedback(OutOfFuelFeedback, false);
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();
            if (!IsSwimming) return;
            ConsumeFuel();
        }

        protected virtual void ConsumeFuel()
        {
            if (SharedJetpack == null || SharedJetpack.JetpackUnlimited) return;

            SharedJetpack.JetpackFuelDurationLeft -= SwimFuelConsumption * Time.deltaTime;

            if (SharedJetpack.JetpackFuelDurationLeft <= 0f)
            {
                SharedJetpack.JetpackFuelDurationLeft = 0f;
                UpdateSharedFuelBar();
                OnFuelEmpty();
                return;
            }
            UpdateSharedFuelBar();
        }

        protected virtual void OnFuelEmpty()
        {
            StopSwimming();
            SetFeedback(OutOfFuelFeedback, true);

            if (KillOnEmptyFuel && _character != null)
            {
                Health characterHealth = _character.GetComponent<Health>();
                if (characterHealth != null) characterHealth.Kill();
            }
        }

        public virtual void StartSwimming()
        {
            if (!_initialized || !AbilityAuthorized || IsSwimming || _character == null) return;
            if (SharedJetpack != null && SharedJetpack.JetpackFuelDurationLeft <= 0f) return;

            IsSwimming = true;
            if (SwimFlyAbility != null)
            {
                SwimFlyAbility.PermitAbility(true);
                SwimFlyAbility.AlwaysFlying = true;
            }
            _movement.ChangeState(CharacterStates.MovementStates.Flying);
            SetFeedback(SwimStartFeedback, true);
        }

        public virtual void StopSwimming()
        {
            if (!IsSwimming) return;
            CleanStates(); // Reutilizamos la limpieza al salir del agua
            SetFeedback(SwimEndFeedback, true);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.layer == _waterLayer) StartSwimming();
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.gameObject.layer == _waterLayer) StopSwimming();
        }

        protected virtual void UpdateSharedFuelBar()
        {
            if (SharedJetpack == null || !GUIManager.HasInstance || _character == null) return;
            GUIManager.Instance.UpdateJetpackBar(SharedJetpack.JetpackFuelDurationLeft, 0f, SharedJetpack.JetpackFuelDuration, _character.PlayerID);
        }

        protected virtual void SetFeedback(GameObject feedback, bool active)
        {
            if (feedback == null) return;
            feedback.SetActive(false);
            if (active) feedback.SetActive(true);
        }
    }
}