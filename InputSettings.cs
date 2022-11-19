using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InputSettings", menuName = "Scriptable Objects/Input Settings")]
public class InputSettings : ScriptableObject
{
    public float globalAudioVolume;
    public float playerAudioVolume;
    public float musicAudioVolume;

    public float verMouseSence;
    public float horMouseSence;

    public KeyCode playerMoveForward;
    public KeyCode playerMoveBack;
    public KeyCode playerMoveRight;
    public KeyCode playerMoveLeft;

    public KeyCode playerJump;

    public CrouchMode crouchMode;
    public KeyCode playerCrouch;

    public KeyCode playerRun;

    public enum CrouchMode { Click, Hold }
}