using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Glider : MonoBehaviour
{
    [SerializeField] private Transform _wingCP;

    [Header("Плотность воздуха")]
    [SerializeField] private float _airDensity = 1.225f;

    [Header("Аэродиномические характеристики крыла")]
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
    private bool IsGround;
    private float _startPosition;

    private JetEngine _jetEngine;

    // Стили для GUI
    private GUIStyle _containerStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _valueStyle;
    private Texture2D _bgTexture;
    private bool _stylesInitialized = false;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        if (_jetEngine == null)
        {
            _jetEngine = GetComponent<JetEngine>();
        }
    }

    private void FixedUpdate()
    {

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

        if (_cd != 0) _qlidek = _cl / _cd;
        else _qlidek = 0;

        Vector3 Ddir = -flowDir;

        Vector3 liftDir = Vector3.Cross(lhs: flowDir, rhs: ySpan);
        liftDir.Normalize();

        Vector3 L = _lMag * liftDir;
        Vector3 D = _dMag * Ddir;

        _rigidbody.AddForceAtPosition(L + D, _wingCP.position, ForceMode.Force);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            _startPosition = transform.position.y;
            IsGround = true;
        }
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _bgTexture = new Texture2D(1, 1);
        _bgTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.15f));
        _bgTexture.Apply();

        _containerStyle = new GUIStyle(GUI.skin.box);
        _containerStyle.normal.background = _bgTexture;
        _containerStyle.padding = new RectOffset(15, 15, 15, 15);


        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.fontSize = 14;
        _labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        _labelStyle.alignment = TextAnchor.MiddleLeft;

        _valueStyle = new GUIStyle(GUI.skin.label);
        _valueStyle.fontSize = 14;
        _valueStyle.fontStyle = FontStyle.Bold;
        _valueStyle.normal.textColor = Color.white;
        _valueStyle.alignment = TextAnchor.MiddleRight;

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();

        GUILayout.BeginArea(new Rect(20, 20, 320, 600));
        GUILayout.BeginVertical(_containerStyle);




        DrawDataRow("Скорость (TAS)", $"{_speadMS:F1} м/с / {(int)(_speadMS * 3.6f)} км/ч");
        DrawDataRow("Высота (Alt)", $"{transform.position.y:F1} м");
        DrawDataRow("Верт. скорость", $"{_rigidbody.linearVelocity.y:F1} м/с");


        DrawDataRow("Угол атаки (AoA)", $"{_alphaRad * Mathf.Rad2Deg:F1}°");
        DrawDataRow("Коэф. подъемной (Cl)", $"{_cl:F3}");
        DrawDataRow("Коэф. сопр. (Cd)", $"{_cd:F4}");
        DrawDataRow("Качество (L/D)", $"{_qlidek:F1}");

        DrawDataRow("Сила подъемная", $"{(int)_lMag} Н");
        DrawDataRow("Сила сопр.", $"{(int)_dMag} Н");
        DrawDataRow("Дин. напор (Q)", $"{(int)_qDyn} Па");

        if (_jetEngine != null)
        {

            DrawDataRow("Тяга (Throttle)", $"{_jetEngine._throttle01:P0}");
            DrawDataRow("Форсаж", _jetEngine._afterBurner ? "ACTIVE" : "OFF");
            DrawDataRow("Тяга (Thrust)", $"{(int)_jetEngine._lastAppliedThrust} Н");
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawDataRow(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, _labelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, _valueStyle);
        GUILayout.EndHorizontal();
    }

}