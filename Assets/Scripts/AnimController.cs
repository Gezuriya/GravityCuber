using UnityEngine;

public class AnimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private GravityCube _playerController;
    [SerializeField] private Rigidbody _rb;

    [Header("Fall Settings")]
    [SerializeField] private float _shortFallThreshold = 0.5f;
    [SerializeField] private float _landAnimationDuration = 0.3f;

    private bool _wasGrounded;
    private float _lastLandTime;
    private float _fallStartTime;
    private bool _isInLongFall;
    private Vector3 _surfaceNormal = Vector3.up;

    void Update()
    {
        if (_animator == null || _playerController == null || _rb == null) return;

        UpdateSurfaceNormal();
        UpdateAnimatorParameters();
        HandleFallTimer();
        HandleSpecialTransitions();
        Rotation();
    }

    private void UpdateSurfaceNormal()
    {
        if (_playerController.IsGrounded)
        {
            if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 1.5f))
            {
                _surfaceNormal = hit.normal;
            }
            else
            {
                _surfaceNormal = transform.up;
            }
        }
    }
    [SerializeField] private float _rotationSpeed = 10f;

    private void Rotation()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); 
        float vertical = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            Vector3 localDirection = new Vector3(horizontal, 0f, vertical).normalized;

            float targetAngle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

            Quaternion targetLocalRotation = Quaternion.Euler(0f, targetAngle, 0f);

            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimatorParameters()
    {
        float verticalVelocity = Vector3.Dot(_rb.linearVelocity, _surfaceNormal);
        _animator.SetFloat("VerticalVelocity", verticalVelocity);

        Vector3 horizontalVelocity = _rb.linearVelocity - (_surfaceNormal * verticalVelocity);
        float speed = horizontalVelocity.magnitude;
        _animator.SetFloat("Speed", speed);

        _animator.SetBool("IsGrounded", _playerController.IsGrounded);
        _animator.SetBool("IsInLongFall", _isInLongFall);

        if (!_playerController.IsGrounded && _wasGrounded)
        {
            _animator.SetTrigger("Jump");
            _fallStartTime = Time.time;
            _isInLongFall = false;
        }

        _wasGrounded = _playerController.IsGrounded;
    }

    private void HandleFallTimer()
    {
        if (!_playerController.IsGrounded && !_isInLongFall)
        {
            float fallDuration = Time.time - _fallStartTime;

            if (fallDuration >= _shortFallThreshold)
            {
                _isInLongFall = true;
                _animator.SetBool("IsInLongFall", true);
            }
        }

        if (_playerController.IsGrounded)
        {
            _isInLongFall = false;
        }
    }

    private void HandleSpecialTransitions()
    {
        if (_playerController.IsGrounded && !_wasGrounded)
        {
            if (_isInLongFall)
            {
                _animator.SetTrigger("LandTrigger");
            }
            _lastLandTime = Time.time;
        }
    }

    public bool CanExitLandAnimation()
    {
        return Time.time - _lastLandTime > _landAnimationDuration;
    }
}
