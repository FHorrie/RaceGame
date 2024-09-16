using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarWheel : MonoBehaviour
{
    [SerializeField]
    private float _springStrength = 30000f;
    [SerializeField]
    //use formula (2 * sqrt(K * mass) * Zeta) = dampingDensity 
    //Zeta between 0.2 and 1
    //Chosen values give (min: 2190), (max: 10954)
    private float _dampingDensity = 3000f;
    [SerializeField]
    private float _maxSpringOffset = 0.3f;
    [SerializeField]
    private float _restDist = 0.5f;
    [SerializeField]
    private float _wheelRadius = 0.25f;

    [SerializeField]
    private bool _flipped = false;

    private Vector3 _groundedPoint;

    private Rigidbody _carRb = null;

    [SerializeField]
    private float _tyreGripFactor = 0.65f;

    private float _brakeFactor = 0.06f;
    
    [SerializeField]
    private float _tyreWeight = 14f;

    private bool _isGrounded = false;

    [SerializeField]
    private float _peakTorque = 6500f;

    [SerializeField]
    private Transform _wheelVisualTransform = null;

    public bool Flipped
    {
        get { return _flipped; }
    }

    public float BrakeFactor
    {
        get { return _brakeFactor; }
        set { _brakeFactor = value; }
    }

    private void Start()
    {
        _carRb = GetComponentInParent<Rigidbody>();
        if (_carRb == null)
            Debug.LogError("Rigdbody was not found");
    }
    private void Update()
    {
    }

    private void FixedUpdate()
    {
        UpdateTyre();
        RollTyre();
    }

    private void UpdateTyre()
    {
        if (_carRb == null)
            return;

        float maxLength = _restDist + _maxSpringOffset;

        bool rayDidHit = Physics.SphereCast(transform.position, _wheelRadius, -transform.up, out RaycastHit hitInfo, maxLength - _wheelRadius);

        if (rayDidHit)
        {
            _isGrounded = true;

            UpdateSuspension(hitInfo);
            UpdateTyreGrip();
            UpdateFriction();
        }
        else
        {
            _isGrounded = false;

            _wheelVisualTransform.position = transform.position - transform.up * (-_wheelRadius + maxLength);
        }
    }

    private void UpdateSuspension(RaycastHit hitInfo)
    {
        //World space direction of spring force
        Vector3 springForceDir = transform.up;

        //World space velocity of the suspension
        Vector3 tyreWorldVel = _carRb.GetPointVelocity(transform.position);

        //Calc spring offset
        float springOffset = _restDist - hitInfo.distance;

        //Visual ground point
        _groundedPoint = transform.position - springForceDir * (_restDist - springOffset);

        //Calc velocity along spring dir
        //Note: springDir is unit vector
        float vel = Vector3.Dot(springForceDir, tyreWorldVel);

        //Calculate force using simple spring formula:
        float force = (_springStrength * springOffset) - (vel * _dampingDensity);

        //F = (Offset * SpringStrength) - (Vel * Damping)
        _carRb.AddForceAtPosition(force * springForceDir, transform.position);

        //Match wheels to ray transform
        if (_wheelVisualTransform)
        {
            _wheelVisualTransform.position = _groundedPoint;
        }
    }

    private void UpdateTyreGrip()
    {
        //World space velocity of the suspension
        Vector3 tyreWorldVel = _carRb.GetPointVelocity(transform.position);

        //World space direction of the tyre outwards force
        Vector3 lateralDir = transform.right;

        //Tire velocity in steering dir
        //Note: springDir is unit vector
        float steeringVel = Vector3.Dot(lateralDir, tyreWorldVel);

        //the change in velocity that we're looking for is -steeringVel * gripFactor
        float desiredVelChange = -steeringVel * _tyreGripFactor;

        //change turn velocity into acceleration (vel / time)
        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

        //F = Mass * Acceleration
        _carRb.AddForceAtPosition(lateralDir * _tyreWeight * desiredAccel, transform.position);
    }

    public void UpdateFriction()
    {
        //World space velocity of the suspension
        Vector3 tyreWorldVel = _carRb.GetPointVelocity(transform.position);

        //World space direction of the spring force
        Vector3 forwardDir = transform.forward;

        //Tire velocity in steering dir
        //Note: springDir is unit vector
        float frictionVel = Vector3.Dot(forwardDir, tyreWorldVel);

        //the change in velocity that we're looking for is -steeringVel * gripFactor
        float desiredVelChange = -frictionVel * _brakeFactor;

        //change turn velocity into acceleration (vel / time)
        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

        //F = Mass * Acceleration
        _carRb.AddForceAtPosition(forwardDir * _tyreWeight * desiredAccel, transform.position);
    }

    private void RollTyre()
    {
        if (_flipped)
            _wheelVisualTransform.Rotate((Time.fixedDeltaTime * Vector3.Dot(_carRb.velocity, transform.forward) * _wheelRadius * 1000), 0, 0);
        else
            _wheelVisualTransform.Rotate(Time.fixedDeltaTime * Vector3.Dot(_carRb.velocity, transform.forward) * _wheelRadius * 1000, 0, 0);
    }

    public void ApplyTorque(float torqueAmount)
    {
        if (_isGrounded)
        {
            if (_flipped)
                _carRb.AddForceAtPosition(-transform.forward * torqueAmount * _peakTorque, _groundedPoint);
            else
                _carRb.AddForceAtPosition(transform.forward * torqueAmount * _peakTorque, _groundedPoint);
        }
    }
}