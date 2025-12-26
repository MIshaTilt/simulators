using UnityEngine;

public class CarSuspension : MonoBehaviour
{
    [Header("Suspension Points")]
    [SerializeField] private Transform fl;
    [SerializeField] private Transform fr;
    [SerializeField] private Transform rl;
    [SerializeField] private Transform rr;

    [Header("Visual Wheels (Meshes)")]
    [SerializeField] private Transform visualFL; 
    [SerializeField] private Transform visualFR;
    [SerializeField] private Transform visualRL;
    [SerializeField] private Transform visualRR;

    [Header("Suspension Settings")]
    [SerializeField] private float restLength = 0.4f; 
    [SerializeField] private float springTravel = 0.2f;
    [SerializeField] private float springStiffness = 30000f;
    [SerializeField] private float damperStiffness = 4000f;
    [SerializeField] private float wheelRadius = 0.35f;

    [Header("Anti-Roll Bar")]
    [SerializeField] private float frontAntiRollStiffness = 8000f;
    [SerializeField] private float rearAntiRollStiffness = 6000f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private Rigidbody rb;

    private float lastFLcompression;
    private float lastFRcompression;
    private float lastRLcompression;
    private float lastRRcompression;

    public float ForceFL { get; private set; }
    public float ForceFR { get; private set; }
    public float ForceRL { get; private set; }
    public float ForceRR { get; private set; }
    
    public float CompressionFL => lastFLcompression;
    public float CompressionFR => lastFRcompression;
    public float CompressionRL => lastRLcompression;
    public float CompressionRR => lastRRcompression;

    public float HitDistFL { get; private set; }
    public float HitDistFR { get; private set; }
    public float HitDistRL { get; private set; }
    public float HitDistRR { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // Передаем и точку подвески (fl), и визуальную модель (visualFL)
        ForceFL = SimulateWheel(fl, visualFL, ref lastFLcompression, out float distFL);
        ForceFR = SimulateWheel(fr, visualFR, ref lastFRcompression, out float distFR);
        ForceRL = SimulateWheel(rl, visualRL, ref lastRLcompression, out float distRL);
        ForceRR = SimulateWheel(rr, visualRR, ref lastRRcompression, out float distRR);

        HitDistFL = distFL; HitDistFR = distFR;

        ApplyAntiRollBars();
    }

    private float SimulateWheel(Transform pivot, Transform visualWheel, ref float lastCompression, out float hitDistance)
    {
        Vector3 origin = pivot.position;
        Vector3 direction = -pivot.up;
        float maxDist = restLength + springTravel + wheelRadius;
        
        hitDistance = maxDist; 
        float totalForce = 0f;
        float currentLenForVisual = maxDist;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDist))
        {
            hitDistance = hit.distance;
            float currentLength = hit.distance - wheelRadius;
            
            currentLength = Mathf.Clamp(currentLength, restLength - springTravel, restLength + springTravel);

            float compression = restLength - currentLength;
            float springForce = compression * springStiffness;
            float compressionVelocity = (compression - lastCompression) / Time.fixedDeltaTime;
            float damperForce = compressionVelocity * damperStiffness;

            totalForce = springForce + damperForce;

            Vector3 forceVector = pivot.up * totalForce;
            rb.AddForceAtPosition(forceVector, pivot.position, ForceMode.Force);

            lastCompression = compression;
            
            currentLenForVisual = hit.distance;
        }
        else
        {
            lastCompression = -springTravel; 
            totalForce = 0f;
            currentLenForVisual = maxDist; 
        }

        if (visualWheel != null)
        {
            
            float visualDist = currentLenForVisual - wheelRadius;
            
            visualDist = Mathf.Clamp(visualDist, restLength - springTravel, restLength + springTravel);

            visualWheel.position = origin + direction * visualDist;
            
        }

        return totalForce;
    }


    private void ApplyAntiRollBars()
    {
        float frontDiff = lastFLcompression - lastFRcompression;
        float frontForce = frontDiff * frontAntiRollStiffness;

        if (lastFLcompression > -springTravel + 0.001f)
            rb.AddForceAtPosition(-fl.up * frontForce, fl.position, ForceMode.Force);
        if (lastFRcompression > -springTravel + 0.001f)
            rb.AddForceAtPosition(fr.up * frontForce, fr.position, ForceMode.Force);

        float rearDiff = lastRLcompression - lastRRcompression;
        float rearForce = rearDiff * rearAntiRollStiffness;

        if (lastRLcompression > -springTravel + 0.001f)
            rb.AddForceAtPosition(-rl.up * rearForce, rl.position, ForceMode.Force);
        if (lastRRcompression > -springTravel + 0.001f)
            rb.AddForceAtPosition(rr.up * rearForce, rr.position, ForceMode.Force);
    }


    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        DrawWheelGizmo(fl);
        DrawWheelGizmo(fr);
        DrawWheelGizmo(rl);
        DrawWheelGizmo(rr);
    }

    private void DrawWheelGizmo(Transform pivot)
    {
        if (pivot == null) return;

        Vector3 origin = pivot.position;
        Vector3 direction = -pivot.up;

        float minLen = restLength - springTravel; 
        float maxLen = restLength + springTravel; 
        float rayLen = maxLen + wheelRadius;

        Gizmos.color = Color.white; 
        Vector3 pointMin = origin + direction * minLen;
        Vector3 pointMax = origin + direction * maxLen;
        
        Gizmos.DrawLine(origin, pointMax);
        Gizmos.DrawLine(pointMin + pivot.right * 0.05f, pointMin - pivot.right * 0.05f);
        Gizmos.DrawLine(pointMax + pivot.right * 0.05f, pointMax - pivot.right * 0.05f);

        Vector3 pointRest = origin + direction * restLength;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pointRest + pivot.right * 0.1f, pointRest - pivot.right * 0.1f);

        bool isHit = Physics.Raycast(origin, direction, out RaycastHit hit, rayLen);
        
        Vector3 wheelCenterPos;
        
        if (isHit)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, hit.point);
            
            Gizmos.DrawWireSphere(hit.point, 0.05f);

            wheelCenterPos = hit.point + pivot.up * wheelRadius;
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + direction * rayLen);
            
            wheelCenterPos = origin + direction * maxLen;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(wheelCenterPos, wheelRadius);

        DrawSpring(origin, wheelCenterPos);
    }

    private void DrawSpring(Vector3 start, Vector3 end)
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        int segments = 8;
        Vector3 prev = start;
        Vector3 dir = (end - start).normalized;
        float dist = Vector3.Distance(start, end);
        float step = dist / segments;
        

        Vector3 right = Vector3.Cross(dir, Vector3.forward).normalized * 0.1f;
        if (right.sqrMagnitude < 0.001f) right = Vector3.Cross(dir, Vector3.right).normalized * 0.1f;

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i * step;
            Vector3 currentBase = start + dir * t;
            Vector3 offset = (i % 2 == 0) ? right : -right;
            if (i == segments) offset = Vector3.zero;

            Vector3 current = currentBase + offset;
            Gizmos.DrawLine(prev, current);
            prev = current;
        }
    }
}