using UnityEngine;


public enum GravityAxis { Down, Up, Left, Right }

public class GravityCube : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float maxSpeedGround = 8f;
    [SerializeField, Min(0f)] private float maxSpeedAir = 8f;
    [SerializeField, Min(0f)] private float accelGround = 40f;
    [SerializeField, Min(0f)] private float accelAir = 8f;

    [Header("Gravity")]
    [SerializeField, Min(0f)] private float gravityMagnitude = 25f;
    [SerializeField, Min(0f)] private float pushImpulse = 6f;       // Space
    [SerializeField, Min(0f)] private float rotationSpeed = 540f;

    [Header("Grounding")]
    [SerializeField, Min(0f)] private float groundProbeExtra = 0.05f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Feedback")]
    [SerializeField] private Color colorDown = Color.blue;
    [SerializeField] private Color colorUp = Color.red;
    [SerializeField] private Color colorLeft = Color.green;
    [SerializeField] private Color colorRight = Color.yellow;
    [SerializeField] private ParticleSystem pushVfxPrefab;
    [SerializeField] private AudioClip gravityChangeSfx;
    [SerializeField] private AudioClip pushSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    // state
    public GravityAxis CurrentAxis { get; private set; } = GravityAxis.Down;
    public bool IsGrounded;
    public bool IsFalling;

    // cached
    Rigidbody rb;
    Collider col;
    Renderer rend;
    AudioSource audioSrc;
    MaterialPropertyBlock mpb;

    // computed
    Vector3 gravity;  
    Vector3 surfaceUp;         // -gravity.normalized
    Vector3 lastGroundNormal;  // VFX
    Vector3 lastGroundPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rend = GetComponent<Renderer>();
        audioSrc = GetComponent<AudioSource>();

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        mpb = new MaterialPropertyBlock();
        SetGravity(CurrentAxis, playSfx: false); 
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded)
        {
            DoPushOff();
            print("PUSHED OFF");
        }

        if (IsFalling)
        {
            if (Input.GetKeyDown(KeyCode.Q)) SetGravity(GravityAxis.Left);
            if (Input.GetKeyDown(KeyCode.E)) SetGravity(GravityAxis.Right);
            if (Input.GetKeyDown(KeyCode.R)) SetGravity(GravityAxis.Up);
            if (Input.GetKeyDown(KeyCode.F)) SetGravity(GravityAxis.Down);
        }

        Quaternion target = Quaternion.FromToRotation(transform.up, surfaceUp) * transform.rotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        rb.AddForce(gravity, ForceMode.Acceleration);

        ProbeGround();

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        MoveOnSurface(input);
    }

    void DoPushOff()
    {
        rb.AddForce(transform.up * pushImpulse, ForceMode.VelocityChange);
        IsFalling = true;
        IsGrounded = false;

        // VFX 
        if (pushVfxPrefab != null)
        {
            Vector3 pos = lastGroundPoint != Vector3.zero
                ? lastGroundPoint + lastGroundNormal * 0.02f
                : transform.position - surfaceUp * (col.bounds.extents.magnitude * 0.5f);
            Quaternion rot = Quaternion.LookRotation(surfaceUp, Vector3.forward);
            Instantiate(pushVfxPrefab, pos, rot);
        }

        // SFX
        if (pushSfx != null) audioSrc.PlayOneShot(pushSfx, sfxVolume);
    }

    void SetGravity(GravityAxis axis, bool playSfx = true)
    {
        CurrentAxis = axis;
        gravity = AxisToVector(axis) * gravityMagnitude;
        surfaceUp = -gravity.normalized;

        UpdateColor(axis);
        if (playSfx && gravityChangeSfx != null)
            audioSrc.PlayOneShot(gravityChangeSfx, sfxVolume);
    }

    static Vector3 AxisToVector(GravityAxis a)
    {
        switch (a)
        {
            case GravityAxis.Down: return Vector3.down;
            case GravityAxis.Up: return Vector3.up;
            case GravityAxis.Left: return Vector3.left;
            case GravityAxis.Right: return Vector3.right;
            default: return Vector3.down;
        }
    }

    void UpdateColor(GravityAxis a)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        Color c = colorDown;
        if (a == GravityAxis.Up) c = colorUp;
        else if (a == GravityAxis.Left) c = colorLeft;
        else if (a == GravityAxis.Right) c = colorRight;
        mpb.SetColor("_Color", c);  
        rend.SetPropertyBlock(mpb);
    }


    void MoveOnSurface(Vector2 input)
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, surfaceUp).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, surfaceUp).normalized;

        Vector3 wishDir = (forward * input.y + right * input.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        Vector3 vel = rb.linearVelocity;
        Vector3 lateral = Vector3.ProjectOnPlane(vel, surfaceUp);

        float maxSpeed = IsGrounded ? maxSpeedGround : maxSpeedAir;
        float accel = IsGrounded ? accelGround : accelAir;

        Vector3 targetVel = wishDir * maxSpeed;
        Vector3 deltaVel = targetVel - lateral;

        Vector3 requiredAccel = deltaVel / Time.fixedDeltaTime;
        if (requiredAccel.magnitude > accel)
            requiredAccel = requiredAccel.normalized * accel;

        rb.AddForce(requiredAccel, ForceMode.Acceleration);

        lateral = Vector3.ProjectOnPlane(rb.linearVelocity, surfaceUp);
        if (lateral.magnitude > maxSpeed)
        {
            Vector3 excess = lateral.normalized * (lateral.magnitude - maxSpeed);
            rb.AddForce(-excess / Time.fixedDeltaTime, ForceMode.Acceleration);
        }
    }


    void ProbeGround()
    {
        Vector3 dir = gravity.normalized;
        Vector3 origin = transform.position;
        float distance = 0.55f;

        IsGrounded = Physics.Raycast(origin, dir, distance, groundMask);

        IsFalling = !IsGrounded;
        if (IsGrounded)
        {
            lastGroundNormal = -dir;
            lastGroundPoint = origin + dir * distance;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + gravity.normalized * 1.5f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + surfaceUp * 1.0f);
    }
}
