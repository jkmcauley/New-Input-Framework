using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using Game.Scripts.UI;

namespace Game.Scripts.LiveObjects
{
    public class Crate : MonoBehaviour
    {
        [SerializeField] private float _punchDelay;
        [SerializeField] private GameObject _wholeCrate, _brokenCrate;
        [SerializeField] private Rigidbody[] _pieces;
        [SerializeField] private BoxCollider _crateCollider;
        [SerializeField] private InteractableZone _interactableZone;
        private bool _isReadyToBreak = false;
        private bool _canPunch = true;
        private bool _crateFinished = false;
        private InputSystem_Actions _input;
        private Collider _playerCollider;

        private List<Rigidbody> _brakeOff = new List<Rigidbody>();

        private void Awake()
        {
            _input = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            // InteractableZone.onZoneInteractionComplete += InteractableZone_onZoneInteractionComplete;
            _input.Player.Interacttaphold.performed += OnInteractTapHold;
            _input.Player.Enable();
        }

        private void Start()
        {
            _brakeOff.AddRange(_pieces);
        }

        // No Exit mode needed for crate - player never loses WASD control.
        // After the crate swaps to broken pieces, frozen piece colliders can trap
        // the CharacterController; we ignore those collisions instead.

        private void OnInteractTapHold(InputAction.CallbackContext context)
        {
            if (_crateFinished)
                return;

            if (_interactableZone == null || _interactableZone.PlayerInZone == false)
                return;

            if (_interactableZone.GetZoneID() != 6)
                return;

            if (_canPunch == false)
                return;

            float forceMultiplier = 0.25f;

            if (context.interaction is HoldInteraction)
                forceMultiplier = 1f;
            else if (context.interaction is TapInteraction)
                forceMultiplier = 0.25f;
            else
                return;

            forceMultiplier = Mathf.Clamp(forceMultiplier, 0f, 1f);

            if (_isReadyToBreak == false && _brakeOff.Count > 0)
            {
                _wholeCrate.SetActive(false);
                _brokenCrate.SetActive(true);
                _isReadyToBreak = true;
                IgnorePlayerCollisionWithPieces();
            }

            if (_isReadyToBreak && _brakeOff.Count > 0)
            {
                BreakPart(forceMultiplier);
                if (_brakeOff.Count == 0)
                {
                    FinishCrate();
                }
                else
                {
                    StartCoroutine(PunchDelay());
                }
            }
        }

        // Legacy: zone press always used the same force
        // private void InteractableZone_onZoneInteractionComplete(InteractableZone zone)
        // {
        //     if (_isReadyToBreak == false && _brakeOff.Count >0)
        //     {
        //         _wholeCrate.SetActive(false);
        //         _brokenCrate.SetActive(true);
        //         _isReadyToBreak = true;
        //     }
        //
        //     if (_isReadyToBreak && zone.GetZoneID() == 6) //Crate zone
        //     {
        //         if (_brakeOff.Count > 0)
        //         {
        //             BreakPart();
        //             StartCoroutine(PunchDelay());
        //         }
        //         else if(_brakeOff.Count == 0)
        //         {
        //             _isReadyToBreak = false;
        //             _crateCollider.enabled = false;
        //             _interactableZone.CompleteTask(6);
        //             Debug.Log("Completely Busted");
        //         }
        //     }
        // }

        public void BreakPart(float forceMultiplier)
        {
            int rng = Random.Range(0, _brakeOff.Count);
            _brakeOff[rng].constraints = RigidbodyConstraints.None;
            // _brakeOff[rng].AddForce(new Vector3(1f, 1f, 1f), ForceMode.Force);
            Vector3 force = new Vector3(2f, 2f, 2f) * forceMultiplier;
            _brakeOff[rng].AddForce(force, ForceMode.Impulse);
            _brakeOff.Remove(_brakeOff[rng]);
        }

        private void IgnorePlayerCollisionWithPieces()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            _playerCollider = player.GetComponent<Collider>();
            if (_playerCollider == null)
                return;

            foreach (var piece in _pieces)
            {
                if (piece == null)
                    continue;

                var pieceCols = piece.GetComponentsInChildren<Collider>();
                foreach (var col in pieceCols)
                {
                    if (col != null)
                        Physics.IgnoreCollision(_playerCollider, col, true);
                }
            }
        }

        private void FinishCrate()
        {
            _crateFinished = true;
            _isReadyToBreak = false;
            _crateCollider.enabled = false;
            _interactableZone.CompleteTask(6);
            UIManager.Instance.DisplayInteractableZoneMessage(false);

            _input.Player.Interacttaphold.performed -= OnInteractTapHold;

            Debug.Log("Completely Busted");
        }

        IEnumerator PunchDelay()
        {
            _canPunch = false;
            float delayTimer = 0;
            while (delayTimer < _punchDelay)
            {
                yield return new WaitForEndOfFrame();
                delayTimer += Time.deltaTime;
            }

            // _interactableZone.ResetAction(6);
            _canPunch = true;
        }

        private void OnDisable()
        {
            // InteractableZone.onZoneInteractionComplete -= InteractableZone_onZoneInteractionComplete;
            _input.Player.Interacttaphold.performed -= OnInteractTapHold;
            _input.Player.Disable();
        }
    }
}
