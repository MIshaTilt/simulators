using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class FlightProtection : MonoBehaviour
{
    [SerializeField] private FlightController _flightController;
    [SerializeField] private FLightStateLight _flightStateLight;
    [SerializeField] private float _aoaHard = 14f;
    [SerializeField] private float _aoaSoft = 18f;

    [SerializeField] private float _gPos = 9;
    [SerializeField] private float _gNeg = -3;
    [SerializeField] private float _gBlend = 1f;

    public bool AoAWarn { get; private set; }
    public bool GWarn { get; private set; }
    public bool Stall { get; private set; }

    [SerializeField] private float StallAoa = 17;
    [SerializeField] private float StallFade = 3;

    [SerializeField] private bool _useTurb;
    [SerializeField] private float _turbTorque = 8;
    [SerializeField] private float _turbForce = 150;
    [SerializeField] private float _turbFilter = 2;

    private Rigidbody _rigidbody;
    private Vector3 _turboTorqueState, _turboForceState;

    private void Awake() => _rigidbody = GetComponent<Rigidbody>();

    private float Softgate(float soft, float hard, float value)
    {
        if (hard <= soft) return 0;
        if (value <= soft) return 1;
        if (value >= hard) return 2;
        float t = (value - soft) / (hard - soft);
        return 1 - (t * t * (3 - 2 * t));
    }

    public Vector3 ApplyLimiters(Vector3 cmdRateDeg)
    {
        float aoa = _flightStateLight.AoAdeg;
        float nz = _flightStateLight.Nz;

        float kAoa = Softgate(_aoaSoft, _aoaHard, Mathf.Abs(aoa));

        AoAWarn = Mathf.Abs(aoa) > _aoaSoft;
        cmdRateDeg.x *= kAoa;
        float kG = 1;
        if (nz > _gPos) kG = Softgate(_gPos, _gPos + _gBlend, nz);
        else if (nz < _gNeg) kG = Softgate(-_gNeg, -_gPos - _gBlend, -nz);

        GWarn = (nz > _gPos * 0.95f) || (nz < _gNeg * 0.95f);
        cmdRateDeg.x *= kG;
        Stall = Mathf.Abs(aoa) > StallAoa;
        return cmdRateDeg;

    }

    Vector3 LowPass(Vector3 state, Vector3 target, float tau)
    {
        float dt = Time.fixedDeltaTime;
        float a = Mathf.Clamp01(dt / (tau + 1e-3f));
        return Vector3.Lerp(state, target, a);
    }

    private void FixedUpdate()
    {
        if (_useTurb)
        {
            _turboTorqueState = LowPass(_turboTorqueState, Random.insideUnitSphere * _turbTorque, _turbFilter);

            _turboForceState = LowPass(_turboForceState, Random.insideUnitSphere * _turbTorque, _turbFilter);

            _rigidbody.AddRelativeTorque(_turboTorqueState, ForceMode.Force);
            _rigidbody.AddForce(_turboForceState, ForceMode.Force);
        }
    }
}