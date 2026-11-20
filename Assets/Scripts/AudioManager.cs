using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Self-contained procedural Horror AudioManager (with optional custom AudioClip Inspector slots).
    /// Provides:
    /// - Continuous eerie dark mansion wind + horror minor drone ambient music loop
    /// - Campfire crackle ambience & dramatic Bandit ambush hit stinger for Act 1 cutscene
    /// - Footstep taps, wooden drawer open/close creaks, item/oil pickup chimes,
    ///   stone rune clicks, puzzle unlock/error, door swing, and tense low-oil heartbeat (< 25%).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Optional Custom AudioClips (Leave empty for built-in procedural synthesis)")]
        public AudioClip customAmbientMusic;
        public AudioClip customCampfireLoop;
        public AudioClip customAmbushStinger;
        public AudioClip customFootstep;
        public AudioClip customDrawerOpen;
        public AudioClip customDrawerClose;
        public AudioClip customItemPickup;
        public AudioClip customOilPickup;
        public AudioClip customPuzzleClick;
        public AudioClip customPuzzleUnlock;
        public AudioClip customPuzzleError;
        public AudioClip customDoorSwing;
        public AudioClip customDoorLocked;
        public AudioClip customNoteOpen;
        public AudioClip customHeartbeat;
        public AudioClip customLampIgnite;
        public AudioClip customGameOver;
        public AudioClip customVictory;
        public AudioClip customRatSqueak;

        [Header("Volumes")]
        [Range(0f, 1f)] public float ambientVolume = 0.30f;
        [Range(0f, 1f)] public float campfireVolume = 0.32f;
        [Range(0f, 1f)] public float sfxVolume = 0.74f;
        [Range(0f, 1f)] public float heartbeatMaxVolume = 0.68f;

        private AudioSource ambientSource;
        private AudioSource campfireSource;
        private AudioSource sfxSource;
        private AudioSource heartbeatSource;

        private AudioClip procAmbient;
        private AudioClip procCampfire;
        private AudioClip procAmbushStinger;
        private AudioClip procFootstep;
        private AudioClip procDrawerOpen;
        private AudioClip procDrawerClose;
        private AudioClip procItemPickup;
        private AudioClip procOilPickup;
        private AudioClip procPuzzleClick;
        private AudioClip procPuzzleUnlock;
        private AudioClip procPuzzleError;
        private AudioClip procDoorSwing;
        private AudioClip procDoorLocked;
        private AudioClip procNoteOpen;
        private AudioClip procHeartbeat;
        private AudioClip procLampIgnite;
        private AudioClip procGameOver;
        private AudioClip procVictory;
        private AudioClip procRatSqueak;

        private bool heartbeatActive;
        private float heartbeatTimer;
        private float heartbeatIntensity;

        private const int SampleRate = 22050;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupSources();
            GenerateProceduralClips();
            StartAmbientDrone();
        }

        private void Update()
        {
            if (!heartbeatActive) return;

            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                AudioClip hb = customHeartbeat != null ? customHeartbeat : procHeartbeat;
                if (heartbeatSource != null && hb != null)
                {
                    heartbeatSource.PlayOneShot(hb, Mathf.Lerp(0.28f, heartbeatMaxVolume, heartbeatIntensity));
                }
                heartbeatTimer = Mathf.Lerp(1.02f, 0.50f, heartbeatIntensity);
            }
        }

        private void SetupSources()
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.spatialBlend = 0f;
            ambientSource.ignoreListenerPause = true;
            ambientSource.volume = ambientVolume;

            campfireSource = gameObject.AddComponent<AudioSource>();
            campfireSource.loop = true;
            campfireSource.playOnAwake = false;
            campfireSource.spatialBlend = 0f;
            campfireSource.volume = campfireVolume;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = sfxVolume;

            heartbeatSource = gameObject.AddComponent<AudioSource>();
            heartbeatSource.loop = false;
            heartbeatSource.playOnAwake = false;
            heartbeatSource.spatialBlend = 0f;
            heartbeatSource.volume = 1f;
        }

        private void GenerateProceduralClips()
        {
            procAmbient = CreateMansionWindDrone(8.0f);
            procCampfire = CreateCampfireCrackleLoop(4.0f);
            procAmbushStinger = CreateAmbushHitStinger(1.1f);
            procFootstep = CreateFootstepClip(0.11f);
            procDrawerOpen = CreateDrawerClip("DrawerOpen", 0.36f, 135f, 195f);
            procDrawerClose = CreateDrawerClip("DrawerClose", 0.30f, 185f, 115f);
            procItemPickup = CreateChordClip("ItemPickup", 0.34f, new[] { 523.25f, 659.25f, 783.99f }, 0.34f);
            procOilPickup = CreateSweepClip("OilPickup", 0.30f, 380f, 640f, 0.34f, false);
            procPuzzleClick = CreateClickClip("PuzzleClick", 0.08f, 640f, 0.34f);
            procPuzzleUnlock = CreateChordClip("PuzzleUnlock", 0.65f, new[] { 293.66f, 440f, 554.37f, 880f }, 0.38f);
            procPuzzleError = CreateSweepClip("PuzzleError", 0.36f, 185f, 88f, 0.42f, true);
            procDoorSwing = CreateDoorSwingClip(0.68f);
            procDoorLocked = CreateSweepClip("DoorLocked", 0.18f, 145f, 92f, 0.34f, true);
            procNoteOpen = CreatePaperRustleClip(0.22f);
            procHeartbeat = CreateHeartbeatClip(0.45f);
            procLampIgnite = CreateSweepClip("LampIgnite", 0.48f, 220f, 520f, 0.32f, false);
            procGameOver = CreateSweepClip("GameOver", 0.90f, 210f, 56f, 0.44f, true);
            procVictory = CreateChordClip("Victory", 1.15f, new[] { 392f, 493.88f, 587.33f, 783.99f, 987.77f }, 0.36f);
            procRatSqueak = CreateRatSqueakClip(0.18f);
        }

        public void StartAmbientDrone()
        {
            if (ambientSource == null) return;
            AudioClip clip = customAmbientMusic != null ? customAmbientMusic : procAmbient;
            if (clip == null) return;

            ambientSource.clip = clip;
            ambientSource.volume = ambientVolume;
            if (!ambientSource.isPlaying) ambientSource.Play();
        }

        public void SetCampfireAmbience(bool active)
        {
            if (campfireSource == null) return;
            if (active)
            {
                AudioClip clip = customCampfireLoop != null ? customCampfireLoop : procCampfire;
                if (clip != null)
                {
                    campfireSource.clip = clip;
                    campfireSource.volume = campfireVolume;
                    if (!campfireSource.isPlaying) campfireSource.Play();
                }
            }
            else if (campfireSource.isPlaying)
            {
                campfireSource.Stop();
            }
        }

        public void SetLowOilHeartbeat(bool active, float oilRatio0To25)
        {
            heartbeatActive = active;
            heartbeatIntensity = Mathf.Clamp01(1f - (oilRatio0To25 / 0.25f));
            if (!active) heartbeatTimer = 0.05f;
        }

        public void PlayFootstep()
        {
            AudioClip clip = customFootstep != null ? customFootstep : procFootstep;
            if (sfxSource != null && clip != null)
            {
                sfxSource.pitch = Random.Range(0.88f, 1.12f);
                sfxSource.PlayOneShot(clip, 0.38f);
                sfxSource.pitch = 1f;
            }
        }

        public void PlayHeavyFootstep(float volumeScale = 0.85f)
        {
            AudioClip clip = customFootstep != null ? customFootstep : procFootstep;
            if (sfxSource != null && clip != null)
            {
                sfxSource.pitch = Random.Range(0.52f, 0.64f);
                sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
                sfxSource.pitch = 1f;
            }
        }

        public void PlayAmbushStinger() => PlayOneShot(customAmbushStinger, procAmbushStinger, 0.92f);
        public void PlayDrawerOpen() => PlayOneShot(customDrawerOpen, procDrawerOpen, 0.72f);
        public void PlayDrawerClose() => PlayOneShot(customDrawerClose, procDrawerClose, 0.68f);
        public void PlayItemPickup() => PlayOneShot(customItemPickup, procItemPickup, 0.78f);
        public void PlayOilPickup() => PlayOneShot(customOilPickup, procOilPickup, 0.76f);
        public void PlayPuzzleClick() => PlayOneShot(customPuzzleClick, procPuzzleClick, 0.65f);
        public void PlayPuzzleUnlock() => PlayOneShot(customPuzzleUnlock, procPuzzleUnlock, 0.85f);
        public void PlayPuzzleError() => PlayOneShot(customPuzzleError, procPuzzleError, 0.80f);
        public void PlayDoorSwing() => PlayOneShot(customDoorSwing, procDoorSwing, 0.82f);
        public void PlayDoorLocked() => PlayOneShot(customDoorLocked, procDoorLocked, 0.60f);
        public void PlayNoteOpen() => PlayOneShot(customNoteOpen, procNoteOpen, 0.65f);
        public void PlayLampIgnite() => PlayOneShot(customLampIgnite, procLampIgnite, 0.74f);
        public void PlayGameOver() => PlayOneShot(customGameOver, procGameOver, 0.84f);
        public void PlayVictory() => PlayOneShot(customVictory, procVictory, 0.88f);

        public void PlayRatSqueak()
        {
            AudioClip clip = customRatSqueak != null ? customRatSqueak : procRatSqueak;
            if (sfxSource != null && clip != null)
            {
                sfxSource.pitch = Random.Range(0.92f, 1.18f);
                sfxSource.PlayOneShot(clip, 0.52f);
                sfxSource.pitch = 1f;
            }
        }

        private void PlayOneShot(AudioClip customClip, AudioClip fallbackClip, float volume)
        {
            AudioClip clip = customClip != null ? customClip : fallbackClip;
            if (sfxSource != null && clip != null)
            {
                sfxSource.pitch = 1f;
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        // =========================================================================
        // PROCEDURAL HORROR SYNTHESIS GENERATORS
        // =========================================================================

        private static AudioClip CreateMansionWindDrone(float duration)
        {
            int count = Mathf.FloorToInt(SampleRate * duration);
            float[] data = new float[count];
            System.Random rng = new System.Random(77);
            float windLp1 = 0f;
            float windLp2 = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float lfo1 = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 0.125f * t);
                float lfo2 = 0.50f + 0.50f * Mathf.Sin(2f * Mathf.PI * 0.25f * t + 1.1f);
                float windMod = 0.35f + 0.65f * Mathf.Sin(2f * Mathf.PI * 0.125f * t + 2.4f);

                // Low eerie gothic minor chord + subtle hollow mansion wind
                float drone =
                    Mathf.Sin(2f * Mathf.PI * 55.0f * t) * 0.38f +
                    Mathf.Sin(2f * Mathf.PI * 110.0f * t) * 0.18f * lfo1 +
                    Mathf.Sin(2f * Mathf.PI * 130.75f * t) * 0.14f * lfo2 +
                    Mathf.Sin(2f * Mathf.PI * 155.5f * t) * 0.08f * lfo1 +
                    Mathf.Sin(2f * Mathf.PI * 165.0f * t) * 0.10f * lfo2;

                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                windLp1 = windLp1 * 0.96f + white * 0.04f;
                windLp2 = windLp2 * 0.92f + windLp1 * 0.08f;
                float wind = windLp2 * 1.9f * windMod;

                data[i] = Mathf.Clamp((drone + wind) * 0.46f, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("MansionWindDrone", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateCampfireCrackleLoop(float duration)
        {
            int count = Mathf.FloorToInt(SampleRate * duration);
            float[] data = new float[count];
            System.Random rng = new System.Random(512);
            float lp = 0f;

            for (int i = 0; i < count; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp = lp * 0.88f + white * 0.12f;
                // Random tiny wood crackle pops
                float pop = rng.NextDouble() < 0.0022 ? (float)(rng.NextDouble() * 1.4 - 0.7) : 0f;
                data[i] = Mathf.Clamp((lp * 0.55f + pop * 0.65f) * 0.48f, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("CampfireLoop", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateAmbushHitStinger(float duration)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];
            System.Random rng = new System.Random(909);

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float timeSec = (float)i / SampleRate;
                float impactEnv = Mathf.Exp(-t * 4.5f);
                float subDrop = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(135f, 38f, t) * timeSec);
                float dissonant = Mathf.Sin(2f * Mathf.PI * 220f * timeSec) * 0.4f +
                                  Mathf.Sin(2f * Mathf.PI * 233.08f * timeSec) * 0.4f +
                                  Mathf.Sin(2f * Mathf.PI * 311.13f * timeSec) * 0.35f;
                float burst = t < 0.12f ? (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - t / 0.12f) : 0f;

                data[i] = Mathf.Clamp((subDrop * 0.65f + dissonant * 0.35f + burst * 0.55f) * impactEnv * 0.85f, -0.98f, 0.98f);
            }

            AudioClip clip = AudioClip.Create("AmbushStinger", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateFootstepClip(float duration)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];
            System.Random rng = new System.Random(42);
            float lp = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float env = Mathf.Sin(Mathf.Min(1f, t * 14f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - t, 2.2f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp = lp * 0.72f + noise * 0.28f;
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(130f, 55f, t) * (i / (float)SampleRate));
                data[i] = (lp * 0.55f + thud * 0.45f) * env * 0.65f;
            }

            AudioClip clip = AudioClip.Create("Footstep", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateDrawerClip(string clipName, float duration, float fStart, float fEnd)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];
            System.Random rng = new System.Random(107);
            float lp = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float timeSec = (float)i / SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp = lp * 0.80f + noise * 0.20f;

                float slideEnv = Mathf.Sin(t * Mathf.PI) * 0.75f;
                float woodTone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(fStart, fEnd, t) * timeSec) * 0.28f;
                float endKnock = t > 0.76f ? Mathf.Exp(-(t - 0.76f) * 32f) * Mathf.Sin(2f * Mathf.PI * 105f * timeSec) * 0.72f : 0f;

                data[i] = Mathf.Clamp((lp * 0.65f + woodTone) * slideEnv + endKnock, -1f, 1f) * 0.55f;
            }

            AudioClip clip = AudioClip.Create(clipName, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateDoorSwingClip(float duration)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float creakFreq = 180f + 90f * Mathf.Sin(t * Mathf.PI * 2.5f) - t * 60f;
                phase += (2f * Mathf.PI * creakFreq) / SampleRate;
                float env = Mathf.Sin(t * Mathf.PI);
                float creak = (Mathf.Sin(phase) * 0.5f + Mathf.Sin(phase * 2.03f) * 0.25f) * env;
                float rumble = Mathf.Sin(2f * Mathf.PI * 68f * (i / (float)SampleRate)) * env * 0.5f;
                data[i] = Mathf.Clamp((creak * 0.45f + rumble) * 0.55f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("DoorSwing", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreatePaperRustleClip(float duration)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];
            System.Random rng = new System.Random(303);
            float prev = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float env = Mathf.Sin(t * Mathf.PI) * (0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 5f));
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                float hp = white - prev * 0.65f;
                prev = white;
                data[i] = Mathf.Clamp(hp * env * 0.22f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("PaperRustle", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateHeartbeatClip(float duration)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float lub = t < 0.14f
                    ? Mathf.Exp(-t * 24f) * Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(72f, 42f, t / 0.14f) * t)
                    : 0f;
                float dubT = t - 0.16f;
                float dub = (dubT >= 0f && dubT < 0.18f)
                    ? Mathf.Exp(-dubT * 28f) * Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(62f, 38f, dubT / 0.18f) * dubT) * 0.78f
                    : 0f;
                data[i] = Mathf.Clamp((lub + dub) * 0.92f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Heartbeat", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateClickClip(string clipName, float duration, float freq, float amp)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float env = Mathf.Exp(-t * 14f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)SampleRate)) * env * amp;
            }
            AudioClip clip = AudioClip.Create(clipName, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSweepClip(string clipName, float duration, float startFreq, float endFreq, float amp, bool harsh)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += (2f * Mathf.PI * freq) / SampleRate;
                float env = Mathf.Sin(Mathf.Min(1f, t * 14f) * Mathf.PI * 0.5f) * (1f - t);
                float sample = Mathf.Sin(phase);
                if (harsh) sample = Mathf.Clamp(sample * 1.8f + Mathf.Sin(phase * 1.49f) * 0.45f, -1f, 1f);
                else sample = sample * 0.78f + Mathf.Sin(phase * 2f) * 0.22f;
                data[i] = sample * env * amp;
            }

            AudioClip clip = AudioClip.Create(clipName, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateChordClip(string clipName, float duration, float[] freqs, float amp)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];
            float inv = 1f / Mathf.Max(1, freqs.Length);

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float timeSec = (float)i / SampleRate;
                float env = Mathf.Sin(Mathf.Min(1f, t * 18f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - t, 1.3f);

                float sum = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * timeSec);
                }
                data[i] = sum * inv * env * amp;
            }

            AudioClip clip = AudioClip.Create(clipName, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateRatSqueakClip(float duration)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(SampleRate * duration));
            float[] data = new float[count];
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                // Double high-pitched rat squeak chirp
                float chirp1 = t < 0.44f ? Mathf.Sin((t / 0.44f) * Mathf.PI) : 0f;
                float chirp2 = t > 0.52f ? Mathf.Sin(((t - 0.52f) / 0.48f) * Mathf.PI) * 0.85f : 0f;
                float env = chirp1 + chirp2;

                float freq = t < 0.48f
                    ? Mathf.Lerp(2600f, 4300f, Mathf.Sin((t / 0.48f) * Mathf.PI))
                    : Mathf.Lerp(2900f, 4650f, Mathf.Sin(((t - 0.48f) / 0.52f) * Mathf.PI));

                phase += (2f * Mathf.PI * freq) / SampleRate;
                float sample = Mathf.Sin(phase) * 0.72f + Mathf.Sin(phase * 1.5f) * 0.28f;
                data[i] = Mathf.Clamp(sample * env * 0.42f, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("RatSqueak", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
