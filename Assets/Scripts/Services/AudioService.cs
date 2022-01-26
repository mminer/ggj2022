using UnityEngine;

class AudioService : Services.Service
{
    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySoundEffect(AudioClip clip, float volume = 1)
    {
        audioSource.PlayOneShot(clip, volume);
    }
}
