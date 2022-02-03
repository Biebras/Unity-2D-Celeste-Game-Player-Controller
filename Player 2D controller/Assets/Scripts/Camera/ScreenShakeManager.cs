using UnityEngine;
using Cinemachine;
using System;

public class ScreenShakeManager : MonoBehaviour
{
    public static ScreenShakeManager s_singleton;

    [SerializeField] private CinemachineVirtualCamera _virtualCamera;
    [SerializeField] private ScreenShake[] _screenShakes;

    private ScreenShake _currentScreenShake;
    private float _shakeElapsedTime = 0f;

    private CinemachineBasicMultiChannelPerlin _virtualCameraNoise;

    private void Awake()
    {
        s_singleton = this;

        if (_virtualCamera != null)
            _virtualCameraNoise = _virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    private void Update()
    {
        HandleScreenShake();
    }

    private void HandleScreenShake()
    {
        if (_virtualCameraNoise == null)
            return;

        if (_shakeElapsedTime > 0)
        {
            _shakeElapsedTime -= Time.deltaTime;
            return;
        }

        _virtualCameraNoise.m_AmplitudeGain = 0f;
        _shakeElapsedTime = 0f;
    }

    public void PlayCameraShake(string name)
    {
        _currentScreenShake = GetScreenShake(name);
        _shakeElapsedTime = _currentScreenShake.shakeDuration;
        _virtualCameraNoise.m_AmplitudeGain = _currentScreenShake.shakeAmplitude;
        _virtualCameraNoise.m_FrequencyGain = _currentScreenShake.shakeFrequency;
    }

    private ScreenShake GetScreenShake(string name)
    {
        ScreenShake screenShake = Array.Find(_screenShakes, s => s.name == name);

        if (screenShake == null)
            throw new Exception("Couldn't find a sound with the name: " + screenShake);

        return screenShake;
    }
}