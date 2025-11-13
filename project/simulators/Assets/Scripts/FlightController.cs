using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]


public class FlightController : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset _playerInput;

    private Vector3 _maxRateDeg = new Vector3(90, 90, 120);
    [SerializeField] private Vector3 _kp = new Vector3(3, 2, 3);
    [SerializeField] private Vector3 _kd = new Vector3(0.8f, 0.6f, 0.9f);

    [SerializeField] private Vector3 _maxTorque = new Vector3(30, 25, 35);
    [SerializeField] private float _deadZone = 0.05f;
    [SerializeField] private Vector2 _attHoldKp = new Vector2(2, 2);
    [SerializeField] private float _attHoldMaxRate = 45f;

    private Rigidbody _rigidbody;
    private InputAction _yaw;
    private InputAction _pitch;
    private InputAction _roll;
    private InputAction _hold;

    private float _targetPitchDeg;
    private float _targetRollDeg;
    private bool _isHolding;

    private Vector3 _omegaBodyDeg;

    private void Awake() => Initialize();

    private void Initialize()
    {
        _playerInput.Enable();
        _rigidbody = GetComponent<Rigidbody>();

        var map = _playerInput.FindActionMap("Player");
        _pitch = map.FindAction("Pitch");
        _roll = map.FindAction("Roll");
        _yaw = map.FindAction("Yaw");
        _hold = map.FindAction("HoldAttr");
    }

    private void OnEnable()
    {
        _hold.performed += OnHoldOn;
        _hold.canceled += OnHoldOff;



    }


    private void OnDisable()
    {
        _hold.performed -= OnHoldOn;
        _hold.canceled -= OnHoldOff;
    }

    void OnHoldOn(InputAction.CallbackContext _)
    {
        _isHolding = true;
        var e = GetLocalPitchRollDeg();
        _targetPitchDeg = e.xPitch;
        _targetRollDeg = e.zRoll;
        Debug.Log(1);
    }



    void OnHoldOff(InputAction.CallbackContext _)
    {
        _isHolding = false;

    }

    float NormalizeAngle(float a) => (a > 180) ? a - 360 : a;

    private (float xPitch, float zRoll) GetLocalPitchRollDeg()
    {
        Vector3 e = transform.localEulerAngles;
        float pitch = NormalizeAngle(e.x);
        float roll = NormalizeAngle(e.z);

        return (pitch, roll);

    }

    Vector3 ReadRateComandDeg()
    {
        float uPitch = _pitch.ReadValue<float>();
        float uRoll = _roll.ReadValue<float>();
        float uYaw = _yaw.ReadValue<float>();

        Vector3 max = _maxRateDeg;
        return new Vector3(uPitch * max.x, uYaw * max.y, uRoll * max.z);
    }

    Vector3 GenerateHoldRateDeg()
    {
        var e = GetLocalPitchRollDeg();
        float errPitch = Mathf.DeltaAngle(e.xPitch, _targetPitchDeg);
        float errRoll = Mathf.DeltaAngle(e.zRoll, _targetRollDeg);

        float wPitch = Mathf.Clamp(errPitch * _attHoldKp.x, -_attHoldMaxRate, _attHoldMaxRate);
        float wRoll = Mathf.Clamp(errRoll * _attHoldKp.y, -_attHoldMaxRate, _attHoldMaxRate);

        return new Vector3(wPitch, 0, wRoll);
    }

    private void FixedUpdate()
    {
        Vector3 omega = _rigidbody.angularVelocity;
        Vector3 omegaBody = transform.InverseTransformDirection(omega);
        _omegaBodyDeg = omegaBody * Mathf.Rad2Deg;

        Vector3 rateCmdDeg = ReadRateComandDeg();

        if (_isHolding) rateCmdDeg = GenerateHoldRateDeg();

        Vector3 errDeg = rateCmdDeg - _omegaBodyDeg;

        Vector3 tau = new Vector3(
           _kp.x * errDeg.x - _kd.x * _omegaBodyDeg.x,
           _kp.y * errDeg.y - _kd.y * _omegaBodyDeg.y,
           _kp.z * errDeg.z - _kd.z * _omegaBodyDeg.z);

        _rigidbody.AddTorque(tau, ForceMode.Force);
    }

    private float CalculateValue(float value, float deadZone)
    {
        return Mathf.Abs(value) < deadZone ? 0 : value;
    }

    private void OnGUI()
    {
        GUI.color = Color.black;

        GUILayout.BeginArea(new Rect(12, 220, 420, 220));
        GUIStyle style = new GUIStyle();
        style.fontSize = 35;
        GUILayout.Label("FlightController");
        GUILayout.Label($"p={_omegaBodyDeg.x:0}");
        GUILayout.EndArea();
    }
}