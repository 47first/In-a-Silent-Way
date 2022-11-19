using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoundManager : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip drop;
    [SerializeField] private AudioClip playerFixed;
    [SerializeField] private List<AudioClip> footSteps;

    #region Wind
    [SerializeField] private AudioSource windSource;
    [SerializeField] private AnimationCurve windSoundCurve;
    private float windVolume;
    private float curWindCurveTime;
    private float maxWindCurveTime;
    #endregion

    #region Player Landed
    private float lastTimeLanded;
    private float landedTimeCooldown;
    #endregion

    #region Moving
    private float lastTimePlayFootStep;
    public float footStepPlayInterval;
    #endregion

    private void Start()
    {
        lastTimePlayFootStep = 0;
        landedTimeCooldown = 0.5f;
        playerController.OnFixedPosition += OnFixedPosition;
        playerController.OnPlayerLanded += OnPlayerLanded;

        windSource.mute = true;
        curWindCurveTime = 0;
        maxWindCurveTime = windSoundCurve.keys[windSoundCurve.keys.Length - 1].time;
    }

    private void Update()
    {
        lastTimeLanded += Time.deltaTime;
        OnPlayerMoving();
        OnPlayerFalling();
    }

    private void OnPlayerFalling() 
    {
        if (!playerController.IsFixedPos && !playerController.IsOnGround && playerController.Gravity < -10)
        {
            windSource.mute = false;

            if (curWindCurveTime < maxWindCurveTime)
                curWindCurveTime += Time.deltaTime;

            if (curWindCurveTime > maxWindCurveTime)
                curWindCurveTime = maxWindCurveTime;

            windVolume = windSoundCurve.Evaluate(curWindCurveTime);

            windSource.volume = windVolume * GameManager.InputSettings.globalAudioVolume;
        }
        else
        {
            windSource.mute = true;
            windVolume = 0;
            curWindCurveTime = 0;
        }
    }

    private void OnPlayerMoving() 
    {
        float positionDistance = Vector3.Distance(playerController.LastFixedUpdatedPosition, playerController.transform.position);

        if (playerController.IsPlayerMoving && playerController.IsOnGround && (positionDistance > 0.1f))
        {
            lastTimePlayFootStep += Time.deltaTime;
            float footStepInterval = 8 / playerController.Speed * footStepPlayInterval;
            if (lastTimePlayFootStep > footStepInterval)
            {
                lastTimePlayFootStep = 0;
                int randomIndex = Random.Range(0, footSteps.Count);
                source.PlayOneShot(footSteps[randomIndex], GameManager.InputSettings.globalAudioVolume * GameManager.InputSettings.playerAudioVolume);
            }
        }
    }

    private void OnFixedPosition() 
    {
        source.PlayOneShot(playerFixed, GameManager.InputSettings.globalAudioVolume * GameManager.InputSettings.playerAudioVolume);
    }

    private void OnPlayerLanded(float gravity) 
    {
        if (lastTimeLanded > landedTimeCooldown)
        {
            lastTimeLanded = 0;
            float volume = Mathf.Clamp(gravity / -15, 0, 2);
            source.PlayOneShot(drop, volume * GameManager.InputSettings.globalAudioVolume * GameManager.InputSettings.playerAudioVolume);
        }
    }
}