using UnityEngine;
using Cinemachine;
using System;

public class ScreenShakeManager : MonoBehaviour
{
    public static ScreenShakeManager singleton;

    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private ScreenShake[] screenShakes;

    private ScreenShake currentScreenShake;
    private float shakeElapsedTime = 0f;

    private CinemachineBasicMultiChannelPerlin virtualCameraNoise;

    private void Awake()
    {
        singleton = this;

        if (virtualCamera != null)
            virtualCameraNoise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    private void Update()
    {
        HandleScreenShake();
    }

    private void HandleScreenShake()
    {
        if (virtualCameraNoise == null)
            return;

        if (shakeElapsedTime > 0)
        {
            shakeElapsedTime -= Time.deltaTime;
            return;
        }

        virtualCameraNoise.m_AmplitudeGain = 0f;
        shakeElapsedTime = 0f;
    }

    public void PlayCameraShake(string name)
    {
        currentScreenShake = GetScreenShake(name);
        shakeElapsedTime = currentScreenShake.shakeDuration;
        virtualCameraNoise.m_AmplitudeGain = currentScreenShake.shakeAmplitude;
        virtualCameraNoise.m_FrequencyGain = currentScreenShake.shakeFrequency;
    }

    private ScreenShake GetScreenShake(string name)
    {
        ScreenShake screenShake = Array.Find(screenShakes, s => s.name == name);

        if (screenShake == null)
            throw new Exception("Couldn't find a sound with the name: " + screenShake);

        return screenShake;
    }
}