using UnityEngine;
using FMODUnity;

public static class FMODUtilities
{
    public static bool IsEmitterStopping(StudioEventEmitter emitter)
    {
        if (emitter == null || !emitter.EventInstance.isValid())
        {
            return false;
        }
        emitter.EventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE playbackState);
        return playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPING;
    }
}
