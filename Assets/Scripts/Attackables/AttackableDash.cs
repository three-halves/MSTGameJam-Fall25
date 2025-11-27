using UnityEngine;

public class AttackableDash : AttackableBase
{
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private float[] _power;

    public override Vector3 OnAttacked(Player player, int chargeStage)
    {
        base.OnAttacked(player, chargeStage);
        
        if (_particleSystem != null)
        {
            _particleSystem.Play();
        }

        player.DisableGravityForSeconds(0.25f);
        return player.GetLookVector() * _power[chargeStage];


    }
}