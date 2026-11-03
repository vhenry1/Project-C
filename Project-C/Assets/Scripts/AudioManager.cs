using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sound effects and music. Every sound is generated in code so the game works with no audio files.
/// To use real audio, drop clips into Assets/Resources/Audio/ with these names and they replace the generated ones:
/// attack, hit, hurt, down, plant, harvest, buy, deny, shoot, dawn, nightfall, click, music_day, music_night.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const int SampleRate = 22050;

    private static AudioManager instance;
    private AudioSource sfxSource;
    private AudioSource musicSource;
    private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private bool musicIsNight;
    private bool musicStarted;

    private void Awake()
    {
        instance = this;
        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.22f;
    }

    public static void Play(string id, float volume = 0.6f)
    {
        if (instance == null) return;
        AudioClip clip = instance.Get(id);
        if (clip != null) instance.sfxSource.PlayOneShot(clip, volume);
    }

    public static void Music(bool night)
    {
        if (instance == null) return;
        if (instance.musicStarted && instance.musicIsNight == night) return;
        instance.musicStarted = true;
        instance.musicIsNight = night;
        instance.musicSource.clip = instance.Get(night ? "music_night" : "music_day");
        instance.musicSource.Play();
    }

    private AudioClip Get(string id)
    {
        AudioClip clip;
        if (cache.TryGetValue(id, out clip)) return clip;

        clip = Resources.Load<AudioClip>("Audio/" + id);
        if (clip == null) clip = Synthesize(id);
        cache[id] = clip;
        return clip;
    }

    // ---- Generated sounds ----

    private static AudioClip Synthesize(string id)
    {
        switch (id)
        {
            case "attack":    return Make(id, Sweep(0.12f, 900f, 200f, 0.5f, true));
            case "hit":       return Make(id, Sweep(0.09f, 220f, 90f, 0.6f, false));
            case "hurt":      return Make(id, Sweep(0.2f, 300f, 110f, 0.6f, false));
            case "down":      return Make(id, Notes(0.18f, 0.6f, 220f, 185f, 147f));
            case "plant":     return Make(id, Notes(0.07f, 0.5f, 392f, 440f));
            case "harvest":   return Make(id, Notes(0.07f, 0.55f, 523f, 659f, 784f));
            case "buy":       return Make(id, Notes(0.07f, 0.55f, 784f, 1047f));
            case "deny":      return Make(id, Notes(0.12f, 0.5f, 140f, 120f));
            case "shoot":     return Make(id, Sweep(0.1f, 1000f, 300f, 0.4f, false));
            case "dawn":      return Make(id, Notes(0.12f, 0.5f, 523f, 659f, 784f, 1047f));
            case "nightfall": return Make(id, Notes(0.2f, 0.5f, 294f, 247f, 196f, 147f));
            case "click":     return Make(id, Notes(0.04f, 0.4f, 620f));
            case "music_day":
                return Make(id, Notes(0.26f, 0.30f,
                    523f, 659f, 784f, 659f, 880f, 784f, 659f, 587f,
                    523f, 659f, 784f, 988f, 880f, 784f, 659f, 587f));
            case "music_night":
                return Make(id, Notes(0.42f, 0.34f,
                    220f, 0f, 262f, 0f, 330f, 262f, 0f, 247f,
                    196f, 0f, 233f, 0f, 294f, 233f, 0f, 220f));
            default:          return Make(id, Notes(0.08f, 0.4f, 440f));
        }
    }

    private static AudioClip Make(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // A sequence of short decaying notes. A frequency of 0 is a rest.
    private static float[] Notes(float noteLength, float volume, params float[] freqs)
    {
        int per = Mathf.RoundToInt(noteLength * SampleRate);
        float[] data = new float[per * freqs.Length];
        for (int n = 0; n < freqs.Length; n++)
        {
            if (freqs[n] <= 0f) continue;
            for (int i = 0; i < per; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Pow(1f - (float)i / per, 1.4f);
                data[n * per + i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env * volume;
            }
        }
        return data;
    }

    // A sound that slides from one pitch to another (optionally noisy, for swishes).
    private static float[] Sweep(float length, float f0, float f1, float volume, bool noisy)
    {
        int total = Mathf.RoundToInt(length * SampleRate);
        float[] data = new float[total];
        float phase = 0f;
        System.Random rng = new System.Random(7);
        for (int i = 0; i < total; i++)
        {
            float k = (float)i / total;
            float freq = Mathf.Lerp(f0, f1, k);
            phase += 2f * Mathf.PI * freq / SampleRate;
            float tone = Mathf.Sin(phase);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float env = Mathf.Pow(1f - k, 1.5f);
            data[i] = (noisy ? noise * 0.7f + tone * 0.3f : tone) * env * volume;
        }
        return data;
    }
}
