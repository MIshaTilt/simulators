using UnityEngine;
using UnityEngine.InputSystem;

public class KartAero : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private InputActionAsset inputAsset;

    [Header("Aero Drag")]
    [SerializeField] private float airDensity = 1.225f;
    [SerializeField] private float dragCoefficient = 0.9f; // Cx
    [SerializeField] private float frontalArea = 0.6f;     // A

    [Header("Rear Wing & DRS")]
    [SerializeField] private Transform rearWing;
    [SerializeField] private float wingArea = 0.4f;
    [SerializeField] private float liftCoefficientSlope = 0.08f; // k
    [SerializeField] private float normalWingAngle = 20f;
    [SerializeField] private float drsWingAngle = 0f;
    
    [Header("Ground Effect")]
    [SerializeField] private float groundEffectStrength = 3000f;
    [SerializeField] private float groundRayLength = 1.0f;

    // Состояния
    private bool _drsActive;
    private InputAction _drsAction;
    private float _currentWingAngle;

    // Телеметрия
    public float CurrentDrag { get; private set; }
    public float CurrentDownforce { get; private set; }
    public bool IsDRSOpen => _drsActive;
    public float CurrentWingAngle => _currentWingAngle;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        
        if (inputAsset != null)
        {
            var map = inputAsset.FindActionMap("Kart"); 
            // Убедитесь, что Action "DRS" создан в Input Actions
            _drsAction = map.FindAction("DRS"); 
        }
        _currentWingAngle = normalWingAngle;
    }

    private void OnEnable() => _drsAction?.Enable();
    private void OnDisable() => _drsAction?.Disable();

    private void Update()
    {
        // Логика переключения DRS
        if (_drsAction != null)
        {
            // Режим удержания кнопки (как в примере) или переключения
            _drsActive = _drsAction.IsPressed();
        }

        // Плавная смена угла крыла
        float targetAngle = _drsActive ? drsWingAngle : normalWingAngle;
        _currentWingAngle = Mathf.MoveTowards(_currentWingAngle, targetAngle, Time.deltaTime * 100f);
        
        // Визуализация (опционально)
        if (rearWing)
        {
             // Предполагаем, что крыло вращается по оси X
             Vector3 localRot = rearWing.localEulerAngles;
             localRot.x = _currentWingAngle; 
             // Внимание: углы Эйлера могут быть капризными, для простоты примера:
             // Лучше использовать rearWing.localRotation = Quaternion.Euler(...)
        }
    }

    private void FixedUpdate()
    {
        ApplyDrag();
        ApplyWingDownforce();
        ApplyGroundEffect();
    }

    private void ApplyDrag()
    {
        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed < 0.1f) 
        {
            CurrentDrag = 0f;
            return;
        }

        // Fd = 0.5 * rho * Cx * A * v^2
        float dragForce = 0.5f * airDensity * dragCoefficient * frontalArea * speed * speed;
        CurrentDrag = dragForce;

        Vector3 dragVec = -v.normalized * dragForce;
        rb.AddForce(dragVec, ForceMode.Force);
    }

    private void ApplyWingDownforce()
    {
        if (rearWing == null) return;
        
        float speed = rb.linearVelocity.magnitude;
        if (speed < 0.1f)
        {
            CurrentDownforce = 0f;
            return;
        }

        // Cl = k * alpha
        float alphaRad = _currentWingAngle * Mathf.Deg2Rad;
        float Cl = liftCoefficientSlope * alphaRad;

        // F_down = 0.5 * rho * Cl * A * v^2
        float downforce = 0.5f * airDensity * Cl * wingArea * speed * speed;
        CurrentDownforce = downforce;

        Vector3 forceVec = -transform.up * downforce; // Давим вниз относительно авто
        rb.AddForceAtPosition(forceVec, rearWing.position, ForceMode.Force);
    }

    private void ApplyGroundEffect()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, groundRayLength))
        {
            float h = hit.distance;
            if (h < 0.05f) h = 0.05f; // Защита от деления на ноль

            // F_ge = C_ge / h
            float geForce = groundEffectStrength / h;
            
            rb.AddForce(-transform.up * geForce, ForceMode.Force);
        }
    }
}