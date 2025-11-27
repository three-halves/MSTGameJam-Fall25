using UnityEngine;

public class Rod : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Player _player;
    [SerializeField] private HUD _hud;
    [SerializeField] private FollowTransform _followTransform;
    [SerializeField] private LineRenderer _lineRenderer;

    [Header("Usage Parameters")]
    [SerializeField] private float _maxDistance;
    [SerializeField] private float _pullStrength;
    [SerializeField] private float _inputReductionStrength;
    [SerializeField] private float _yStrengthBoost;
    [SerializeField] private float _travelSpeed;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private bool _linearForce;
    [SerializeField] Vector3 _veiwportOffset;
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
                transform.position, 
                _travelDir, 
                out RaycastHit hit, 
                _travelSpeed,
                _layerMask
            ))
            // Collision found 
            {
                if (_player.transform.position.y - hit.point.y < 1) _player.DisableGravityForSeconds(_disableGravityTime);
                transform.position = hit.point;
                _onSurface = true;
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
        Vector3 offset = Quaternion.AngleAxis(_player.CameraTransform.rotation.eulerAngles.y, Vector3.up) * _veiwportOffset;
        Vector3[] positions = new Vector3[]{ _player.transform.position + offset, transform.position };
        _lineRenderer.SetPositions(positions);
    }

    public void Use()
    {
        _retracted = false;
        _followTransform.enabled = false;
        _travelDir = _player.GetLookVector();
    }

    public void Retract()
    {
        _onSurface = false;
        _retracted = true;
        _followTransform.enabled = true;
        _player.DisableGravityForSeconds(0);

        if (hookedSurface != null)
        {
            hookedSurface.OnReleased(_player, this);
            hookedSurface = null;
        }
    }
}