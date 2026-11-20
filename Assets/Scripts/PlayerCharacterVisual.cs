using UnityEngine;

namespace LastLight
{
    public enum CharacterGender
    {
        Male,
        Female
    }

    /// <summary>
    /// Manages realistic-proportioned procedural 3D humanoid models (Male & Female),
    /// procedural walk bob & limb swing animation, and First-Person body visibility toggling.
    /// </summary>
    public class PlayerCharacterVisual : MonoBehaviour
    {
        [System.Serializable]
        public class HumanoidRig
        {
            public GameObject root;
            public Transform visualRoot;
            public Transform leftArm;
            public Transform rightArm;
            public Transform leftLeg;
            public Transform rightLeg;
        }

        public HumanoidRig maleRig = new HumanoidRig();
        public HumanoidRig femaleRig = new HumanoidRig();

        [Header("Walk Animation")]
        public float walkCycleSpeed = 10.0f;
        public float legSwingAngle = 30f;
        public float leftArmSwingAngle = 24f;
        public float bobAmplitude = 0.055f;

        private CharacterGender activeGender = CharacterGender.Male;
        private bool hideBodyForFirstPerson;
        private float walkPhase;
        private float stepSoundAccumulator;

        public CharacterGender ActiveGender => activeGender;
        public HumanoidRig ActiveRig => activeGender == CharacterGender.Female ? femaleRig : maleRig;

        public void SetGender(CharacterGender gender)
        {
            activeGender = gender;
            ApplyActiveVisibility();
            ResetPose();
        }

        public void SetFirstPersonBodyHidden(bool hidden)
        {
            hideBodyForFirstPerson = hidden;
            ApplyActiveVisibility();
        }

        private void ApplyActiveVisibility()
        {
            if (maleRig != null && maleRig.root != null)
            {
                maleRig.root.SetActive(!hideBodyForFirstPerson && activeGender == CharacterGender.Male);
            }
            if (femaleRig != null && femaleRig.root != null)
            {
                femaleRig.root.SetActive(!hideBodyForFirstPerson && activeGender == CharacterGender.Female);
            }
        }

        public void UpdateWalkAnimation(float normalizedSpeed, float dt, bool playFootsteps)
        {
            if (normalizedSpeed > 0.08f)
            {
                float phaseStep = walkCycleSpeed * Mathf.Clamp(normalizedSpeed, 0.35f, 1.25f) * dt;
                walkPhase += phaseStep;

                if (playFootsteps)
                {
                    stepSoundAccumulator += phaseStep;
                    if (stepSoundAccumulator >= Mathf.PI)
                    {
                        stepSoundAccumulator -= Mathf.PI;
                        AudioManager.Instance?.PlayFootstep();
                    }
                }
            }

            HumanoidRig rig = ActiveRig;
            if (rig == null || rig.visualRoot == null || hideBodyForFirstPerson) return;

            if (normalizedSpeed > 0.08f)
            {
                float swing = Mathf.Sin(walkPhase);
                float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * bobAmplitude * normalizedSpeed;

                rig.visualRoot.localPosition = new Vector3(0f, bob, 0f);
                if (rig.leftLeg != null) rig.leftLeg.localRotation = Quaternion.Euler(swing * legSwingAngle, 0f, 0f);
                if (rig.rightLeg != null) rig.rightLeg.localRotation = Quaternion.Euler(-swing * legSwingAngle, 0f, 0f);
                if (rig.leftArm != null) rig.leftArm.localRotation = Quaternion.Euler(-swing * leftArmSwingAngle, 0f, 0f);
                if (rig.rightArm != null) rig.rightArm.localRotation = Quaternion.Euler(-38f + swing * 6f, 0f, 0f);
            }
            else
            {
                float idleBlend = 1f - Mathf.Exp(-10f * dt);
                float breathe = Mathf.Sin(Time.time * 2.0f) * 0.012f;
                rig.visualRoot.localPosition = Vector3.Lerp(rig.visualRoot.localPosition, new Vector3(0f, breathe, 0f), idleBlend);

                if (rig.leftLeg != null) rig.leftLeg.localRotation = Quaternion.Slerp(rig.leftLeg.localRotation, Quaternion.identity, idleBlend);
                if (rig.rightLeg != null) rig.rightLeg.localRotation = Quaternion.Slerp(rig.rightLeg.localRotation, Quaternion.identity, idleBlend);
                if (rig.leftArm != null) rig.leftArm.localRotation = Quaternion.Slerp(rig.leftArm.localRotation, Quaternion.identity, idleBlend);
                if (rig.rightArm != null) rig.rightArm.localRotation = Quaternion.Slerp(rig.rightArm.localRotation, Quaternion.Euler(-36f, 0f, 0f), idleBlend);
            }
        }

        public void ResetPose()
        {
            walkPhase = 0f;
            stepSoundAccumulator = 0f;
            ResetRig(maleRig);
            ResetRig(femaleRig);
        }

        private static void ResetRig(HumanoidRig rig)
        {
            if (rig == null) return;
            if (rig.visualRoot != null) rig.visualRoot.localPosition = Vector3.zero;
            if (rig.leftLeg != null) rig.leftLeg.localRotation = Quaternion.identity;
            if (rig.rightLeg != null) rig.rightLeg.localRotation = Quaternion.identity;
            if (rig.leftArm != null) rig.leftArm.localRotation = Quaternion.identity;
            if (rig.rightArm != null) rig.rightArm.localRotation = Quaternion.Euler(-36f, 0f, 0f);
        }
    }
}
