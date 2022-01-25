using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer
{
    private float duration;
    private Action onTimerEnd;
    private float durationLeft;

    public Timer(float duration, bool loop, Action onTimerEnd)
    {
        this.duration = duration;
        this.onTimerEnd = onTimerEnd;

        durationLeft = duration;

        if (loop)
            this.onTimerEnd += RestartTimer;
    }

    public void Tick()
    {
        durationLeft -= Time.deltaTime;

        if (IsOver())
            onTimerEnd?.Invoke();
    }

    public bool IsOver()
    {
        return durationLeft <= 0;
    }

    public void RestartTimer()
    {
        durationLeft = duration;
    }
}
