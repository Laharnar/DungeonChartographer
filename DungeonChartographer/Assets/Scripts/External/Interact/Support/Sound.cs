using System.Collections;
using UnityEngine;

[System.Serializable]
public class SoundPlay
{
    public string tag;
    public string name;
    public AudioClip clip;

}

public static class SoundHelper
{
    public static SoundPlay GetBy(this IEnumerable sounds, string tag, SoundPath path)
    {
        foreach (SoundPlay item in sounds)
        {
            switch (path)
            {
                case SoundPath.NAME:
                    if (item.name == tag)
                        return item;
                    break;
                case SoundPath.TAG:
                    if (item.tag == tag)
                        return item;
                    break;
                default:
                    break;
            }
        }
        Debug.Log($"No clip {tag}");
        return null;
    }
}

public enum SoundPath
{
    NAME,
    TAG
}

public class Sound : MonoBehaviour
{

    public AudioSource audioSource;
    [SerializeField] SoundPlay[] sounds;
    [SerializeField, Header("Optional")] SoundsLib lib;
    public string playTagOnStart;
    public string playTagOnDelay;
    public float delay = 1;

    private IEnumerator Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        PlaySoundTag(playTagOnStart);
        yield return new WaitForSeconds(delay);
        PlaySoundTag(playTagOnDelay);
    }

    public void PlaySoundName(string audioName)
    {
        if (string.IsNullOrEmpty(audioName))
            return;

        var clip = sounds.GetBy(audioName, SoundPath.NAME);
        if (clip == null)
            Debug.LogWarning($"No clip found under tag {audioName}");
        else audioSource.clip = clip.clip;
        if (lib != null && audioSource.clip == null)
        {
            audioSource.clip = lib.Sounds.GetBy(audioName, SoundPath.NAME).clip;
        }
        audioSource.Play();
    }

    public void PlaySoundTag(string audioTag)
    {
        if (string.IsNullOrEmpty(audioTag))
            return;
        var clip = sounds.GetBy(audioTag, SoundPath.TAG);
        if (clip == null)
            Debug.LogWarning($"No clip found under tag {audioTag}");
        else audioSource.clip = clip.clip;
        if (lib != null && audioSource.clip == null)
        {
            audioSource.clip = lib.Sounds.GetBy(audioTag, SoundPath.TAG).clip;
        }
        audioSource.Play();
    }
}

