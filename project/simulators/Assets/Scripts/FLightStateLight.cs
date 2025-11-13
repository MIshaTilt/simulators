using UnityEngine;

public class FLightStateLight : MonoBehaviour
{
    [SerializeField] private Transform _wingChord;

    private const float MinValueForAngleAttack = 1e-3f;

    public float IAS {  get; private set; }

    public float AoAdeg {  get; private set; }

    public float Nz { get; private set; }

    private Rigidbody _rigidbody;
    private Vector3 _vPrev;
    private float _tPrev;

    private void Awake() => Initialize();

    private void Initialize()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _vPrev = _rigidbody.linearVelocity;
        _tPrev = Time.time;
    }

    private void FixedUpdate()
    {
        Vector3 currentVelocity = _rigidbody.linearVelocity;
        IAS = currentVelocity.magnitude;

        if (IAS > MinValueForAngleAttack)
        {
            Vector3 flow = (-currentVelocity).normalized;

            float flowX = Vector3.Dot(flow, _wingChord.forward);
            float flowZ = Vector3.Dot(flow, _wingChord.up);
            AoAdeg = Mathf.Deg2Rad * Mathf.Atan2(flowZ, flowX);
        }
        else {
            AoAdeg = 0;
        }

        float currentTime = Time.time;
        float dt = Mathf.Max(MinValueForAngleAttack, currentTime - _tPrev);
        Vector3 aWorld = (currentVelocity - _vPrev) / dt;
        float aVert = Vector3.Dot(aWorld + Physics.gravity, transform.up);


        Nz = 1f + (aVert / Mathf.Abs(Physics.gravity.y));
        _vPrev = currentVelocity;
        _tPrev = currentTime;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
