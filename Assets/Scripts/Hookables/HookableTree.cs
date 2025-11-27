using System;
using UnityEngine;

class HookableTree : HookableBase
{
    [SerializeField] private float _power;
    [SerializeField] private float _maxCharge;

    private Player _playerRef = null;
    private float _charge = 0f;

    public override void OnHooked(Player player, Rod rod)
    {
        base.OnHooked(player, rod);
        _playerRef = player;
        _charge = 0f;
    }

    public override Vector3 OnReleased(Player player, Rod rod)
    {
        base.OnReleased(player, rod);
        _playerRef = null;
        return _power * _charge * ((rod.transform.position - player.transform.position).normalized + Vector3.up * 0.25f);
    }

    void FixedUpdate()
    {
        if (_playerRef != null)
        {
            Vector3 angle = (_playerRef.transform.position - transform.position).normalized;
            float delta = Vector3.Dot(angle, _playerRef.GetVelocityVector() + _playerRef.GetWishDir());
            _charge = Math.Clamp(_charge + delta, 0, _maxCharge);
        }

    }
}