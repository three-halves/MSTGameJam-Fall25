using UnityEngine;

public class AttackableBase : MonoBehaviour
{
    public virtual Vector3 OnAttacked(Player player, int chargeStage)
    {
        return Vector3.zero;
    }
}

