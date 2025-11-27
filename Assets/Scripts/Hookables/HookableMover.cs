using UnityEngine;

class HookableMover : HookableBase
{
    [SerializeField] private Vector3 _hookedOffset;
    [SerializeField] private float _speed;

    private Vector3 startPos;
    private Vector3 hookedPos;
    private Vector3 target;
    private Transform parent;

    public override void OnHooked(Player player, Rod rod)
    {
        base.OnHooked(player, rod);
        rod.transform.parent = transform;
        target = hookedPos;
    }

    public override Vector3 OnReleased(Player player, Rod rod)
    {
        target = startPos;
        rod.transform.parent = parent;
        return base.OnReleased(player, rod);
    }

    void Start()
    {
        parent = transform.parent;
        startPos = transform.position;
        hookedPos = transform.position + _hookedOffset;
        target = startPos;
    }

    void FixedUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, _speed);
    }
}