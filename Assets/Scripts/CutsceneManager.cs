using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastLight
{
    /// <summary>
    /// Live Movie-Style 12-Second 2-Act Opening Cutscene (skippable via top-right "SKIP >>"):
    /// - Act 1 (Jungle Campfire, ~6s): Forest clearing with pine trees, tent, logs, flickering campfire.
    ///   Player sits by the fire; 2 masked Bandits ("Daku") emerge from the dark trees with clubs/torches,
    ///   surround the player, trigger a dramatic camera zoom + hit blackout + subtitle:
    ///   "Taken from the forest... locked inside the Forgotten Haveli."
    /// - Act 2 (Mansion Bed Wake-Up, ~6s): Fade in inside the dark decaying Old Haveli bedroom.
    ///   Player wakes on the carved 4-poster bed, steps onto the floor, picks up the antique brass oil lantern
    ///   from the bedside table, ignites its flickering flame, and seamlessly enters First-Person gameplay.
    /// </summary>
    public class CutsceneManager : MonoBehaviour
    {
        [Header("Core References")]
        public UIManager uiManager;
        public PlayerController playerController;
        public LampController lampController;
        public CameraController cameraController;

        [Header("Act 1 — Jungle Campfire Setpiece")]
        public GameObject forestSceneRoot;
        public Transform forestPlayerSitPoint;
        public Transform campfireCenter;
        public Transform bandit1Transform;
        public Transform bandit2Transform;
        public Transform bandit1StartPoint;
        public Transform bandit2StartPoint;
        public Transform bandit1SurroundPoint;
        public Transform bandit2SurroundPoint;
        public Transform act1CamStartPoint;
        public Transform act1CamEndPoint;

        private Coroutine cutsceneRoutine;
        private Action onCutsceneComplete;
        private FloorController activeFloor1;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        private void Awake()
        {
            if (uiManager != null && uiManager.skipIntroButton != null)
            {
                uiManager.skipIntroButton.onClick.RemoveListener(SkipCutscene);
                uiManager.skipIntroButton.onClick.AddListener(SkipCutscene);
            }
        }

        public void PrepareStartScreenBackdrop(FloorController floor1)
        {
            activeFloor1 = floor1;
            if (forestSceneRoot != null) forestSceneRoot.SetActive(false);
            AudioManager.Instance?.SetCampfireAmbience(false);

            if (floor1 == null || playerController == null) return;

            playerController.SetInputEnabled(false);
            playerController.SetIntroPhysicsMode(true);
            playerController.characterVisual?.SetFirstPersonBodyHidden(false);

            if (floor1.nightstandLampObject != null) floor1.nightstandLampObject.SetActive(true);

            Vector3 bedPos = floor1.introBedLyingPoint != null ? floor1.introBedLyingPoint.position : new Vector3(-8.6f, 0.80f, -8.2f);
            playerController.transform.position = bedPos;
            playerController.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (lampController != null)
            {
                lampController.SetDraining(false);
                lampController.SetFloorLightProfile(0);
                lampController.SetOil(lampController.startingOil, snapLight: false);
                lampController.SetManualLightState(manual: true, lightEnabled: false, range: 0f, intensity: 0f);
            }

            if (cameraController != null)
            {
                cameraController.SetCutsceneOverride(true);
                cameraController.transform.position = bedPos + new Vector3(2.8f, 1.4f, 2.2f);
                cameraController.transform.LookAt(bedPos + Vector3.up * 0.25f);
            }
        }

        public void PlayOpeningMovie(FloorController floor1, Action onComplete)
        {
            if (cutsceneRoutine != null) StopCoroutine(cutsceneRoutine);

            activeFloor1 = floor1;
            onCutsceneComplete = onComplete;
            isPlaying = true;

            if (uiManager != null) uiManager.ShowIntroOverlay();

            cutsceneRoutine = StartCoroutine(OpeningMovieRoutine(floor1));
        }

        public void SkipCutscene()
        {
            if (!isPlaying) return;
            if (cutsceneRoutine != null)
            {
                StopCoroutine(cutsceneRoutine);
                cutsceneRoutine = null;
            }
            FinishCutsceneImmediate();
        }

        public void CancelForDirectStart(FloorController floor)
        {
            if (cutsceneRoutine != null)
            {
                StopCoroutine(cutsceneRoutine);
                cutsceneRoutine = null;
            }
            isPlaying = false;
            onCutsceneComplete = null;

            if (forestSceneRoot != null) forestSceneRoot.SetActive(false);
            AudioManager.Instance?.SetCampfireAmbience(false);
            if (cameraController != null) cameraController.SetCutsceneOverride(false);
            if (floor != null && floor.nightstandLampObject != null) floor.nightstandLampObject.SetActive(false);
        }

        private IEnumerator OpeningMovieRoutine(FloorController floor1)
        {
            CanvasGroup backdrop = uiManager != null ? uiManager.introBackdropGroup : null;
            Text subtitle = uiManager != null ? uiManager.introStoryText : null;

            playerController.SetInputEnabled(false);
            playerController.SetIntroPhysicsMode(true);
            playerController.characterVisual?.SetFirstPersonBodyHidden(false);

            if (lampController != null)
            {
                lampController.SetDraining(false);
                lampController.SetManualLightState(manual: true, lightEnabled: false, range: 0f, intensity: 0f);
            }

            // =========================================================================
            // ACT 1 — JUNGLE CAMPFIRE AMBUSH (~6.0s)
            // =========================================================================
            if (floor1 != null) floor1.gameObject.SetActive(false);
            if (forestSceneRoot != null) forestSceneRoot.SetActive(true);
            AudioManager.Instance?.SetCampfireAmbience(true);

            // Place Player sitting by the campfire
            Vector3 sitPos = forestPlayerSitPoint != null ? forestPlayerSitPoint.position : new Vector3(-70f, 0f, -2.0f);
            Vector3 firePos = campfireCenter != null ? campfireCenter.position : new Vector3(-70f, 0.3f, 0f);
            playerController.transform.position = sitPos;
            Vector3 lookFire = firePos - sitPos;
            lookFire.y = 0f;
            playerController.transform.rotation = lookFire.sqrMagnitude > 0.01f ? Quaternion.LookRotation(lookFire.normalized) : Quaternion.identity;

            // Place Bandits hidden in the dark pine trees
            if (bandit1Transform != null && bandit1StartPoint != null) bandit1Transform.position = bandit1StartPoint.position;
            if (bandit2Transform != null && bandit2StartPoint != null) bandit2Transform.position = bandit2StartPoint.position;

            if (cameraController != null)
            {
                cameraController.SetCutsceneOverride(true);
                if (act1CamStartPoint != null) cameraController.transform.position = act1CamStartPoint.position;
                cameraController.transform.LookAt(firePos + Vector3.up * 0.6f);
            }

            if (subtitle != null)
            {
                subtitle.text = "";
                SetTextAlpha(subtitle, 0f);
            }

            // Fade in from black into the flickering jungle campfire (0.45s)
            yield return FadeCanvasGroup(backdrop, 1f, 0f, 0.45f);

            // Peaceful fire moment while Bandits begin creeping out of the trees (2.2s)
            Vector3 camA = act1CamStartPoint != null ? act1CamStartPoint.position : sitPos + new Vector3(3.2f, 2.2f, -3.5f);
            Vector3 camB = act1CamEndPoint != null ? act1CamEndPoint.position : sitPos + new Vector3(-2.4f, 1.8f, -2.8f);
            Vector3 b1Start = bandit1StartPoint != null ? bandit1StartPoint.position : sitPos + new Vector3(-6.5f, 0f, 4.5f);
            Vector3 b2Start = bandit2StartPoint != null ? bandit2StartPoint.position : sitPos + new Vector3(6.5f, 0f, 4.2f);
            Vector3 b1Mid = Vector3.Lerp(b1Start, bandit1SurroundPoint != null ? bandit1SurroundPoint.position : sitPos + new Vector3(-1.3f, 0f, 0.8f), 0.55f);
            Vector3 b2Mid = Vector3.Lerp(b2Start, bandit2SurroundPoint != null ? bandit2SurroundPoint.position : sitPos + new Vector3(1.3f, 0f, 0.8f), 0.55f);

            float elapsed = 0f;
            float creepDur = 2.15f;
            while (elapsed < creepDur)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(elapsed / creepDur);
                if (cameraController != null)
                {
                    cameraController.transform.position = Vector3.Lerp(camA, camB, t);
                    cameraController.transform.LookAt(Vector3.Lerp(firePos + Vector3.up * 0.6f, sitPos + Vector3.up * 1.1f, t));
                }
                MoveBandit(bandit1Transform, b1Start, b1Mid, sitPos, t);
                MoveBandit(bandit2Transform, b2Start, b2Mid, sitPos, t);
                yield return null;
            }

            // Bandits rush & surround the player + dramatic camera zoom + hit blackout! (1.2s)
            Vector3 b1End = bandit1SurroundPoint != null ? bandit1SurroundPoint.position : sitPos + new Vector3(-1.15f, 0f, 0.65f);
            Vector3 b2End = bandit2SurroundPoint != null ? bandit2SurroundPoint.position : sitPos + new Vector3(1.15f, 0f, 0.65f);
            Vector3 zoomEnd = sitPos + new Vector3(0.15f, 1.35f, -1.25f);

            elapsed = 0f;
            float rushDur = 1.15f;
            bool stingerPlayed = false;
            while (elapsed < rushDur)
            {
                elapsed += Time.deltaTime;
                float rawT = Mathf.Clamp01(elapsed / rushDur);
                float t = SmoothStep01(rawT);

                MoveBandit(bandit1Transform, b1Mid, b1End, sitPos, t);
                MoveBandit(bandit2Transform, b2Mid, b2End, sitPos, t);

                if (cameraController != null)
                {
                    Vector3 shake = rawT > 0.72f ? UnityEngine.Random.insideUnitSphere * 0.14f : Vector3.zero;
                    cameraController.transform.position = Vector3.Lerp(camB, zoomEnd, t) + shake;
                    cameraController.transform.LookAt(sitPos + Vector3.up * 1.25f);
                }

                if (rawT > 0.80f && !stingerPlayed)
                {
                    stingerPlayed = true;
                    AudioManager.Instance?.PlayAmbushStinger();
                }

                yield return null;
            }

            // Instant hit blackout + atmospheric subtitle (2.2s)
            if (backdrop != null) backdrop.alpha = 1f;
            AudioManager.Instance?.SetCampfireAmbience(false);
            if (forestSceneRoot != null) forestSceneRoot.SetActive(false);

            yield return ShowSubtitle(subtitle, "Taken from the forest... locked inside the Forgotten Haveli.", 2.2f);

            // =========================================================================
            // ACT 2 — OLD HAVELI BED WAKE-UP & ANTIQUE LANTERN PICKUP (~6.0s)
            // =========================================================================
            if (floor1 != null)
            {
                floor1.gameObject.SetActive(true);
                if (floor1.nightstandLampObject != null) floor1.nightstandLampObject.SetActive(true);
            }

            Vector3 bedLyingPos = floor1 != null && floor1.introBedLyingPoint != null
                ? floor1.introBedLyingPoint.position
                : new Vector3(-8.6f, 0.80f, -8.2f);
            Vector3 stepOffPos = floor1 != null && floor1.introBedStepOffPoint != null
                ? floor1.introBedStepOffPoint.position
                : new Vector3(-6.4f, 0f, -7.4f);
            Vector3 pickupPos = floor1 != null && floor1.playerSpawnPoint != null
                ? floor1.playerSpawnPoint.position
                : new Vector3(-5.8f, 0f, -6.8f);

            playerController.transform.position = bedLyingPos;
            playerController.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Vector3 act2CamPos = bedLyingPos + new Vector3(3.4f, 1.55f, 2.3f);
            if (cameraController != null)
            {
                cameraController.transform.position = act2CamPos;
                cameraController.transform.LookAt(bedLyingPos + Vector3.up * 0.25f);
            }

            // Fade in inside dark Old Haveli bedroom (0.7s)
            yield return FadeCanvasGroup(backdrop, 1f, 0f, 0.70f);

            // Sit up on the 4-poster wooden bed (1.35s)
            Quaternion lyingRot = Quaternion.Euler(90f, 0f, 0f);
            Quaternion sitUpRot = Quaternion.Euler(0f, 95f, 0f);
            Vector3 sitBedPos = new Vector3(bedLyingPos.x, bedLyingPos.y - 0.14f, bedLyingPos.z);

            elapsed = 0f;
            float sitDur = 1.35f;
            while (elapsed < sitDur)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(elapsed / sitDur);
                playerController.transform.position = Vector3.Lerp(bedLyingPos, sitBedPos, t);
                playerController.transform.rotation = Quaternion.Slerp(lyingRot, sitUpRot, t);
                if (cameraController != null) cameraController.transform.LookAt(playerController.transform.position + Vector3.up * 0.9f);
                yield return null;
            }

            // Step off the bed onto the wooden/stone floor (1.25s)
            Vector3 floorPos = new Vector3(stepOffPos.x, playerController.standingHeightY, stepOffPos.z);
            elapsed = 0f;
            float stepDur = 1.25f;
            while (elapsed < stepDur)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(elapsed / stepDur);
                playerController.transform.position = Vector3.Lerp(sitBedPos, floorPos, t);
                playerController.characterVisual?.UpdateWalkAnimation(0.65f, Time.deltaTime, playFootsteps: true);
                if (cameraController != null) cameraController.transform.LookAt(playerController.transform.position + Vector3.up * 1.1f);
                yield return null;
            }

            // Step beside the nightstand & pick up the antique brass oil lantern (1.15s)
            Vector3 lampStandPos = new Vector3(pickupPos.x, playerController.standingHeightY, pickupPos.z);
            Vector3 toLamp = lampStandPos - floorPos;
            toLamp.y = 0f;
            Quaternion walkRot = toLamp.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toLamp.normalized) : Quaternion.identity;

            elapsed = 0f;
            float walkDur = 1.15f;
            while (elapsed < walkDur)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(elapsed / walkDur);
                playerController.transform.position = Vector3.Lerp(floorPos, lampStandPos, t);
                playerController.transform.rotation = Quaternion.Slerp(sitUpRot, walkRot, t);
                playerController.characterVisual?.UpdateWalkAnimation(0.75f, Time.deltaTime, playFootsteps: true);
                if (cameraController != null) cameraController.transform.LookAt(playerController.transform.position + Vector3.up * 1.1f);
                yield return null;
            }
            playerController.characterVisual?.ResetPose();

            // Pick up antique lantern -> deactivate bedside lamp & ignite Player's warm flickering Point Light (0.55s)
            if (floor1 != null && floor1.nightstandLampObject != null)
            {
                floor1.nightstandLampObject.SetActive(false);
            }
            AudioManager.Instance?.PlayLampIgnite();

            if (lampController != null)
            {
                float fullRange = lampController.GetFullFloorRange(0) * 0.85f;
                float fullIntensity = lampController.GetFullFloorIntensity(0) * 0.85f;
                elapsed = 0f;
                float igniteDur = 0.55f;
                while (elapsed < igniteDur)
                {
                    elapsed += Time.deltaTime;
                    float t = SmoothStep01(elapsed / igniteDur);
                    lampController.SetManualLightState(true, true, Mathf.Lerp(1.2f, fullRange, t), Mathf.Lerp(3f, fullIntensity, t));
                    yield return null;
                }
            }

            cutsceneRoutine = null;
            FinishCutsceneImmediate();
        }

        private void FinishCutsceneImmediate()
        {
            isPlaying = false;
            AudioManager.Instance?.SetCampfireAmbience(false);
            if (forestSceneRoot != null) forestSceneRoot.SetActive(false);

            if (activeFloor1 != null)
            {
                activeFloor1.gameObject.SetActive(true);
                if (activeFloor1.nightstandLampObject != null)
                {
                    activeFloor1.nightstandLampObject.SetActive(false);
                }

                Vector3 spawnPos = activeFloor1.playerSpawnPoint != null
                    ? activeFloor1.playerSpawnPoint.position
                    : new Vector3(-5.8f, 0f, -6.8f);

                if (playerController != null)
                {
                    playerController.SetIntroPhysicsMode(false);
                    playerController.TeleportUpright(spawnPos, facingYaw: 25f);
                }
            }
            else if (playerController != null)
            {
                playerController.SetIntroPhysicsMode(false);
            }

            if (cameraController != null)
            {
                cameraController.SetCutsceneOverride(false);
                // Default to First-Person view as gameplay begins!
                cameraController.SetViewMode(CameraViewMode.FirstPerson);
                cameraController.SetYaw(25f, 4f);
                cameraController.SnapToTarget();
            }

            if (lampController != null)
            {
                lampController.SetFloorLightProfile(0);
                lampController.SetOil(lampController.startingOil, snapLight: true);
            }

            Action callback = onCutsceneComplete;
            onCutsceneComplete = null;
            callback?.Invoke();
        }

        private static void MoveBandit(Transform bandit, Vector3 from, Vector3 to, Vector3 lookTarget, float t)
        {
            if (bandit == null) return;
            Vector3 pos = Vector3.Lerp(from, to, t);
            pos.y += Mathf.Abs(Mathf.Sin(t * Mathf.PI * 5f)) * 0.06f;
            bandit.position = pos;

            Vector3 dir = lookTarget - pos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                bandit.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        private static IEnumerator ShowSubtitle(Text textComp, string message, float duration)
        {
            if (textComp == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            textComp.text = message;
            float fadeIn = 0.35f;
            float fadeOut = 0.40f;
            float hold = Mathf.Max(0.2f, duration - fadeIn - fadeOut);

            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                SetTextAlpha(textComp, Mathf.Clamp01(elapsed / fadeIn));
                yield return null;
            }
            SetTextAlpha(textComp, 1f);

            yield return new WaitForSeconds(hold);

            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                SetTextAlpha(textComp, 1f - Mathf.Clamp01(elapsed / fadeOut));
                yield return null;
            }
            SetTextAlpha(textComp, 0f);
        }

        private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            group.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration)));
                yield return null;
            }
            group.alpha = to;
        }

        private static void SetTextAlpha(Text text, float alpha)
        {
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }

        private static float SmoothStep01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
