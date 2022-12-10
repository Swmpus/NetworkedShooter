using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerInputManager : MonoBehaviour
{
    [SerializeField]
    PlayerSpawner spawner;

    CharacterControls controls;
    CharacterControls.SpawnSelectionActions spawnControls;

    private void Awake()
    {
        controls = new CharacterControls();
        spawnControls = controls.SpawnSelection;
        spawnControls.Click.started += ctx => spawner.Click();
        spawnControls.MousePosition.performed += ctx => spawner.SetMousePos(ctx.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        spawnControls.Enable();
    }

    private void OnDisable()
    {
        spawnControls.Disable();
    }
}
