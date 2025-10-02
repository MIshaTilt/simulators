using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private Transform _wingCP;

    [Header("ѕлотность воздуха")]
    [SerializeField] private float _airDensity = 1.225f;

    [Header("јэродиномические характеристики крыла")]
    [SerializeField] private float _wingArea = 1.5f;
    [SerializeField] private float _wingAspect = 8.0f;

    [SerializeField] private float _wingCDD = 0.02f;

    [SerializeField] private float _wingClaplha = 5.5f;

    private Rigidbody _rigidbody;


    private Vector3 _vPoint;
    private Vector3 _worldVelocity;
    private float _speadMS;
    private float _alphaRad;

    private float _cl, _cd, _qDyn, _lMag, _dMag, _qlidek;


    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }


    private void FixedUpdate()
    {
        // скорость в точке крыла

        _vPoint = _rigidbody.GetPointVelocity(_wingCP.position);
        _speadMS = _vPoint.magnitude;

        Vector3 flowDir = (-_vPoint).normalized;
        Vector3 xChord = _wingCP.forward;
        Vector3 zUP = _wingCP.up;
        Vector3 ySpan = _wingCP.right;


        float flowX = Vector3.Dot(lhs: flowDir, rhs: xChord);
        float flowZ = Vector3.Dot(lhs: flowDir, rhs: zUP);
        _alphaRad = Mathf.Atan2(y: flowZ, flowX);

        _cl = _wingClaplha * _alphaRad;
        _cd = _wingCDD + _cl * _cl / (Mathf.PI * _wingAspect * 0.85f);


        _qDyn = 0.5f * _airDensity * _speadMS * _speadMS;
        _lMag = _qDyn * _wingArea * _cl;
        _dMag = _qDyn * _wingArea * _cd;


        Vector3 Ddir = -flowDir;


        Vector3 liftDir = Vector3.Cross(lhs: flowDir, rhs: ySpan);
        liftDir.Normalize();


        Vector3 L = _lMag * liftDir;
        Vector3 D = _dMag * Ddir;


        _rigidbody.AddForceAtPosition(L + D, _wingCP.position, ForceMode.Force);

        // _worldVelocity = _rigidbody.linearVelocity;
        //_speadMS = _worldVelocity.magnitude;

    }

    private void StepOne()
    {
        Vector3 xChord = _wingCP.forward;//вдоль хорды
        Vector3 zUP = _wingCP.up;// нормаль к поверхности

        Vector3 flowDir = _speadMS > 0 ? _worldVelocity.normalized : _wingCP.forward;


        float flowX = Vector3.Dot(lhs: flowDir, rhs: xChord);
        float flowZ = Vector3.Dot(lhs: flowDir, rhs: zUP);

        _alphaRad = Mathf.Atan2(y: flowZ, flowX);
    }





    /*private void OnGUI()
    {
        GUI.color = Color.black;
        GUILayout.Label(text: $"—корость: {_speadMS:0.0} m/s");
        GUILayout.Label(text: $"”гол атакт: {_alphaRad * Mathf.Deg2Rad:0.0}");
    }*/

}