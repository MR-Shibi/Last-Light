using System;
using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Antique Oil Lantern Controller:
    /// - Linear 4-minute full drain (drainRate = 100f / 240f ≈ 0.4167%/sec), strict 0..100% cap.
    /// - Warm Mode (default): warm amber light, linear PointLight.range from 6.5f (100%) down to 1.5f (0%).
    /// - Blue Spirit Flame Mode: cool cyan/blue light, tighter 4.0f radius, 1.5x oil drain,
    ///   reveals hidden cyan clues/arrows toward puzzle containers & faint red marks on cursed/empty drawers.
    /// - Lamp ON/OFF Toggle: extinguishes the lantern to hide from Bandit Patrols and pauses oil drain while OFF.
    /// </summary>
    public class LampController : MonoBehaviour
    {
        [Header("Light, UI & Lantern References")]
        public Light lampLight;
        public UIManager uiManager;
        public LanternProp firstPersonLantern;
        public LanternProp thirdPersonMaleLantern;
        public LanternProp thirdPersonFemaleLantern;

        [Header("Oil Economy (Linear 4-Minute Drain, Strict 0..100% Cap)")]
        public float maxOil = 100f;
        public float startingOil = 75f;
        public float drainRate = 100f / 240f; // 100% -> 0% in exactly 240 seconds (4 minutes)
        public float spiritDrainMultiplier = 1.5f;
        public float oilPickupRestore = 20f;

        [Header("Dynamic Light Radius & Flame Modes")]
        public float maxLightRange = 6.5f;
        public float spiritMaxLightRange = 4.0f;
        public float minLightRange = 1.5f;
        public float maxLightIntensity = 18.5f;
        public float minLightIntensity = 3.0f;
        public float lightSmoothSpeed = 8.0f;
        public Color lampWarmColor = new Color(1f, 0.76f, 0.32f, 1f);
        public Color lampSpiritColor = new Color(0.20f, 0.80f, 1.0f, 1f);

        [Header("Live Runtime State")]
        public bool isGameActive;
        public bool isPaused;
        public bool isSpiritFlame;
        public bool isExtinguished;

        private float currentOil = 75f;
        private bool hasDepleted;
        private bool manualCutsceneLight;
        private float flareBonus;
        private float flickerSeed;

        public event Action OnOilDepleted;
        public event Action<float, float> OnOilChanged;
        public event Action<bool> OnSpiritVisionChanged;
        public event Action<bool> OnLampExtinguishedChanged;

        public float CurrentOil => currentOil;
        public float MaxOil => maxOil;
        public float OilNormalized => Mathf.Clamp01(currentOil / 100f);
        public bool IsDraining => isGameActive && !isPaused && !hasDepleted && !isExtinguished;
        public bool IsSpiritFlame => isSpiritFlame;
        public bool IsExtinguished => isExtinguished;
        public bool IsSpiritVisionActive => isGameActive && isSpiritFlame && !isExtinguished && !hasDepleted && currentOil > 0f;
        public float CurrentLightRange => (lampLight != null && lampLight.enabled && !isExtinguished) ? lampLight.range : 0f;
        public Vector3 LightWorldPosition => lampLight != null ? lampLight.transform.position : transform.position + Vector3.up * 1.25f;

        private void Awake()
        {
            flickerSeed = UnityEngine.Random.Range(0f, 100f);
            maxOil = 100f;
            currentOil = Mathf.Clamp(startingOil, 0f, 100f);

            if (lampLight != null)
            {
                lampLight.type = LightType.Point;
                lampLight.color = lampWarmColor;
            }
        }

        private void Update()
        {
            if (manualCutsceneLight)
            {
                AudioManager.Instance?.SetLowOilHeartbeat(false, 1f);
                return;
            }

            if (flareBonus > 0f)
            {
                flareBonus = Mathf.MoveTowards(flareBonus, 0f, Time.deltaTime * 3.5f);
            }

            // Linear drain while game active, unpaused, and lamp is ON (extinguishing lamp pauses drain!)
            if (isGameActive && !isPaused && !hasDepleted && !isExtinguished)
            {
                float activeRate = isSpiritFlame ? drainRate * spiritDrainMultiplier : drainRate;
                currentOil = Mathf.Clamp(currentOil - activeRate * Time.deltaTime, 0f, 100f);
                NotifyOilUI();

                float ratio = OilNormalized;
                AudioManager.Instance?.SetLowOilHeartbeat(ratio < 0.25f && currentOil > 0f, ratio);

                if (currentOil <= 0f)
                {
                    TriggerDepletion();
                    return;
                }
            }
            else
            {
                AudioManager.Instance?.SetLowOilHeartbeat(false, 1f);
            }

            UpdateLightSmooth(Time.deltaTime);
        }

        public void SetGameActive(bool active)
        {
            isGameActive = active;
            if (active)
            {
                manualCutsceneLight = false;
                isPaused = false;
                hasDepleted = currentOil <= 0f;
                Time.timeScale = 1f;
                SnapLightToCurrentOil();
                NotifyOilUI();
                NotifyLampButtonsUI();
                OnSpiritVisionChanged?.Invoke(IsSpiritVisionActive);
            }
            else
            {
                AudioManager.Instance?.SetLowOilHeartbeat(false, 1f);
                OnSpiritVisionChanged?.Invoke(false);
            }
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
            if (!paused)
            {
                Time.timeScale = 1f;
            }
            else
            {
                AudioManager.Instance?.SetLowOilHeartbeat(false, 1f);
            }
        }

        public void SetDraining(bool enabled)
        {
            if (enabled)
            {
                SetGameActive(true);
            }
            else
            {
                isPaused = true;
                AudioManager.Instance?.SetLowOilHeartbeat(false, 1f);
            }
        }

        public void ResetLampModesForFloor()
        {
            isSpiritFlame = false;
            isExtinguished = false;
            NotifyLampButtonsUI();
            OnLampExtinguishedChanged?.Invoke(false);
            OnSpiritVisionChanged?.Invoke(false);
        }

        public void ToggleFlameMode()
        {
            if (!isGameActive || hasDepleted) return;
            SetSpiritFlame(!isSpiritFlame);
        }

        public void SetSpiritFlame(bool spirit)
        {
            isSpiritFlame = spirit;
            AudioManager.Instance?.PlayPuzzleClick();
            NotifyLampButtonsUI();
            OnSpiritVisionChanged?.Invoke(IsSpiritVisionActive);
        }

        public void ToggleLampOnOff()
        {
            if (!isGameActive || hasDepleted) return;
            SetLampExtinguished(!isExtinguished);
        }

        public void SetLampExtinguished(bool extinguished)
        {
            if (isExtinguished == extinguished) return;
            isExtinguished = extinguished;

            if (isExtinguished)
            {
                AudioManager.Instance?.PlayDrawerClose();
                if (lampLight != null)
                {
                    lampLight.enabled = false;
                    lampLight.range = 0f;
                    lampLight.intensity = 0f;
                }
            }
            else
            {
                AudioManager.Instance?.PlayLampIgnite();
                SnapLightToCurrentOil();
            }

            NotifyLampButtonsUI();
            OnLampExtinguishedChanged?.Invoke(isExtinguished);
            OnSpiritVisionChanged?.Invoke(IsSpiritVisionActive);
        }

        public void UpdateLanternPropsMotion(float normalizedMoveSpeed)
        {
            float ratio = OilNormalized;
            firstPersonLantern?.UpdateLanternMotion(normalizedMoveSpeed, ratio, isSpiritFlame, isExtinguished);
            thirdPersonMaleLantern?.UpdateLanternMotion(normalizedMoveSpeed, ratio, isSpiritFlame, isExtinguished);
            thirdPersonFemaleLantern?.UpdateLanternMotion(normalizedMoveSpeed, ratio, isSpiritFlame, isExtinguished);
        }

        public void SetFloorLightProfile(int floorIndex)
        {
            maxLightRange = 6.5f;
            spiritMaxLightRange = 4.0f;
            minLightRange = 1.5f;
            maxLightIntensity = floorIndex == 0 ? 18.5f : (floorIndex == 1 ? 16.5f : 14.5f);
        }

        public float GetFullFloorRange(int floorIndex) => maxLightRange;

        public float GetFullFloorIntensity(int floorIndex)
        {
            return floorIndex == 0 ? 18.5f : (floorIndex == 1 ? 16.5f : 14.5f);
        }

        public void SetOil(float amount, bool snapLight = false)
        {
            currentOil = Mathf.Clamp(amount, 0f, 100f);
            hasDepleted = currentOil <= 0f;
            NotifyOilUI();

            if (snapLight)
            {
                manualCutsceneLight = false;
                SnapLightToCurrentOil();
            }
        }

        public void AddOil(float amount = 20f)
        {
            if (hasDepleted) return;
            float restore = amount > 0f ? amount : oilPickupRestore;
            currentOil = Mathf.Clamp(currentOil + restore, 0f, 100f);
            flareBonus = 0.55f;
            NotifyOilUI();
        }

        public void DrainOilImmediate(float amount)
        {
            if (hasDepleted) return;
            currentOil = Mathf.Clamp(currentOil - amount, 0f, 100f);
            NotifyOilUI();

            if (currentOil <= 0f)
            {
                TriggerDepletion();
            }
        }

        public void SetManualLightState(bool manual, bool lightEnabled, float range, float intensity)
        {
            manualCutsceneLight = manual;
            if (manual)
            {
                isGameActive = false;
                OnSpiritVisionChanged?.Invoke(false);
            }
            if (lampLight != null)
            {
                lampLight.enabled = lightEnabled;
                lampLight.range = range;
                lampLight.intensity = intensity;
                lampLight.color = lampWarmColor;
            }
        }

        public void SnapLightToCurrentOil()
        {
            manualCutsceneLight = false;
            if (lampLight == null) return;

            if (currentOil <= 0f || isExtinguished)
            {
                lampLight.enabled = false;
                lampLight.range = 0f;
                lampLight.intensity = 0f;
                return;
            }

            lampLight.enabled = true;
            ComputeTargetLight(out float targetRange, out float targetIntensity);
            lampLight.range = targetRange;
            lampLight.intensity = targetIntensity;
            lampLight.color = isSpiritFlame ? lampSpiritColor : lampWarmColor;
        }

        private void NotifyOilUI()
        {
            OnOilChanged?.Invoke(currentOil, 100f);
            if (uiManager != null)
            {
                uiManager.UpdateOilUI(currentOil, 100f);
            }
        }

        private void NotifyLampButtonsUI()
        {
            if (uiManager != null)
            {
                uiManager.UpdateFlameModeButton(isSpiritFlame);
                uiManager.UpdateLampPowerButton(!isExtinguished);
            }
        }

        private void UpdateLightSmooth(float dt)
        {
            if (lampLight == null) return;

            if (currentOil <= 0f || hasDepleted || isExtinguished)
            {
                lampLight.range = Mathf.Lerp(lampLight.range, 0f, dt * 16f);
                lampLight.intensity = Mathf.Lerp(lampLight.intensity, 0f, dt * 16f);
                if (lampLight.intensity < 0.08f) lampLight.enabled = false;
                return;
            }

            if (!lampLight.enabled) lampLight.enabled = true;

            ComputeTargetLight(out float targetRange, out float targetIntensity);

            float ratio = OilNormalized;
            float flickerStrength = ratio < 0.25f ? 0.16f : 0.055f;
            float noise = Mathf.PerlinNoise(flickerSeed, Time.time * (ratio < 0.25f ? 12.5f : 6.0f)) - 0.5f;
            float flickerMult = 1f + noise * 2f * flickerStrength;

            float lerpFactor = 1f - Mathf.Exp(-lightSmoothSpeed * dt);
            lampLight.range = Mathf.Lerp(lampLight.range, (targetRange + flareBonus) * flickerMult, lerpFactor);
            lampLight.intensity = Mathf.Lerp(lampLight.intensity, targetIntensity * flickerMult, lerpFactor);
            Color targetColor = isSpiritFlame ? lampSpiritColor : lampWarmColor;
            lampLight.color = Color.Lerp(lampLight.color, targetColor, lerpFactor);
        }

        private void ComputeTargetLight(out float targetRange, out float targetIntensity)
        {
            // Linear scaling: 6.5f at 100% -> 1.5f at 0% (Warm), or tighter 4.0f -> 1.5f (Spirit)
            float ratio = OilNormalized;
            float topRange = isSpiritFlame ? spiritMaxLightRange : maxLightRange;
            targetRange = Mathf.Lerp(minLightRange, topRange, ratio);
            float intensityCap = isSpiritFlame ? maxLightIntensity * 0.85f : maxLightIntensity;
            targetIntensity = Mathf.Lerp(minLightIntensity, intensityCap, ratio);
        }

        private void TriggerDepletion()
        {
            hasDepleted = true;
            isGameActive = false;
            isPaused = false;
            isExtinguished = false;
            currentOil = 0f;
            AudioManager.Instance?.SetLowOilHeartbeat(false, 0f);
            OnSpiritVisionChanged?.Invoke(false);

            if (lampLight != null)
            {
                lampLight.range = 0f;
                lampLight.intensity = 0f;
                lampLight.enabled = false;
            }

            NotifyOilUI();
            OnOilDepleted?.Invoke();
        }
    }
}
