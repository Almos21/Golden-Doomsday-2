using UnityEngine;
using MoreMountains.CorgiEngine;

public class WaterFreeSwim : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        var character = collision.GetComponent<Character>();
        if (character == null) return;

        var flyAbility = character.FindAbility<CharacterFly>();

        if (flyAbility != null)
        {
            flyAbility.PermitAbility(true);
            flyAbility.AlwaysFlying = true;

            if (character.MovementState.CurrentState != CharacterStates.MovementStates.Flying)
            {
                character.MovementState.ChangeState(CharacterStates.MovementStates.Flying);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var character = collision.GetComponent<Character>();
        if (character == null) return;

        var flyAbility = character.FindAbility<CharacterFly>();

        if (flyAbility != null)
        {
            flyAbility.AlwaysFlying = false;
            flyAbility.PermitAbility(false);

            character.MovementState.ChangeState(CharacterStates.MovementStates.Idle);
        }
    }
}