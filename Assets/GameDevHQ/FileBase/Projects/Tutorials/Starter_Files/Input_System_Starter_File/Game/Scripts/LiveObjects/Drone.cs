using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Game.Scripts.UI;

namespace Game.Scripts.LiveObjects
{
    public class Drone : MonoBehaviour
    {
        private enum Tilt
        {
            NoTilt, Forward, Back, Left, Right
        }

        [SerializeField]
        private Rigidbody _rigidbody;
        [SerializeField]
        private float _speed = 5f;
        private bool _inFlightMode = false;
        [SerializeField]
        private Animator _propAnim;
        [SerializeField]
        private CinemachineVirtualCamera _droneCam;
        [SerializeField]
        private InteractableZone _interactableZone;
        private InputSystem_Actions _input;

        public static event Action OnEnterFlightMode;
        public static event Action onExitFlightmode;

        private void Awake()
        {
            _input = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += EnterFlightMode;
        }

        private void EnterFlightMode(InteractableZone zone)
        {
            if (_inFlightMode != true && zone.GetZoneID() == 4) // drone Scene
            {
                _propAnim.SetTrigger("StartProps");
                _droneCam.Priority = 11;
                _inFlightMode = true;
                _input.Drone.Enable();
                OnEnterFlightMode?.Invoke();
                UIManager.Instance.DroneView(true);
                _interactableZone.CompleteTask(4);
            }
        }

        private void ExitFlightMode()
        {
            _droneCam.Priority = 9;
            _inFlightMode = false;
            _input.Drone.Disable();
            UIManager.Instance.DroneView(false);
        }

        private void Update()
        {
            if (_inFlightMode)
            {
                CalculateTilt();
                CalculateMovementUpdate();

                // if (Input.GetKeyDown(KeyCode.Escape))
                if (_input.Drone.Exit.WasPressedThisFrame())
                {
                    _inFlightMode = false;
                    onExitFlightmode?.Invoke();
                    ExitFlightMode();
                }
            }
        }

        private void FixedUpdate()
        {
            _rigidbody.AddForce(transform.up * (9.81f), ForceMode.Acceleration);
            if (_inFlightMode)
                CalculateMovementFixedUpdate();
        }

        private void CalculateMovementUpdate()
        {
            // if (Input.GetKey(KeyCode.LeftArrow))
            // if (Input.GetKey(KeyCode.RightArrow))
            float rotate = _input.Drone.Rotate.ReadValue<float>();
            if (Mathf.Abs(rotate) > 0.01f)
            {
                var tempRot = transform.localRotation.eulerAngles;
                tempRot.y += rotate * (_speed / 3);
                transform.localRotation = Quaternion.Euler(tempRot);
            }
        }

        private void CalculateMovementFixedUpdate()
        {
            // if (Input.GetKey(KeyCode.Space))
            // if (Input.GetKey(KeyCode.V))
            float throttle = _input.Drone.Throttle.ReadValue<float>();
            if (Mathf.Abs(throttle) > 0.01f)
            {
                _rigidbody.AddForce(transform.up * _speed * throttle, ForceMode.Acceleration);
            }
        }

        private void CalculateTilt()
        {
            // if (Input.GetKey(KeyCode.A))
            // else if (Input.GetKey(KeyCode.D))
            // else if (Input.GetKey(KeyCode.W))
            // else if (Input.GetKey(KeyCode.S))
            Vector2 flying = _input.Drone.Flying.ReadValue<Vector2>();

            if (flying.x < -0.1f)
                transform.rotation = Quaternion.Euler(00, transform.localRotation.eulerAngles.y, 30);
            else if (flying.x > 0.1f)
                transform.rotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, -30);
            else if (flying.y > 0.1f)
                transform.rotation = Quaternion.Euler(30, transform.localRotation.eulerAngles.y, 0);
            else if (flying.y < -0.1f)
                transform.rotation = Quaternion.Euler(-30, transform.localRotation.eulerAngles.y, 0);
            else
                transform.rotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, 0);
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= EnterFlightMode;
            _input.Drone.Disable();
        }
    }
}
