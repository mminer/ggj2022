using UnityEngine;

class AudioService : Services.Service
{
    AudioSource playerAudioSource;

    [SerializeField] private float footstepVolume = 0.1f;
    [SerializeField] private AudioClip[] footsteps;
    private int footstepIndex;

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
}
