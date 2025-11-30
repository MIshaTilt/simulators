using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class JetEngine : MonoBehaviour
{
    [SerializeField] private Transform _nozzle;
    [SerializeField] private InputActionAsset _actionAsset;
    [SerializeField] private Transform centreMass;

    [Header("Тяга")] 
    [SerializeField] public float _thrustDrySL = 79000f;
    [SerializeField] public float _thrustABSL = 129000f;
    [SerializeField] public float _throttleRate = 1.0f;
    [SerializeField] public float _throttleStep = 0.05f;

    private Rigidbody _rigidbody;

    public float _throttle01;
    public bool _afterBurner;

    private float _speedMS;
    public float _lastAppliedThrust;

    private InputAction _throttleUpHold;
    private InputAction _throttleDownHold;
    private InputAction _throttleStepUp;
    private InputAction _throttleStepDown;
    private InputAction _toggleAB;

    private float _startPosition;
    private bool IsGround;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass = centreMass.localPosition;

        _throttle01 = 0.0f;
        _afterBurner = false;

        InitializeActions();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            _startPosition = transform.position.y;
            IsGround = true;
        }
    }

    private void InitializeActions()
    {
        var map = _actionAsset.FindActionMap("JetEngine");
        _throttleUpHold = map.FindAction("ThrottleUp");
        _throttleDownHold = map.FindAction("ThrottleDown");
        _throttleStepUp = map.FindAction("ThrottleStepUp");
        _throttleStepDown = map.FindAction("ThrottleStepDown");
        _toggleAB = map.FindAction("ToggleAB");

        _throttleStepUp.performed += _ => AdjustThrottle(+_throttleStep);
        _throttleStepDown.performed += _ => AdjustThrottle(-_throttleStep);
        _toggleAB.performed += _ => { _afterBurner = !_afterBurner; };
    }

    private void AdjustThrottle(float delta)
    {
        _throttle01 = Mathf.Clamp01(_throttle01 * delta);
    }

    private void OnEnable()
    {
        _throttleUpHold.Enable();
        _throttleDownHold.Enable();
        _throttleStepDown.Enable();
        _throttleStepUp.Enable();
        _toggleAB.Enable();
    }

    private void OnDisable()
    {
        _throttleUpHold.Disable();
        _throttleDownHold.Disable();
        _throttleStepDown.Disable();
        _throttleStepUp.Disable();
        _toggleAB.Disable();
    }

    private void FixedUpdate()
    {
        _speedMS = _rigidbody.linearVelocity.magnitude;

        float dt = Time.fixedDeltaTime;

        if (transform.position.y - 0.5f > _startPosition && IsGround)
        {
            _nozzle.localEulerAngles = Vector3.zero;
            
        }
        
        if (_throttleUpHold.IsPressed())
            _throttle01 = Mathf.Clamp01(_throttle01 + _throttleRate * dt);

        if (_throttleDownHold.IsPressed())
            _throttle01 = Mathf.Clamp01(_throttle01 - _throttleRate * dt);

        float throttle = _throttle01 * (_afterBurner ? _thrustABSL : _thrustDrySL);
        _lastAppliedThrust = throttle;

        if (_nozzle != null && throttle > 0)
        {
            Vector3 force = _nozzle.forward * throttle;
            _rigidbody.AddForceAtPosition(force, _nozzle.position, ForceMode.Impulse);
        }
    }
}