using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Scripts.UI;


namespace Game.Scripts.LiveObjects
{
    public class InteractableZone : MonoBehaviour
    {
        private enum ZoneType
        {
            Collectable,
            Action,
            HoldAction
        }

        private enum KeyState
        {
            Press,
            PressHold
        }

        [SerializeField]
        private ZoneType _zoneType;
        [SerializeField]
        private int _zoneID;
        [SerializeField]
        private int _requiredID;
        [SerializeField]
        [Tooltip("Press the (---) Key to .....")]
        private string _displayMessage;
        [SerializeField]
        private GameObject[] _zoneItems;
        private bool _inZone = false;
        private bool _itemsCollected = false;
        private bool _actionPerformed = false;
        [SerializeField]
        private Sprite _inventoryIcon;
        [SerializeField]
        private KeyCode _zoneKeyInput;
        [SerializeField]
        private KeyState _keyState;
        [SerializeField]
        private GameObject _marker;

        private bool _inHoldState = false;
        private InputSystem_Actions _input;

        private static int _currentZoneID = 0;
        public static int CurrentZoneID
        {
            get
            {
                return _currentZoneID;
            }
            set
            {
                _currentZoneID = value;

            }
        }


        public static event Action<InteractableZone> onZoneInteractionComplete;
        public static event Action<int> onHoldStarted;
        public static event Action<int> onHoldEnded;

        private void Awake()
        {
            _input = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += SetMarker;
            _input.Player.Enable();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                TryEnterZone();
        }

        // If the player is already standing in this trigger when CurrentZoneID
        // unlocks (e.g. after finishing the drone), OnTriggerEnter will not fire
        // again. Stay re-checks so forklift/crate/etc. can arm without leaving.
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player") && _inZone == false)
                TryEnterZone();
        }

        private void TryEnterZone()
        {
            if (_currentZoneID <= _requiredID)
                return;

            switch (_zoneType)
            {
                case ZoneType.Collectable:
                    if (_itemsCollected == false)
                    {
                        _inZone = true;
                        if (_displayMessage != null)
                        {
                            string message = $"Press the {_zoneKeyInput.ToString()} key to {_displayMessage}.";
                            UIManager.Instance.DisplayInteractableZoneMessage(true, message);
                        }
                        else
                            UIManager.Instance.DisplayInteractableZoneMessage(true, $"Press the {_zoneKeyInput.ToString()} key to collect");
                    }
                    break;

                case ZoneType.Action:
                    if (_actionPerformed == false)
                    {
                        _inZone = true;
                        if (_displayMessage != null)
                        {
                            string message = $"Press the {_zoneKeyInput.ToString()} key to {_displayMessage}.";
                            UIManager.Instance.DisplayInteractableZoneMessage(true, message);
                        }
                        else
                            UIManager.Instance.DisplayInteractableZoneMessage(true, $"Press the {_zoneKeyInput.ToString()} key to perform action");
                    }
                    break;

                case ZoneType.HoldAction:
                    _inZone = true;
                    if (_displayMessage != null)
                    {
                        string message = $"Hold the {_zoneKeyInput.ToString()} key to {_displayMessage}.";
                        UIManager.Instance.DisplayInteractableZoneMessage(true, message);
                    }
                    else
                        UIManager.Instance.DisplayInteractableZoneMessage(true, $"Hold the {_zoneKeyInput.ToString()} key to perform action");
                    break;
            }
        }

        private void Update()
        {
            if (_inZone == true)
            {
                // if (Input.GetKeyDown(_zoneKeyInput) && _keyState != KeyState.PressHold)
                // Zone 6 (crate) uses Player.Interacttaphold Tap/Hold in Crate.cs
                if (WasInteractPressedThisFrame() && _keyState != KeyState.PressHold && _zoneID != 6)
                {
                    //press
                    switch (_zoneType)
                    {
                        case ZoneType.Collectable:
                            if (_itemsCollected == false)
                            {
                                CollectItems();
                                _itemsCollected = true;
                                UIManager.Instance.DisplayInteractableZoneMessage(false);
                            }
                            break;

                        case ZoneType.Action:
                            if (_actionPerformed == false)
                            {
                                PerformAction();
                                _actionPerformed = true;
                                UIManager.Instance.DisplayInteractableZoneMessage(false);
                            }
                            break;
                    }
                }
                // else if (Input.GetKey(_zoneKeyInput) && _keyState == KeyState.PressHold && _inHoldState == false)
                else if (_input.Player.InteractHold.WasPressedThisFrame() && _keyState == KeyState.PressHold && _inHoldState == false)
                {
                    _inHoldState = true;

                    switch (_zoneType)
                    {
                        case ZoneType.HoldAction:
                            PerformHoldAction();
                            break;
                    }
                }

                // if (Input.GetKeyUp(_zoneKeyInput) && _keyState == KeyState.PressHold)
                if (_input.Player.InteractHold.WasReleasedThisFrame() && _keyState == KeyState.PressHold)
                {
                    _inHoldState = false;
                    onHoldEnded?.Invoke(_zoneID);
                }
            }
        }

        private bool WasInteractPressedThisFrame()
        {
            // Space detonator zone uses Detonate; all other press zones use Interact (E).
            if (_zoneKeyInput == KeyCode.Space)
                return _input.Player.Detonate.WasPressedThisFrame();

            return _input.Player.Interact.WasPressedThisFrame();
        }

        private void CollectItems()
        {
            foreach (var item in _zoneItems)
            {
                item.SetActive(false);
            }

            UIManager.Instance.UpdateInventoryDisplay(_inventoryIcon);

            CompleteTask(_zoneID);

            onZoneInteractionComplete?.Invoke(this);

        }

        private void PerformAction()
        {
            foreach (var item in _zoneItems)
            {
                item.SetActive(true);
            }

            if (_inventoryIcon != null)
                UIManager.Instance.UpdateInventoryDisplay(_inventoryIcon);

            onZoneInteractionComplete?.Invoke(this);
        }

        private void PerformHoldAction()
        {
            UIManager.Instance.DisplayInteractableZoneMessage(false);
            onHoldStarted?.Invoke(_zoneID);
        }

        public GameObject[] GetItems()
        {
            return _zoneItems;
        }

        public int GetZoneID()
        {
            return _zoneID;
        }

        public bool PlayerInZone
        {
            get { return _inZone; }
        }

        public void CompleteTask(int zoneID)
        {
            if (zoneID == _zoneID)
            {
                _currentZoneID++;
                onZoneInteractionComplete?.Invoke(this);
            }
        }

        public void ResetAction(int zoneID)
        {
            if (zoneID == _zoneID)
                _actionPerformed = false;
        }

        public void SetMarker(InteractableZone zone)
        {
            if (_zoneID == _currentZoneID)
                _marker.SetActive(true);
            else
                _marker.SetActive(false);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _inZone = false;
                _inHoldState = false;
                UIManager.Instance.DisplayInteractableZoneMessage(false);
            }
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= SetMarker;
            _input.Player.Disable();
        }

    }
}


