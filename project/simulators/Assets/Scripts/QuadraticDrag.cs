using UnityEngine;


namespace forces
{
    public class QuadraticDrag : MonoBehaviour
    {
        private float _mass;
        private float _radius;
        private float _dragCoefficient;
        private float _airDensity;
        private Vector3 _wind = Vector3.zero;

        private Rigidbody _rigidbody;
        private TargetSpawner spawner;
        private float _area;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            spawner = FindFirstObjectByType<TargetSpawner>();
            Destroy(gameObject, 10f);
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void FixedUpdate()
        {
            Vector3 vReal = _rigidbody.linearVelocity - _wind;
            float speed = vReal.magnitude;

            Vector3 drag = -0.5f * _airDensity * _dragCoefficient * _area * speed * vReal;
            _rigidbody.AddForce(drag, ForceMode.Force);
        }

        public void SetPhysicsParams(float mass, float radius, float dragCoefficient, float airDensity, Vector3 wind, Vector3 initialVelocity)
        {
            _mass = mass;
            _radius = radius;
            _airDensity = airDensity;
            _wind = wind;

            _rigidbody.mass = _mass;
            _rigidbody.useGravity = true;
            _rigidbody.linearVelocity = initialVelocity;

            _area = _radius * _radius * Mathf.PI;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Target"))
            {
                Destroy(other.gameObject);
                spawner.SpawnTarget();
                spawner.counter++;
                spawner.counterText.text = spawner.counter.ToString();
            }

        }
    }
}

