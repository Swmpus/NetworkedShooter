using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputManager : MonoBehaviour
{
    [SerializeField]
    CharacterMovement movement;
    [SerializeField]
    MouseLook mouseAim;

    CharacterControls controls;
    CharacterControls.GroundMovementActions groundMovement;
    Vector2 movementInput;
    Vector2 aimInput;

    private void Awake()
    {
        controls = new CharacterControls();
        groundMovement = controls.GroundMovement;

        groundMovement.Move.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
        groundMovement.MouseX.performed += ctx => aimInput.x = ctx.ReadValue<float>();
        groundMovement.MouseY.performed += ctx => aimInput.y = ctx.ReadValue<float>();
        groundMovement.Fire.started += ctx => mouseAim.fire();
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        groundMovement.Enable();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        groundMovement.Disable();
    }

    private void OnDestroy()
    {
        //controls.Disable(); // TODO is this necessary?
    }

    private void Update()
    {
        movement.ReceiveInput(movementInput);
        mouseAim.ReceiveInput(aimInput);
    }
}
