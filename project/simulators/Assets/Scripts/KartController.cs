using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]

public class KartController : MonoBehaviour
{
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private Transform _rearLeftWheel;
    [SerializeField] private Transform _rearRightWheel;

    [SerializeField] private InputActionAsset _playerInput;

    [SerializeField, Range(0, 1)] private float _frontAxisShare = 0.5f;
    [Header("Engine & drivetrain")]
    [SerializeField] private KartEngine _engine;
    [SerializeField] private float _gearRatio = 8f;
    [SerializeField] private float _drivetrainEfficiency = 0.9f;
    [SerializeField] private float _wheelRadius = 0.3f;
    private InputAction _moveAction;

    private float _throttleInput;
    private float _steepInput;

    private float _frontLeftNormalForce, _frontRightNormalForce, _rearLeftNormalForce, _rearRightNormalForce;

    private Rigidbody _rigidbody;

    private Vector3 g = Physics.gravity;

    [SerializeField] private float maxSpeed = 20;

    [SerializeField] private float maxSteeringAngle;
    private Quaternion frontLeftInitialRot;
    private Quaternion frontRightInitialRot;

    [SerializeField] private float frictionCoefficient = 1f;
    [SerializeField] private float lateralStiffnes = 80f;
    [SerializeField] private float rollingResistance;

    private void Awake()
    {
        _playerInput.Enable();
        _rigidbody = GetComponent<Rigidbody>();
        var map = _playerInput.FindActionMap("Kart");
        _moveAction = map.FindAction("Move");

        frontLeftInitialRot = _frontLeftWheel.localRotation;
        frontRightInitialRot = _frontRightWheel.localRotation;

        ComputeStaticWheelLoad();
    }


    private void OnDisable()
    {
        _playerInput.Disable();
    }

    void RotateFrontWheels()
    {
        float steerAngle = maxSteeringAngle * _steepInput;
        Quaternion steerRot = Quaternion.Euler(0, steerAngle, 0);
        _frontLeftWheel.localRotation = frontLeftInitialRot * steerRot;
        _frontRightWheel.localRotation = frontRightInitialRot * steerRot;
    }

    private void Update()
    {
        ReadInput();
        RotateFrontWheels();
        Debug.Log(_steepInput);

    }

    private void ReadInput()
    {
        Vector2 move = _moveAction.ReadValue<Vector2>();
        _steepInput = Mathf.Clamp(move.x, -1, 1);
        _throttleInput = Mathf.Clamp(move.y, -1, 1);

    }

    void ComputeStaticWheelLoad()
    {
        float mass = _rigidbody.mass;
        float totalWeight = mass * Mathf.Abs(g.y);

        float frontWeight = totalWeight * _frontAxisShare;
        float rearWeight = totalWeight - frontWeight;

        _frontRightNormalForce = frontWeight * 0.5f;
        _frontLeftNormalForce = _frontRightNormalForce;
        _rearRightNormalForce = rearWeight * 0.5f;
        _rearLeftNormalForce = _rearRightNormalForce;
    }
    private void ApplyEngineForces()
    {
        Vector3 forward = transform.forward;
        float speedAlongForward = Vector3.Dot(_rigidbody.linearVelocity, forward);

        if (_throttleInput > 0 && speedAlongForward > maxSpeed) return;

        float driveTorque = _engine.Simulate(_throttleInput, speedAlongForward, Time.fixedDeltaTime);

        float driveForcePerWheel = driveTorque / _wheelRadius / 2;

        Vector3 forceRearLeft = forward * driveForcePerWheel;
        Vector3 forceRearRight = forceRearLeft * driveForcePerWheel;

        _rigidbody.AddForceAtPosition(forceRearLeft, _rearLeftWheel.position, ForceMode.Force);
       _rigidbody.AddForceAtPosition(forceRearRight, _rearRightWheel.position, ForceMode.Force);

    }

    private void FixedUpdate()
    {
        ApplyEngineForces();
        ApplyWheelForce(_frontLeftWheel, _frontLeftNormalForce, isSteer: true, isDrive: false);
        ApplyWheelForce(_frontRightWheel, _frontRightNormalForce, isSteer: true, isDrive: false);
        ApplyWheelForce(_rearLeftWheel, _rearLeftNormalForce, isSteer: false, isDrive: true);
        ApplyWheelForce(_rearRightWheel, _rearRightNormalForce, isSteer: false, isDrive: true);

    }

    void ApplyWheelForce(Transform wheel, float normalForce, bool isSteer, bool isDrive)
    {
        Vector3 wheelPos = wheel.position;
        Vector3 wheelForward = wheel.forward;
        Vector3 wheelRight = wheel.right;

        Vector3 velocity = _rigidbody.GetPointVelocity(wheelPos);

        float vlong = Vector3.Dot(velocity, wheelForward);
        float vlat = Vector3.Dot(velocity, wheelRight);

        float Fx = 0f;
        float Fy = 0f;

        if (isDrive)
        {
            Vector3 bodyForward = transform.forward;
            float speedAlongForward = Vector3.Dot(_rigidbody.linearVelocity, bodyForward);

            if (!(_throttleInput > 0) && speedAlongForward > maxSpeed)
            {
                float engineTorque = _engine.Simulate(_throttleInput, speedAlongForward, Time.fixedDeltaTime);
                float totalWheelTorque = engineTorque * _gearRatio * _drivetrainEfficiency;
                float wheelTorque = totalWheelTorque * 0.5f;
                Fx += wheelTorque / _wheelRadius;
            }


            float rooling = -rollingResistance * vlong;
            Fy += rooling;


        }
        else if (isSteer)
        {
            float rooling = -rollingResistance * vlong;
            Fx += rooling;
        }
        float fyRaw = -lateralStiffnes * vlat;
        Fy += fyRaw;


        float frictionlimit = frictionCoefficient * normalForce;
        float forceLenght = Mathf.Sqrt(Fx * Fx + Fy * Fy);

        if (forceLenght > frictionlimit)
        {
            float scale = frictionlimit / forceLenght;
            Fy += scale;
            Fx += scale;
        }
        Vector3 force = wheelForward * Fx + wheelRight * Fy;
        _rigidbody.AddForceAtPosition(force, wheel.position, ForceMode.Force);
    }
}