using UnityEngine;

/// <summary>Plays sound effects and the music loop. Created automatically the first time a sound plays.</summary>
public class AudioManager : MonoBehaviour
{
    static AudioManager inst;
    AudioSource[] sfx;
    AudioSource music;
    int next;

    static void Ensure()
    {
        if (inst != null) return;
        var go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<AudioManager>();
        inst.sfx = new AudioSource[10];
        for (int i = 0; i < inst.sfx.Length; i++)
        {
            inst.sfx[i] = go.AddComponent<AudioSource>();
            inst.sfx[i].playOnAwake = false;
        }
        inst.music = go.AddComponent<AudioSource>();
        inst.music.loop = true;
        inst.music.playOnAwake = false;
        inst.music.volume = PlayerPrefs.GetFloat("CB_MusicVolume", 0.8f) * 0.6f;
    }

    public static void Play(AudioClip clip, float volume = 1f, float pitchVariation = 0.08f)
    {
        if (clip == null) return;
        Ensure();
        var s = inst.sfx[inst.next];
        inst.next = (inst.next + 1) % inst.sfx.Length;
        s.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        s.PlayOneShot(clip, Mathf.Clamp01(volume) * MainMenuController.SfxVolume);
    }

    public static void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        Ensure();
        if (inst.music.clip == clip && inst.music.isPlaying) return;
        inst.music.clip = clip;
        inst.music.volume = PlayerPrefs.GetFloat("CB_MusicVolume", 0.8f) * 0.6f;
        inst.music.Play();
    }

    public static void SetMusicVolume(float v)
    {
        if (inst != null) inst.music.volume = v * 0.6f;
    }
}
