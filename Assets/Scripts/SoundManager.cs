using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Compatibility wrapper delegating to AudioManager.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void PlayOilPickup() => AudioManager.Instance?.PlayOilPickup();
        public void PlayRealKey() => AudioManager.Instance?.PlayPuzzleUnlock();
        public void PlayFakeKey() => AudioManager.Instance?.PlayPuzzleError();
        public void PlaySwitch() => AudioManager.Instance?.PlayPuzzleClick();
        public void PlayDoorUnlock() => AudioManager.Instance?.PlayDoorSwing();
        public void PlayDoorLocked() => AudioManager.Instance?.PlayDoorLocked();
        public void PlayLampIgnite() => AudioManager.Instance?.PlayLampIgnite();
        public void PlayGameOver() => AudioManager.Instance?.PlayGameOver();
        public void PlayVictory() => AudioManager.Instance?.PlayVictory();
    }
}
