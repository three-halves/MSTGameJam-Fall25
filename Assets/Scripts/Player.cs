using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerInput))]
public class Player : MonoBehaviour
{
    public Transform CameraTransform {get; private set;}
    [SerializeField] CharacterController characterController;
    [SerializeField] private TextMeshProUGUI debugText;
    private bool jumpHeld = false;
    private bool jumpedThisInput = false;
    private bool groundedLastTick = false;
    private bool grounded = false;

    [SerializeField] private PlayerMovementParameters _movement;
    
    [Header("Gameplay Parameters")]
    [SerializeField] private float _deathPlaneY;

    public delegate void PlayerDeathHandler();
    public event PlayerDeathHandler OnPlayerDeath;

    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private LayerMask _attackableMask;

    [Header("Usable Item References")]
    [SerializeField] private Hammer _hammer;
    [SerializeField] private Rod _rod;

    // [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioClip[] wallRunSFX;
    [SerializeField] private AudioClip deathSFX;

    [SerializeField] private Vector2 testSens;

    // [SerializeField] private AudioSource windAudioSource;

    private Vector3 _moveInputDir = Vector3.zero;
    private Vector3 _rawMoveInputDir = Vector3.zero;

    private int currentAirJumps;

    private Vector3 lateralVector = new(1, 0, 1);

    private float wallRunTimer = 0f;
    private Vector3 wallRunDirection;
    private Vector3 WallRunNormal;
    // private AttackableBase highlightedObject;
    private Vector3 wallRunCastVector;

    private float disableGravityTimer = 0f;

    // Extra force caused by other objects, to be applied next tick
    private Vector3 applyForce = Vector3.zero;
    private bool overrideYForceNextTick = false;

    private float targetCameraTilt = 0f;
    private float camTiltVel = 0f;
    
    private Vector3 spawnPosition;
    private Vector3 spawnRotation;
    private Vector2 targetCamPosition = Vector2.zero;

    private bool respawnedThisTick;

    // Used by some items to disable wallrun while in use
    public bool OverrideWallrunAbility = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        GetComponent<PlayerInput>().enabled = true;
        GetComponent<PlayerInput>().actions.actionMaps[0].Enable();
        // Set up camera follower
        CameraTransform = Camera.main.transform;
        CameraTransform.forward = transform.forward;

