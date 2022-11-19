using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static InputSettings InputSettings { get; private set; }
    [SerializeField] private InputSettings _inputInstance;

    #region Player

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 startPosition;

    private GameObject _playerInstance;
    public GameObject PlayerInstance { get { return _playerInstance; } 
        set { _playerInstance = value; } 
    }

    private PlayerPack _playerPack;
    public PlayerPack PlayerPack { get { return _playerPack; }
        set { _playerPack = value; }
    }

    #endregion

    private void Start()
    {
        SpawnPlayer();
        SetCursorVisible(false);
        RestartArea.OnPlayerEnterRestartArea += RespawnPlayer;

        InputSettings = _inputInstance;
    }

    private void SetCursorVisible(bool isVisible) 
    {
        if (isVisible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void RespawnPlayer() 
    {
        PlayerPack.PlayerController.transform.position = startPosition;
        PlayerPack.ViewController.CubeInHead.transform.position = new Vector3(startPosition.x, startPosition.y + 1.5f, startPosition.z);
        PlayerPack.ViewController.CubeInHead.RigidBody.velocity = Vector3.zero;
    }

    private void SpawnPlayer() 
    {
        PlayerInstance = GameObject.Instantiate(playerPrefab, startPosition, Quaternion.identity);
        PlayerInstance.name = "Player Pack";
        PlayerPack = PlayerInstance.GetComponent<PlayerPack>();
    }
}