using System;
using UnityEngine;

namespace forces
{
    public class QuadraticDrag : MonoBehaviour
    {
        private float mass = 1f;
        private float radius = 0.1f;
        private float dragCoefficient = 0.47f;
        private float airDensity = 1.225f;
        private Vector3 wind = Vector3.zero;

        private Rigidbody _rb;
        private TargetSpawner spawner;

        private float _area;
        private bool _isHit;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            spawner = FindFirstObjectByType<TargetSpawner>();
            Destroy(gameObject, 10f);


        }
        
        private void FixedUpdate()
        {
            Vector3 vRel = _rb.linearVelocity - wind;
            float speed = vRel.magnitude;
            if (speed < 1e-6f) return;

            Vector3 drag = -0.5f * airDensity * dragCoefficient * _area * speed * vRel;
            _rb.AddForce(drag, ForceMode.Force);
        }
        
        public void SetPhysicalParams(float mass, float radius, float dragCoefficient, float airDensity, 
            Vector3 wind, Vector3 initialVelocity)
        {
            this.mass = Mathf.Max(0.001f, mass);
            this.radius = Mathf.Max(0.001f, radius);
            this.dragCoefficient = Mathf.Max(0f, dragCoefficient);
            this.airDensity = Mathf.Max(0f, airDensity);
            this.wind = wind;
            
            gameObject.transform.localScale = new Vector3(radius, radius, radius);
            
            _rb.mass = mass;
            _rb.linearDamping = 0f;
            _rb.useGravity = true;
            _rb.linearVelocity = initialVelocity;
            
            _area = Mathf.PI * radius * radius;
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