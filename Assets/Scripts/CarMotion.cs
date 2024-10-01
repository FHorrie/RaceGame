using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class CarMotion : MonoBehaviour
{
    [SerializeField]
    private float _maxTyreAngleDeg = 50f;
    [SerializeField]
    private float _maxSpeed = 20f;

    private float _gearMaxSpeed = 0f;

    private Vector3 _currentCarLocalVelocity;
    private float _carCurrentVelocityRatio = 0f;
    private float _carMaxVelocityRatio = 0f;

    private Rigidbody _carRb = null;

    float _accelerateInput = 0f;
    float _reverseInput = 0f;
    float _steeringInput = 0f;

    [SerializeField]
    private AnimationCurve _torqueCurve;

    [SerializeField]
    private AnimationCurve _steeringCurve;

    [SerializeField]
    private AnimationCurve _brakeCurve;

    [SerializeField]
    private CarWheel[] _steeringWheels;

    [SerializeField]
    private CarWheel[] _powerWheels;

    [SerializeField]
    private CarWheel[] _brakeWheels;

    [SerializeField]
    private float[] _forwardGearRatios;

    [SerializeField]
    private float _reverseGearRatio = 0;

    private int _currentGear = 0;

    [SerializeField]
    private Transform _COM;

    public float MaxSpeed
    {
        get { return _maxSpeed; }
    }

    public float GearMaxSpeed
    {
        get { return _gearMaxSpeed; }
    }

    public float CarCurrentVelocityRatio
    {
        get { return _carCurrentVelocityRatio; }
    }

    private void Start()
    {
        _carRb = GetComponent<Rigidbody>();
        if (_carRb == null)
        {
            Debug.LogError("Rigdbody was not found");
            return;
        }

        _carRb.centerOfMass = _COM.localPosition;

    }

    private void Update()
    {
        TurnWheels();
    }

    private void FixedUpdate()
    {
        CalculateCarVelocity();
        Accelerate();
        Brake();
    }

    private void CalculateCarVelocity()
    {
        _carCurrentVelocityRatio = Mathf.Clamp01(_carRb.velocity.sqrMagnitude / (_gearMaxSpeed * _gearMaxSpeed));
        _carMaxVelocityRatio = Mathf.Clamp01(_carRb.velocity.sqrMagnitude / (_maxSpeed * _maxSpeed));
    }

    private void TurnWheels()
    {
        foreach(CarWheel wheel in _steeringWheels)
        {
            if(wheel.Flipped)
                wheel.transform.localRotation = Quaternion.Euler(0, _maxTyreAngleDeg * _steeringCurve.Evaluate(_carMaxVelocityRatio) * _steeringInput + 180, 0);
            else
                wheel.transform.localRotation = Quaternion.Euler(0, _maxTyreAngleDeg * _steeringCurve.Evaluate(_carMaxVelocityRatio) * _steeringInput, 0);
        }
    }

    private void Accelerate()
    {
        //Split available torque over power wheels
        float torquePerWheel = 1f;
        int torqueSplit = 0;
        foreach (CarWheel wheel in _powerWheels)
        {
            if (wheel.IsGrounded)
                torqueSplit++;
        }

        if(torqueSplit > 0)
            torquePerWheel /= torqueSplit;

        //Add torque to wheels
        foreach (CarWheel wheel in _powerWheels)
        {
            if(_accelerateInput > 0.01f && _carCurrentVelocityRatio < 0.98f)
            {
                wheel.ApplyTorque(_torqueCurve.Evaluate(_carCurrentVelocityRatio) * _accelerateInput * torquePerWheel * Mathf.Sign(_currentGear));
            }
        }
    }

    private void Brake()
    {
        if (_carCurrentVelocityRatio > 0.01f)
            foreach (CarWheel wheel in _brakeWheels)
            {
                wheel.BrakeFactor = wheel.BaseBrakeFactor + (float)Math.Clamp(_brakeCurve.Evaluate(_carCurrentVelocityRatio) * _reverseInput, 0, 1 - (double)wheel.BaseBrakeFactor);
            }
        else
            foreach (CarWheel wheel in _brakeWheels)
            {
                wheel.BrakeFactor = wheel.BaseBrakeFactor;
            }
    }

    private void SetGearRatio()
    {
        //Forward Gear
        if (_currentGear > 0)
            _gearMaxSpeed = _maxSpeed * _forwardGearRatios[_currentGear - 1];
        //Neutral
        else if (_currentGear == 0)
            _gearMaxSpeed = 0;
        //Reverse Gear
        else
            _gearMaxSpeed = -_maxSpeed * _reverseGearRatio;

        Debug.Log(_currentGear);
        Debug.Log(_gearMaxSpeed);
    }

    #region inputHandling

    private void OnDrive(InputValue triggerValue)
    {
        _accelerateInput = triggerValue.Get<float>();
    }

    private void OnBrake(InputValue triggerValue)
    {
        _reverseInput = triggerValue.Get<float>();
    }

    private void OnSteering(InputValue stickValue) 
    {
        _steeringInput = stickValue.Get<float>();
    }

    private void OnShiftUp(InputValue buttonValue)
    {
        if (_currentGear < _forwardGearRatios.Length)
        {
            _currentGear++;
            SetGearRatio();
        }

    }

    private void OnShiftDown(InputValue buttonValue)
    {
        if(_currentGear > -1)
        {
            _currentGear--;
            SetGearRatio();
        }
    }

    #endregion
}