        spawnPosition = transform.position;
        spawnRotation = transform.forward;
    }

    void Update()
    {
        float effectSpeed = Mathf.Max(0, Vector3.Scale(characterController.velocity, lateralVector).magnitude - _movement.groundMaxVel * 1.1f);
        // camera fov effects
        if (PlayerPrefs.GetInt("fovfx", 1) == 1)
        {
            Camera.main.fieldOfView = PlayerPrefs.GetFloat("fov", 90) + effectSpeed * 0.5f;
        }
        // windAudioSource.volume = effectSpeed / 40f;

        // Camera tilt logic
        CameraTransform.eulerAngles = new Vector3(
            targetCamPosition.y,
            CameraTransform.eulerAngles.y,
            Mathf.SmoothDampAngle(CameraTransform.eulerAngles.z, targetCameraTilt, ref camTiltVel, 0.1f)
        );
    }

    void FixedUpdate()
    {
        debugText.text = string.Format(
            "CCVel:\t{0}\nInputDir:\t{1}\n{2}\nGrounded: {3}\n{4}", 
            characterController.velocity, 
            _moveInputDir, 
            jumpHeld, 
            grounded, 
            wallRunTimer
        );

        grounded = GroundCheck();
        bool inWallrun = wallRunTimer > 0;
        if (grounded) currentAirJumps = _movement.airJumps;

        // update timers
        wallRunTimer -= Time.fixedDeltaTime;
        disableGravityTimer -= Time.fixedDeltaTime;

        // CheckHighlightableObjects();

        // Player movement
        Vector3 velocity = characterController.velocity;
        if (overrideYForceNextTick)
        {
            overrideYForceNextTick = false;
            velocity.y = Math.Max(velocity.y, 0);
        }

        Vector3 delta = Move(_moveInputDir, velocity + applyForce);
        applyForce = Vector3.zero;

        // Jump logic
        if (jumpHeld && (grounded || (currentAirJumps > 0 && !jumpedThisInput) || inWallrun))
        {
            // Redirect all lateral movement in jump direction
            if(_moveInputDir != Vector3.zero)
                delta = Vector3.Scale(delta, lateralVector).magnitude * _moveInputDir;

            // Give some extra vel for double jumping out of a vertical jump
            if (Vector3.Scale(delta, lateralVector).magnitude < _movement.groundMaxVel * Time.fixedDeltaTime / 3f) 
                delta = _movement.groundMaxVel * Time.fixedDeltaTime / 3f * _moveInputDir;

            // Apply lateral jump force while in wallrun
            if (inWallrun)
            {
                wallRunTimer = 0f;
                delta = _movement.wallRunSpeed * Time.deltaTime * wallRunDirection + WallRunNormal * _movement.wallJumpLatVel;
            }

            // Apply vertical jump force
            delta.y = _movement.jumpVel * Time.fixedDeltaTime;

            jumpedThisInput = true;

            // Remove airjump if appropriate
            if (!grounded && !inWallrun) currentAirJumps--;
        }

        // Apply gravity
        if (disableGravityTimer <= 0) delta.y += _movement.gravity * Time.fixedDeltaTime;
        groundedLastTick = grounded;

        // Cap fall speed
        delta.y = Math.Max(delta.y, inWallrun ? 0 : _movement.maxFallSpeed);

        // Check if below death plane
        if (transform.position.y < _deathPlaneY) Respawn();

        if (respawnedThisTick) delta = Vector3.zero;
        respawnedThisTick = false;

        characterController.Move(delta);
    }

    public void ApplyForce(Vector3 f, bool resetYVel = false)
    {
        applyForce += f;
        overrideYForceNextTick = resetYVel;
    }

    /// <summary>
    /// Apply acceleration based on current velocity and desired velocity
    /// </summary>
    /// <param name="inputDir"></param>
    /// <param name="currentVel"></param>
    /// <param name="acceleration"></param>
    /// <param name="maxVel"></param>
    /// <returns></returns>
    private Vector3 AddAcceleration(Vector3 inputDir, Vector3 currentVel, float acceleration, float maxVel)
    {
        float projectedVel = Vector3.Dot(currentVel * Time.fixedDeltaTime, inputDir);
        float accelVel = acceleration * Time.fixedDeltaTime;
        maxVel *= Time.fixedDeltaTime;

        // Cap max accel
        if (projectedVel + accelVel > maxVel)
        {
            accelVel = maxVel - projectedVel;
        }
        
        return currentVel * Time.fixedDeltaTime + inputDir * accelVel;
    }

    // Used by items to temporarily prevent wallrunning
    public void ForceWallrunCooldown()
    {
        wallRunTimer = 0;
    }

    /// <summary>
    /// Calculates current player state and returns an approprate movement vector based on state & input
    /// </summary>
    /// <param name="inputDir"></param>
    /// <param name="currentVel"></param>
    /// <returns></returns>
    private Vector3 Move(Vector3 inputDir, Vector3 currentVel)
    {
        // Determine current state movement
        bool useGroundPhys = groundedLastTick && grounded;
        bool shouldWallrun = CheckWallrun(inputDir);
        // stop wallruning early if we stop hitting a wall
        if (!shouldWallrun && wallRunTimer > 0) 
            wallRunTimer = 0;

        // Initiate wallrun
        if (shouldWallrun && wallRunTimer <= -_movement.wallRunRecoveryTime)
        {
            wallRunTimer = _movement.wallRunTime;
            currentAirJumps = _movement.airJumps;
            wallRunCastVector = transform.right;

            wallRunDirection = Vector3.Cross(Vector3.up, WallRunNormal);
            float s = Mathf.Sign(Vector3.Dot(wallRunDirection, transform.forward));
            wallRunDirection *= s;
            targetCameraTilt = 5 * s;
            jumpHeld = false;
            _rod.Retract();
        }

        // Do current state movement
        if (wallRunTimer > 0f)
        {
            return WallrunMove(inputDir, currentVel);
        }
        else if (useGroundPhys)
        {
            return GroundMove(inputDir, currentVel);
        }
        else
        {
            return AirMove(inputDir, currentVel);
        }

    }

    /// <summary>
    /// Check if we should initiate (or maintain) a wall run this frame
    /// </summary>
    private bool CheckWallrun(Vector3 inputDir)
    {
        Physics.Raycast(
            transform.position + characterController.center,
            wallRunTimer <= 0 ? transform.right : wallRunCastVector,
            out RaycastHit hit,
            characterController.radius * 1.5f,
            _groundMask
        );
        // check left wall if no right wall found
        if (hit.normal == Vector3.zero)
            Physics.Raycast(
            transform.position + characterController.center,
            (wallRunTimer <= 0 ? transform.right : wallRunCastVector) * -1,
            out hit,
            characterController.radius * 1.5f,
            _groundMask
        );

        // Don't wallrun if no wall is found, input is not perpendicular to wall, or we are grounded, or not fast enough
        if (hit.normal == Vector3.zero || 
            Vector3.Dot(hit.normal, inputDir) == 0 || 
            grounded || 
            Vector3.Scale(characterController.velocity, lateralVector).magnitude < _movement.groundMaxVel * Time.fixedDeltaTime * 0.75) 
        {
            targetCameraTilt = 0f;
            return false;
        }

        // Otherwise, initiate/continue wallrun
        WallRunNormal = hit.normal;
        return true;
    }

    private Vector3 WallrunMove(Vector3 inputDir, Vector3 currentVel)
    {
        float speed = Mathf.Max(Vector3.Scale(currentVel, lateralVector).magnitude, _movement.wallRunSpeed);
        return speed * Time.fixedDeltaTime * wallRunDirection 
            + Vector3.Scale(currentVel, new Vector3(0, 1, 0)) * Time.fixedDeltaTime;
    }

    private Vector3 GroundMove(Vector3 inputDir, Vector3 currentVel)
    {
        // Apply friction
        Vector3 lateralVel = Vector3.Scale(currentVel, lateralVector);
        if (lateralVel.magnitude != 0)
        {
            float d = lateralVel.magnitude * _movement.friction * Time.fixedDeltaTime;
            currentVel.x *= Mathf.Max(lateralVel.magnitude - d, 0) / lateralVel.magnitude;
            currentVel.z *= Mathf.Max(lateralVel.magnitude - d, 0) / lateralVel.magnitude;
        }

        return AddAcceleration(
            inputDir,
            currentVel,
            _movement.groundAcceleration,
            _movement.groundMaxVel
            );
    }

    private Vector3 AirMove(Vector3 inputDir, Vector3 currentVel)
    {
        // Air control
        float oldyspeed = currentVel.y;
        currentVel.y = 0;

        float dot = Vector3.Dot(currentVel.normalized, inputDir);

        float magnitude = currentVel.magnitude;
        currentVel.Normalize();

        // reduce air control while in walljump cooldown
        // float control = _airControl * Mathf.Min(-wallRunTimer / _wallRunRecoveryTime, 1f);
        float control = _movement.airControl;

        float k = control * dot * dot * Time.fixedDeltaTime;
        if (dot != 0)
        {
            currentVel *= currentVel.magnitude;
            currentVel += inputDir * k;
            currentVel.Normalize();
        }
        currentVel.x *= magnitude;
        currentVel.y = oldyspeed;
        currentVel.z *= magnitude;

        return AddAcceleration(
            inputDir,
            currentVel,
            _movement.airAcceleration,
            _movement.airMaxVel
            );
    }

    public void OnLook(InputValue value)
    {
        if (CameraTransform == null) return;
        Vector2 v = value.Get<Vector2>();
        // Vector2 sens = new (
        //     PlayerPrefs.GetFloat("xsens", 0.1f) * (PlayerPrefs.GetInt("xinvert", 0) == 0 ? 1 : -1),
        //     PlayerPrefs.GetFloat("ysens", 0.1f) * (PlayerPrefs.GetInt("yinvert", 1) == 0 ? 1 : -1)
        // );

        Vector2 sens = testSens;

        // Hack fix for webgl canvas bug
        // if (v.x * sens.x >= 90) v.x = 0;
        // if (v.y * sens.y >= 90) v.y = 0;
        // Rotate user and cam to with mouse x movement
        targetCamPosition.x += v.x * sens.x;
        transform.Rotate(Vector3.up, v.x * sens.x, Space.World);

        // Rotate only cam with mouse y movement
        // cameraTransform.Rotate(Vector3.right, v.y * sens.y, Space.Self);
        targetCamPosition.y = Mathf.Clamp(targetCamPosition.y + v.y * sens.y, -85, 85);
        // Vector3 ea = cameraTransform.eulerAngles;
        // cameraTransform.eulerAngles = new Vector3(Mathf.Clamp(-Mathf.DeltaAngle(ea.x,0),-75,55), ea.y, ea.z);
        CalculateMoveInputDir();
    }

    public void AttackableRaycast(out RaycastHit hit)
    {
        float range = _movement.attackRange + characterController.velocity.magnitude * Time.fixedDeltaTime;
        Physics.Raycast(
            transform.position + characterController.center,
            CameraTransform.forward,
            out hit,
            range,
            _attackableMask
        );

        if (hit.collider != null) return;

        Physics.Raycast(
            transform.position + characterController.center + CameraTransform.forward * range,
            -CameraTransform.forward,
            out hit,
            range,
            _attackableMask
        );

    }

    // /// <summary>
    // /// Check if there are any attackable objects in attack range to highlight
    // /// </summary>
    // private void CheckHighlightableObjects()
    // {
    //     // check if there are any objects we can highlight
    //     AttackableRaycast(out RaycastHit hit);

    //     if (hit.collider == null)
    //     {
    //         highlightedObject?.SetHighlight(false);
    //         return;
    //     }
        
    //     if (hit.collider.TryGetComponent<AttackableBase>(out var found))
    //     {
    //         if (found != highlightedObject && highlightedObject != null) 
    //             highlightedObject.SetHighlight(false);
            
    //         found.SetHighlight(true);
    //         highlightedObject = found;
    //     }
    // }

    public void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        _rawMoveInputDir.x = v.x;
        _rawMoveInputDir.z = v.y;
        _rawMoveInputDir.Normalize();
        CalculateMoveInputDir();
    }

    // Rotate desired move dir with cam
    public void CalculateMoveInputDir()
    {
        if (CameraTransform == null) return;
        _moveInputDir = Quaternion.AngleAxis(CameraTransform.rotation.eulerAngles.y, Vector3.up) * _rawMoveInputDir;
    }

    public void OnJump(InputValue value)
    {
        jumpHeld = value.isPressed;
        jumpedThisInput = false;
    }

    public void OnRestart(InputValue value)
    {
        Respawn();
    }

    public void Respawn()
    {
        characterController.enabled = false;
        transform.position = spawnPosition;
        transform.forward = spawnRotation;
        respawnedThisTick = true;
        CalculateMoveInputDir();
        OnPlayerDeath.Invoke();

        // audioSource.pitch = 1f;
        // audioSource.PlayOneShot(deathSFX);
        
        characterController.enabled = true;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            _hammer.StartCharge();
        }
        else
        {
            _hammer.ReleaseCharge();
        }
    }

    public void OnRod(InputValue value)
    {
        if(value.isPressed)
        {
            _rod.Use();
        }
        else
        {
            _rod.Retract();
        }
    }

    public Vector3 GetLookVector()
    {
        return CameraTransform.forward;
    }
    
    public Vector3 GetVelocityVector()
    {
        return characterController.velocity;
    }

    public RaycastHit? GetLookSurface()
    {
        Physics.Raycast(
            transform.position + characterController.center, 
            GetLookVector(), 
            out RaycastHit hit, 
            Mathf.Infinity,
            _attackableMask
        );
        return hit.collider == null ? null : hit;
    }

    public Vector3 GetWishDir()
    {
        return _moveInputDir;
    }

    public void DisableGravityForSeconds(float s)
    {
        disableGravityTimer = s;
    }

    // public void GetCollectable()
    // {
    //     WorldState.Instance.GetCollectable();
    //     collectableDisplayTimer = 3f;
    //     collectableText.text = "x" + WorldState.Instance.collectableCount;
    // }

    public void SetSpawn(Vector3 pos, Vector3 rot)
    {
        spawnPosition = pos;
        spawnRotation = rot;
    }

    // public void OnPause(InputValue value)
    // {
    //     if (value.isPressed) 
    //         FindObjectsByType<PauseMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0].gameObject.SetActive(true);
    // }

    public void OnNewGame(InputValue value)
    {
        if (!value.isPressed) return;
        SceneManager.LoadScene("Main");
    }

    private bool GroundCheck()
    {
        float dist = characterController.height * 0.56f;
        Vector3 origin = transform.position + characterController.center;
        Vector3 offset = transform.forward * characterController.radius;
        return 
            Physics.Raycast(origin, Vector3.down, dist, _groundMask) 
            || Physics.Raycast(origin + offset, Vector3.down, dist, _groundMask)
            || Physics.Raycast(origin - offset, Vector3.down, dist, _groundMask);
    }
}