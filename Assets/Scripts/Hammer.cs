using UnityEngine;
using System;

public class Hammer : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Player _player;
    [SerializeField] private HUD _hud;

    [Header("Usage Parameters")]
    [SerializeField] private float _maxChargeTime;
    [SerializeField] private float _cooldownTime;
    [SerializeField] private float _maxDistance;
    [SerializeField] private Vector3 _launchBias;
    // X component is time threshold, y component is force multiplier
    [SerializeField] private Vector2[] _chargeStages;

    // Private members
    private float _chargeTimer = 0f;
    private bool _isCharging = false;
    private int _currentChargeIndex = 0;
    private float _cooldownTimer = 0f;

    public void Start()
    {
        _player.OnPlayerDeath += () =>
        {
            _chargeTimer = 0f;
            _isCharging = false;
            _hud.UpdateHammer(_isCharging, 0);
        };
    }

    public void FixedUpdate()
    {
        if (_isCharging)
        {
            _chargeTimer += Time.fixedDeltaTime;
            int ind = GetForceIndexAtCharge(_chargeTimer);
            if (_currentChargeIndex != ind)
            {
                _currentChargeIndex = ind;
                _hud.UpdateHammer(_isCharging, ind);
            }
        }
        _cooldownTimer -= Time.fixedDeltaTime;
    }

    public void StartCharge()
    {
        // Debug.Log("Begin Charge");
        if (_cooldownTimer > 0f) return;
        _chargeTimer = 0f;
        _isCharging = true;
        _hud.UpdateHammer(_isCharging, 0);
    }

    public void ReleaseCharge()
    {
        // Debug.Log("Release Charge");
        if (_isCharging == false) return;
        RaycastHit? surface = _player.GetLookSurface();

        // Charge release and launch logic
        if (surface.HasValue)
        {
            TryLaunchPlayer(surface.Value);
        }

        _isCharging = false;
        _hud.UpdateHammer(_isCharging, 0);
        _cooldownTimer = _cooldownTime;
    }

    private void TryLaunchPlayer(RaycastHit surface)
    {
        Vector3? force = null;
        float d = Vector3.Dot(_player.GetLookVector(), _player.GetVelocityVector());
        d = surface.distance - Math.Max(d, 0);
        if (d > _maxDistance) return;

        // Attackable interaction logic
        if (surface.collider.TryGetComponent(out AttackableBase attackable))
        {
            force = attackable.OnAttacked(_player, _currentChargeIndex);
        }

        float f = _chargeStages[_currentChargeIndex].y;

        Vector3 dir = (-_player.GetLookVector() + _launchBias).normalized;
        if (force == null) force = dir * f;
        
        _player.ApplyForce(force.Value, true);
        _player.ForceWallrunCooldown();
    }

    private int GetForceIndexAtCharge(float t)
    {
        for (int i = _chargeStages.Length - 1; i >= 0; i--)
        {
            Vector2 v = _chargeStages[i];
            if (t >= v.x)
            {
                return i;
            }
        }

        return 0;
    }
}