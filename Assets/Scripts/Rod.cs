using UnityEngine;

public class Rod : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Player _player;
    [SerializeField] private HUD _hud;
    [SerializeField] private FollowTransform _followTransform;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Animator _animator;

    [Header("Usage Parameters")]
    [SerializeField] private float _maxDistance;
    [SerializeField] private float _pullStrength;
    [SerializeField] private float _inputReductionStrength;
    [SerializeField] private float _yStrengthBoost;
    [SerializeField] private float _travelSpeed;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private bool _linearForce;
    [SerializeField] Transform lineOrigin;
    [SerializeField] private float _disableGravityTime;

    // Private Members
    private bool _retracted = true;
    private bool _onSurface = false;

    private Vector3 _travelDir;

    private HookableBase hookedSurface = null;

    public void Start()
    {
        _player.OnPlayerDeath += () =>
        {
            Retract();
        };
    }

    public void FixedUpdate()
    {
        // Sent, currently traveling through air
        if (!_retracted && !_onSurface)
        {
            transform.position += _travelDir * _travelSpeed;
            // Check for collision
            if (Physics.Raycast(
                transform.position - _travelDir * _travelSpeed, 
                _travelDir, 
                out RaycastHit hit, 
                _travelSpeed * 2,
                _layerMask
            ))
            // Collision found 
            {
                if (_player.transform.position.y - hit.point.y < 1) _player.DisableGravityForSeconds(_disableGravityTime);
                transform.position = hit.point;
                _onSurface = true;
                _animator.SetBool("OnSurface", _onSurface);
                _player.ForceWallrunCooldown();
                if (hit.transform.TryGetComponent<HookableBase>(out var surface))
                {
                    hookedSurface = surface;
                    surface.OnHooked(_player, this);
                }
            }
            // check for max dist reached
            if (Vector3.Distance(_player.transform.position, transform.position) > _maxDistance)
            {
                Retract();
            }
        }
        // Landed on surface, pull player
        else if (_onSurface)
        {
            Vector3 pullForce = (transform.position - _player.transform.position) * _pullStrength;
            if (_linearForce) pullForce = pullForce.normalized * _pullStrength;
            // Reduce hook lateral strength if player is moving against hook
            float reduction = Vector3.Dot(Vector3.Scale(pullForce, new Vector3(1, 0, 1)).normalized, _player.GetWishDir());
            reduction = (1 + reduction) / 2f * (1 - _inputReductionStrength) + _inputReductionStrength;
            pullForce = Vector3.Scale(pullForce, new Vector3(reduction, 1, reduction));
            pullForce.y *= _yStrengthBoost;
            _player.ApplyForce(pullForce);
        }

        _lineRenderer.enabled = !_retracted;
        // Vector3 offset = Camera.main.transform.right * _veiwportOffset.x  
        // + Camera.main.transform.up * _veiwportOffset.y 
        // + Camera.main.transform.forward * _veiwportOffset.z;
    }

    public void LateUpdate()
    {
        Vector3[] positions = new Vector3[]{ lineOrigin.transform.position, transform.position };
        _lineRenderer.SetPositions(positions);
    }

    public void Use()
    {
        _retracted = false;
        _followTransform.enabled = false;
        _travelDir = _player.GetLookVector();
        _animator.SetBool("Retracted", _retracted);
    }

    public void Retract()
    {
        _onSurface = false;
        _retracted = true;
        _followTransform.enabled = true;
        _player.DisableGravityForSeconds(0);
        _animator.SetBool("OnSurface", _onSurface);
        _animator.SetBool("Retracted", _retracted);

        if (hookedSurface != null)
        {
            Vector3 launchVector = hookedSurface.OnReleased(_player, this);
            hookedSurface = null;

            if (launchVector != Vector3.zero)
            {
                _player.ApplyForce(launchVector);
            }
        }
    }
}