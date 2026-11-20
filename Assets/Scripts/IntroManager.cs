using System;
using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Facade coordinating the 2-Act Live Cutscene (Jungle Campfire Ambush + Old Haveli Bed Wake-Up)
    /// via CutsceneManager.
    /// </summary>
    public class IntroManager : MonoBehaviour
    {
        public CutsceneManager cutsceneManager;

        public bool IsPlaying => cutsceneManager != null && cutsceneManager.IsPlaying;

        public void PrepareBedIntroPose(FloorController floor1)
        {
            cutsceneManager?.PrepareStartScreenBackdrop(floor1);
        }

        public void PlayIntro(FloorController floor1, Action onComplete)
        {
            if (cutsceneManager != null)
            {
                cutsceneManager.PlayOpeningMovie(floor1, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        public void SkipIntro()
        {
            cutsceneManager?.SkipCutscene();
        }

        public void CancelIntroForDirectStart(FloorController floor)
        {
            cutsceneManager?.CancelForDirectStart(floor);
        }
    }
}
