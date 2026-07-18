using System;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

namespace Game.Scripts.LiveObjects
{
    public class Forklift : MonoBehaviour
    {
        [SerializeField]
        private GameObject _lift, _steeringWheel, _leftWheel, _rightWheel, _rearWheels;
        [SerializeField]
        private Vector3 _liftLowerLimit, _liftUpperLimit;
        [SerializeField]
        private float _speed = 5f, _liftSpeed = 1f;
        [SerializeField]
        private CinemachineVirtualCamera _forkliftCam;
        [SerializeField]
        private GameObject _driverModel;
        private bool _inDriveMode = false;
        [SerializeField]
        private InteractableZone _interactableZone;

        private InputSystem_Actions _input;

        public static event Action onDriveModeEntered;
        public static event Action onDriveModeExited;

        private void Awake()
        {
            _input = new InputSystem_Actions();
        }

        private void Start()
        {
            // Forklift body blocks the interact trigger — let the player walk through it.
            var playerCol = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Collider>();
            var forkliftCol = GetComponent<Collider>();
            if (playerCol != null && forkliftCol != null)
                Physics.IgnoreCollision(playerCol, forkliftCol, true);
        }

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += EnterDriveMode;
        }

        private void EnterDriveMode(InteractableZone zone)
        {
            if (_inDriveMode != true && zone.GetZoneID() == 5) //Enter ForkLift
            {
                _inDriveMode = true;
                _forkliftCam.Priority = 11;
                onDriveModeEntered?.Invoke();
                _driverModel.SetActive(true);
                _interactableZone.CompleteTask(5);
                _input.Forklift.Enable();
            }
        }

        private void ExitDriveMode()
        {
            _inDriveMode = false;
            _forkliftCam.Priority = 9;
            _driverModel.SetActive(false);
            onDriveModeExited?.Invoke();
            _input.Forklift.Disable();
        }

        private void Update()
        {
            if (_inDriveMode == true)
            {
                LiftControls();
                CalcutateMovement();
                //if (Input.GetKeyDown(KeyCode.Escape))
                if (_input.Forklift.Exit.WasPressedThisFrame())
                    ExitDriveMode();
            }

        }

        private void CalcutateMovement()
        {
            //float h = Input.GetAxisRaw("Horizontal");
            // float v = Input.GetAxisRaw("Vertical");
            Vector2 input = _input.Forklift.Driving.ReadValue<Vector2>();
            var direction = new Vector3(0, 0, input.y);
            var velocity = direction * _speed;

            transform.Translate(velocity * Time.deltaTime);

            if (Mathf.Abs(input.y) > 0)
            {
                var tempRot = transform.rotation.eulerAngles;
                tempRot.y += input.x * _speed / 2;
                transform.rotation = Quaternion.Euler(tempRot);
            }
        }

        private void LiftControls()
        {
            // if (Input.GetKey(KeyCode.R))
            // else if (Input.GetKey(KeyCode.T))
            if (_input.Forklift.Lift.IsPressed())
                LiftUpRoutine();
            else if (_input.Forklift.Down.IsPressed())
                LiftDownRoutine();
        }

        private void LiftUpRoutine()
        {
            if (_lift.transform.localPosition.y < _liftUpperLimit.y)
            {
                Vector3 tempPos = _lift.transform.localPosition;
                tempPos.y += Time.deltaTime * _liftSpeed;
                _lift.transform.localPosition = new Vector3(tempPos.x, tempPos.y, tempPos.z);
            }
            else if (_lift.transform.localPosition.y >= _liftUpperLimit.y)
                _lift.transform.localPosition = _liftUpperLimit;
        }

        private void LiftDownRoutine()
        {
            if (_lift.transform.localPosition.y > _liftLowerLimit.y)
            {
                Vector3 tempPos = _lift.transform.localPosition;
                tempPos.y -= Time.deltaTime * _liftSpeed;
                _lift.transform.localPosition = new Vector3(tempPos.x, tempPos.y, tempPos.z);
            }
            else if (_lift.transform.localPosition.y <= _liftUpperLimit.y)
                _lift.transform.localPosition = _liftLowerLimit;
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= EnterDriveMode;
        }

    }
}