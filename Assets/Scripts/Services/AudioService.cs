using System;
using UnityEngine;

public class AudioService : Services.Service
{
    AudioSource playerAudioSource;


    [Header("== Footsteps ==")]
    [SerializeField] private float footstepVolume = 0.1f;
    [SerializeField] private AudioClip[] footsteps;
    private int footstepIndex;

    [Header("== Traps ==")]
    [SerializeField] private AudioClip pitFall;

    [Header("== UI Buttons ==")]
    [SerializeField] private AudioClip cycleGlyph;

    void Awake()
    {
        playerAudioSource = GetComponent<AudioSource>();
    }

    public bool ToggleMute()
    {
        var muted = AudioListener.volume == 0;
        AudioListener.volume = muted ? 1f : 0f;
        return !muted;
    }

    public void PlaySoundEffect(AudioClip clip, float volume = 1)
    {
        playerAudioSource.PlayOneShot(clip, volume);
    }

    public void PlayFootstep()
    {
        PlaySoundEffect(footsteps[footstepIndex], footstepVolume);
        footstepIndex = footstepIndex + 1 >= footsteps.Length ? 0 : footstepIndex + 1;
    }

    public void PlayTrap(ItemType trap)
    {
        var trapAudioClip = trap switch
        {
            _ when trap == ItemType.Pit => pitFall,
            _ => throw new ArgumentOutOfRangeException(),
        };

        PlaySoundEffect(trapAudioClip);
    }

    public void PlayCycleGlyph()
    {
        PlaySoundEffect(cycleGlyph, 0.1f);
    }
}
