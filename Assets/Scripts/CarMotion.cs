using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarMotion : MonoBehaviour
{
    //Spring Values

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
    private LayerMask _drivable;

    private Rigidbody _carRb = null;

    [SerializeField]
    private Transform[] _tyreTransforms = null;

    private const int WHEELCOUNT = 4;
    private bool[] _wheelsGrounded = new bool[WHEELCOUNT];
    private bool _isGrounded = false;

    [SerializeField]
    private float _acceleration = 25f;
    private float _deceleration = 10f;
    private float _maxSpeed = 100f;

    private Vector3 _currentCarLocalVelocity = Vector3.zero;
    private float _carVelocityRatio = 0f;

    private void Start()
    {
        _carRb = GetComponent<Rigidbody>();
        if (_carRb == null)
            Debug.LogError("Rigdbody was not found");
    }

    private void Update()
    {

    }

    private void FixedUpdate()
    {
        UpdateSuspension();
        GroundCheck();
    }



    private void GroundCheck()
    {
        int tempWheelsGrounded = 0;

        for (int wheelIdx = 0; wheelIdx <  WHEELCOUNT; ++wheelIdx)
        {
            if (_wheelsGrounded[wheelIdx])
                ++tempWheelsGrounded;

            if(tempWheelsGrounded > 1)
                _isGrounded = true;
            else
                _isGrounded = false;
        }
    }

    private void UpdateSuspension()
    {
        if (_carRb == null)
            return;

        for (int wheelIdx = 0; wheelIdx < WHEELCOUNT; ++wheelIdx)
        {
            //World space direction of spring force
            Vector3 springDir = _tyreTransforms[wheelIdx].up;

            RaycastHit rayHit;

            float maxLength = _restDist + _maxSpringOffset;

            if (Physics.Raycast(_tyreTransforms[wheelIdx].position, -springDir, out rayHit, maxLength + _wheelRadius, _drivable))
            {
                _wheelsGrounded[wheelIdx] = true;

                float curSpringLength = rayHit.distance - _wheelRadius;


                //World space velocity of this tyre
                Vector3 tyreWorldVel = _carRb.GetPointVelocity(_tyreTransforms[wheelIdx].position);

                //Calc normalized offset
                float springOffset = (_restDist - curSpringLength) / _maxSpringOffset;

                //Calc velocity along spring dir
                //Note: springDir is unit vector
                float vel = Vector3.Dot(springDir, tyreWorldVel);

                //Calculate force and apply at wheel point
                //using simple spring formula:
                //F = (Offset * SpringStrength) - (Vel * Damping)
                float force = (_springStrength * springOffset) - (vel * _dampingDensity);
                _carRb.AddForceAtPosition(force * springDir, _tyreTransforms[wheelIdx].position);

                Debug.DrawLine(_tyreTransforms[wheelIdx].position, rayHit.point, Color.green);
            }
            else
            {
                _wheelsGrounded[wheelIdx] = true;

                Debug.DrawLine(
                    _tyreTransforms[wheelIdx].position, 
                    _tyreTransforms[wheelIdx].position + (_wheelRadius + maxLength) * -springDir, 
                    Color.yellow);
            }
        }
    }
}
