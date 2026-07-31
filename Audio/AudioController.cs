using MediaBridge.Events;
using MediaBridge.IO;
using NAudio.CoreAudioApi;

namespace MediaBridge.Audio;

public sealed class AudioController
{
    private readonly MMDevice device;
    
    private readonly JsonLineWriter output;

    private const float VolumeStep = 0.05f;

    public AudioController(JsonLineWriter output)
    {
        this.output = output;
        var enumerator = new MMDeviceEnumerator();
        device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public Task<bool> MuteAsync()
    {
        device.AudioEndpointVolume.Mute = true;
        PublishState();
        return Task.FromResult(true);
    }

    public Task<bool> UnmuteAsync()
    {
        device.AudioEndpointVolume.Mute = false;
        PublishState();
        return Task.FromResult(true);
    }

    public Task<bool> ToggleMuteAsync()
    {
        device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
        PublishState();
        return Task.FromResult(true);
    }

    public Task<bool> VolumeUpAsync()
    {
        var current = device.AudioEndpointVolume.MasterVolumeLevelScalar;
        device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(
            current + VolumeStep,
            0f,
            1f
        );

        PublishState();
        return Task.FromResult(true);
    }

    public Task<bool> VolumeDownAsync()
    {
        var current = device.AudioEndpointVolume.MasterVolumeLevelScalar;
        device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(
            current - VolumeStep,
            0f,
            1f
        );

        PublishState();
        return Task.FromResult(true);
    }

    public Task<bool> SetVolumeAsync(float volume)
    {
        volume = Math.Clamp(volume, 0f, 1f);
        device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
        PublishState();
        return Task.FromResult(true);
    }

    private void PublishState()
    {
        output.Write(
            new VolumeChangedEvent
            {
                Volume = device.AudioEndpointVolume.MasterVolumeLevelScalar,
                Muted = device.AudioEndpointVolume.Mute,
            }
        );
    }
}
