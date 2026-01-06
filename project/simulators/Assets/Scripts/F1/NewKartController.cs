using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class NewKartController : MonoBehaviour
{
    [Header("Config Asset")]
    [SerializeField] private KartConfig _defaultConfig; 

    [Header("Wheel Transforms")]
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private Transform _rearLeftWheel;
    [SerializeField] private Transform _rearRightWheel;

    [Header("Engine Link")]
    [SerializeField] private KartEngine _engine;
    [SerializeField] private KartAero _aero;
    [SerializeField] private CarSuspension _suspension;

    [Header("Input")]
    [SerializeField] private InputActionAsset _playerInput;
    
    // Внутренние параметры
    private Rigidbody _rb;
    private float _gravity = 9.81f;
    private float _mass;
    private float _frontAxisShare = 0.5f; 
    
    private float _frictionCoeff;
    private float _frontStiffness;
    private float _rearStiffness;
    private float _rollResist;
    private float _maxSteer;
    private float _gearRatio;
    private float _wheelRadius;
    private float _drivetrainEff = 0.9f;
    
    // Состояния
    private float _throttleInput;
    private float _steerInput;
    private bool _isHandbrake;
    private InputAction _moveAction;
    private InputAction _brakeAction;

    private float _flNormal, _frNormal, _rlNormal, _rrNormal;
    private Quaternion _flInitRot, _frInitRot;

    // Телеметрия
    private float _telemetryRearFxSum;   
    private float _telemetryFrontFySum;  
    private float _telemetryFLSlip, _telemetryFRSlip, _telemetryRLSlip, _telemetryRRSlip;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
        var map = _playerInput.FindActionMap("Kart");
        _moveAction = map.FindAction("Move");
        _brakeAction = map.FindAction("Brake"); 

        if (_frontLeftWheel) _flInitRot = _frontLeftWheel.localRotation;
        if (_frontRightWheel) _frInitRot = _frontRightWheel.localRotation;

        if (_defaultConfig != null) ApplyKartConfig(_defaultConfig);
        else Debug.LogError("Assign a KartConfig asset!");
    }

    public void ApplyKartConfig(KartConfig config)
    {
        _rb.mass = config.mass;
        _mass = config.mass;
        _frictionCoeff = config.frictionCoefficient;
        _frontStiffness = config.frontLateralStiffness;
        _rearStiffness = config.rearLateralStiffness;
        _rollResist = config.rollingResistance;
        _maxSteer = config.maxSteerAngle;
        _gearRatio = config.gearRatio;
        _wheelRadius = config.wheelRadius;

        if (_engine != null) _engine.ApplyConfig(config);

        ComputeStaticWheelLoad();
    }

    private void ComputeStaticWheelLoad()
    {
        float totalWeight = _mass * _gravity;
        float frontWeight = totalWeight * _frontAxisShare;
        float rearWeight = totalWeight * (1f - _frontAxisShare);

        _flNormal = frontWeight * 0.5f;
        _frNormal = frontWeight * 0.5f;
        _rlNormal = rearWeight * 0.5f;
        _rrNormal = rearWeight * 0.5f;
    }

    private void OnEnable()
    {
        _playerInput.Enable();
        if (_moveAction != null) _moveAction.Enable();
        if (_brakeAction != null) _brakeAction.Enable();
    }
    private void OnDisable() => _playerInput.Disable();

    private void Update()
    {
        Vector2 move = _moveAction.ReadValue<Vector2>();
        _steerInput = Mathf.Clamp(move.x, -1f, 1f);
        _throttleInput = Mathf.Clamp(move.y, -1f, 1f);
        _isHandbrake = _brakeAction != null && _brakeAction.IsPressed();

        RotateFrontWheels();
    }

    private void RotateFrontWheels()
    {
        float angle = _maxSteer * _steerInput;
        Quaternion rot = Quaternion.Euler(0, angle, 0);
        if (_frontLeftWheel) _frontLeftWheel.localRotation = _flInitRot * rot;
        if (_frontRightWheel) _frontRightWheel.localRotation = _frInitRot * rot;
    }

    private void FixedUpdate()
    {
        _telemetryRearFxSum = 0f;
        _telemetryFrontFySum = 0f;

        float speed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        
        // Двигатель 
        float engineInput = Mathf.Abs(_throttleInput); 
        float torque = _engine.Simulate(engineInput, speed, Time.fixedDeltaTime);
        
        // Общая сила
        float totalDriveForce = (torque * _gearRatio * _drivetrainEff) / _wheelRadius;

        // Направление (вперед/назад)
        float direction = _throttleInput >= 0 ? 1f : -1f;
        
        // Сила на колесо
        float forcePerRearWheel = (totalDriveForce * direction) * 0.5f;

        // Вызовы 
        ApplyWheelForce(_frontLeftWheel, _flNormal, isSteer:true, driveForce:0f, stiffness:_frontStiffness, ref _telemetryFLSlip);
        ApplyWheelForce(_frontRightWheel, _frNormal, isSteer:true, driveForce:0f, stiffness:_frontStiffness, ref _telemetryFRSlip);

        ApplyWheelForce(_rearLeftWheel,  _rlNormal, isSteer:false, driveForce:forcePerRearWheel, stiffness:_rearStiffness, ref _telemetryRLSlip);
        ApplyWheelForce(_rearRightWheel, _rrNormal, isSteer:false, driveForce:forcePerRearWheel, stiffness:_rearStiffness, ref _telemetryRRSlip);
    }

    void ApplyWheelForce(Transform wheel, float N, bool isSteer, float driveForce, float stiffness, ref float outSlip)
    {
        if (!wheel) return;

        Vector3 wPos = wheel.position;
        Vector3 wFwd = wheel.forward;
        Vector3 wRight = wheel.right;
        Vector3 vel = _rb.GetPointVelocity(wPos);

        float vLong = Vector3.Dot(vel, wFwd);
        float vLat = Vector3.Dot(vel, wRight);
        
        outSlip = vLat; 

        float Fx = 0f;
        float Fy = 0f;

        if (driveForce > 0 && vLong > 25f) 
            Fx += 0; 
        else 
            Fx += driveForce;
        
        Fx -= _rollResist * vLong;

        float currentStiffness = stiffness;
        if (!isSteer && _isHandbrake)
        {
            currentStiffness = 0f; //Cα = 0
            Fx *= 0.5f; 
        }

        Fy = -currentStiffness * vLat;

        float limit = _frictionCoeff * N;
        float len = Mathf.Sqrt(Fx*Fx + Fy*Fy);
        if (len > limit && len > 0.001f)
        {
            float scale = limit / len;
            Fx *= scale;
            Fy *= scale;
        }

        if (!isSteer) _telemetryRearFxSum += Fx; 
        else _telemetryFrontFySum += Fy;         

        Vector3 finalForce = wFwd * Fx + wRight * Fy;
        _rb.AddForceAtPosition(finalForce, wPos, ForceMode.Force);
    }

    
}