using UnityEngine;

public class AttackableBreak : AttackableBase
{
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private MeshRenderer _mr;
    [SerializeField] private Collider _collider;
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private int requiredChargeLevel = 0;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        GameObject.Find("Player").GetComponent<Player>().OnPlayerDeath += Restore;
    }

    public override Vector3 OnAttacked(Player player, int chargeStage)
    {
        base.OnAttacked(player, chargeStage);
        
        if (chargeStage < requiredChargeLevel) return Vector3.zero;

        if (_particleSystem != null)
        {
            _particleSystem.Play();
        }

        _collider.enabled = false;
        _mr.enabled = false;
        _rb.isKinematic = true;
        return new Vector3(0, 1, 0);
    }

    public void Restore()
    {
        _rb.position = startPos;
        _rb.isKinematic = false;
        _collider.enabled = true;
        _mr.enabled = true;
        transform.position = startPos;
    }
}