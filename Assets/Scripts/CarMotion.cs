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

    private Vector3 _currentCarLocalVelocity;
    private float _carVelocityRatio = 0f;

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
    private Transform _COM;

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
        _currentCarLocalVelocity = transform.InverseTransformDirection(_carRb.velocity);
        _carVelocityRatio = _currentCarLocalVelocity.z / _maxSpeed;
    }

    private void TurnWheels()
    {
        foreach(CarWheel wheel in _steeringWheels)
        {
            if(wheel.Flipped)
                wheel.transform.localRotation = Quaternion.Euler(0, _maxTyreAngleDeg * _steeringCurve.Evaluate(_carVelocityRatio) * _steeringInput + 180, 0);
            else
                wheel.transform.localRotation = Quaternion.Euler(0, _maxTyreAngleDeg * _steeringCurve.Evaluate(_carVelocityRatio) * _steeringInput, 0);
        }
    }

    private void Accelerate()
    {
        foreach (CarWheel wheel in _powerWheels)
        {
            if(_accelerateInput > 0.01 && _carVelocityRatio < 0.98f)
                wheel.ApplyTorque(_torqueCurve.Evaluate(_carVelocityRatio) * _accelerateInput);
        }
    }

    private void Brake()
    {
        if (_carVelocityRatio > 0.1)
            foreach (CarWheel wheel in _brakeWheels)
            {
                wheel.BrakeFactor = 0.06f + (float)Math.Clamp((_brakeCurve.Evaluate(_carVelocityRatio) * _reverseInput), 0, 0.98);
            }
        else
        {
            foreach (CarWheel wheel in _brakeWheels)
            {
                wheel.BrakeFactor = 0.02f;
            }
            foreach (CarWheel wheel in _powerWheels)
            {
                wheel.BrakeFactor = 0.02f;
                if (_reverseInput > 0.01 && _carVelocityRatio > -0.28f)
                    wheel.ApplyTorque(-_torqueCurve.Evaluate(_carVelocityRatio) * _reverseInput);
            }
        }
    }

    #region inputHandling

    void OnDrive(InputValue triggerValue)
    {
        _accelerateInput = triggerValue.Get<float>();
    }

    void OnBrake(InputValue triggerValue)
    {
        _reverseInput = triggerValue.Get<float>();
    }

    void OnSteering(InputValue stickValue) 
    {
        _steeringInput = stickValue.Get<float>();
    }

    #endregion
}
