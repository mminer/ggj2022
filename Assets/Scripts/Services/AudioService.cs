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

    void Awake()
    {
        playerAudioSource = GetComponent<AudioSource>();
    }

    public void PlaySoundEffect(AudioClip clip, float volume = 1)
    {
        playerAudioSource.PlayOneShot(clip, volume);
    }

    public void PlayFootstep()
    {
        playerAudioSource.clip = footsteps[footstepIndex];
        playerAudioSource.volume = footstepVolume;
        playerAudioSource.Play();
        footstepIndex = footstepIndex + 1 >= footsteps.Length ? 0 : footstepIndex + 1;
    }

    public void PlayTrap(ItemType trap)
    {
        playerAudioSource.volume = 1;

        switch (trap)
        {
            case ItemType.Pit:
                playerAudioSource.clip = pitFall;
                break;
            default:
                Debug.LogError($"Unimplemented trap audio: {trap}");
                throw new ArgumentOutOfRangeException();
        };

        playerAudioSource.Play();
    }
}
