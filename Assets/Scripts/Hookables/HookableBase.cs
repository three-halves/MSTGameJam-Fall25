using UnityEngine;

public class HookableBase : MonoBehaviour
{
    public virtual void OnHooked(Player player, Rod rod)
    {

    }

    public virtual Vector3 OnReleased(Player player, Rod rod)
    {
        return Vector3.zero;
    }
}

