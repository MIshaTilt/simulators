using UnityEngine;
using UnityEngine.InputSystem;

public class JetEngine : MonoBehaviour
{

    [SerializeField] private Transform _nozzle;// soplo dvigatelya
    [SerializeField] private InputActionAsset _actionAsset;

    [Header("т€га")]
    [SerializeField] private float _thrustDrySl = 79000f;// sukhoi regim
    [SerializeField] private float _thrustABSL = 129000f;// forsage regim

    [SerializeField] private float _throttleRate = 1f;// skorost` izmenenia RUD - rychag upravlenia dvigatelem
    [SerializeField] private float _throttleStep = 0.05f; // shag izmenenia po x/z

    private Rigidbody _rigidbody;

    //sostoyanie dvigla
    private float _throttle01;//0...1
    private bool _afterBurner;//AB on/off

    private float _speedMS;
    private float _lastAppliedthrust;

    //input
    private InputAction _throttleUpHold;//shift
    private InputAction _throttleDownHold;//lCtrl
    private InputAction _throttleStepUp;//shag po X
    private InputAction _throttleStepDown;//shag po Y
    private InputAction _toggleAB;//lAlt

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _throttle01 = 0.0f;
        _afterBurner = false;

        InitializeActions();

        
    }

    private void AdjustThrotlle(float delta)
    {
        _throttle01 = Mathf.Clamp01(_throttle01 * delta);
    }

    private void InitializeActions()
    {
        var map = _actionAsset.FindActionMap("JetEngine");
        _throttleUpHold = map.FindAction("ThrottleUp");
        _throttleDownHold = map.FindAction("ThrottleDown");
        _throttleStepUp = map.FindAction("ThrottleStepUp");
        _throttleStepDown = map.FindAction("ThrottleStepDown");
        _toggleAB = map.FindAction("toggleAB");

        _throttleStepUp.performed += _ => AdjustThrotlle(+_throttleStep);
        _throttleStepDown.performed += _ => AdjustThrotlle(-_throttleStep);
        _toggleAB.performed += _ => { _afterBurner = !_afterBurner; };
    }

    private void OnEnable()
    {
        _throttleUpHold.Enable();
        _throttleDownHold.Enable();
        _throttleStepUp.Enable();
        _throttleStepDown.Enable();
        _toggleAB.Enable();
    }

    private void OnDisable()
    {
        _throttleUpHold.Disable();
        _throttleDownHold.Disable();
        _throttleStepUp.Disable();
        _throttleStepDown.Disable();
        _toggleAB.Disable();
    }

    private void FixedUpdate()
    {
        _speedMS = _rigidbody.linearVelocity.magnitude;

        //plavnoe izmenenie RUD po uderzhaniyu
        float dt = Time.fixedDeltaTime;

        if (_throttleUpHold.IsPressed())
        {
            _throttle01 = Mathf.Clamp01(_throttle01 + _throttleRate * dt);
        }
        if (_throttleDownHold.IsPressed())
        {
            _throttle01 = Mathf.Clamp01(_throttle01 - _throttleRate * dt);
        }

        //raschet tyagi (bez popravok visoti/skorosti)
        float throttle = _throttle01 * (_afterBurner ? _thrustABSL : _thrustDrySl);
        _lastAppliedthrust = throttle;

        if (_nozzle != null && throttle > 0) {
            Vector3 force = _nozzle.forward * throttle;
            _rigidbody.AddForceAtPosition(force, _nozzle.position, ForceMode.Force);
        }
    }

    private void OnGUI()
    {
        GUI.color = Color.black;
        GUILayout.Label(text: $"{_throttle01}");
    }
}
