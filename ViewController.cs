using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewController : MonoBehaviour
{
    private Vector3 targetRotation;
    private float fixedRotation;
    public Transform model;
    [SerializeField]private PlayerController playerController;
    [SerializeField]private CubeInHead cubeInHead;
    public CubeInHead CubeInHead { get { return cubeInHead; } }

    private void Start()
    {
        targetRotation = this.transform.rotation.eulerAngles;
        playerController.OnFixedPosition += () => fixedRotation = targetRotation.y;
    }

    private void Update()
    {

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (playerController.IsFixedPos)
            targetRotation.y = Mathf.Clamp(GameManager.InputSettings.horMouseSence * mouseX + targetRotation.y, -45 + fixedRotation, 45 + fixedRotation);
        else targetRotation.y = GameManager.InputSettings.horMouseSence * mouseX + targetRotation.y;

        targetRotation.x = Mathf.Clamp(GameManager.InputSettings.verMouseSence * -mouseY + targetRotation.x, -45, 45);

        model.eulerAngles = targetRotation.y * Vector3.up;
        this.transform.localEulerAngles = targetRotation.x * Vector3.right;
    }
}