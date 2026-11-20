using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using LastLight;

namespace LastLight.Editor
{
    /// <summary>
    /// Zero-drag-drop scene builder & one-click Android APK exporter for "Last Light"
    /// ("POLYGON Horror Mansion" stylized low-poly horror escape edition).
    /// Menu: GameJam/Build Last Light  &  GameJam/Build Android APK
    /// </summary>
    public static class AutoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/LastLight.unity";
        private const string MaterialsDir = "Assets/Materials";
        private const string MeshesDir = "Assets/Meshes";
        private const string SpritesDir = "Assets/Sprites";
        private const string ApkOutputPath = "Builds/LastLight.apk";

        private enum ContainerStyle
        {
            TallWardrobe,
            ThreeDrawerChest,
            BedsideNightstand,
            WritingDesk,
            ApothecaryChest
        }

        private struct LowPolyMeshes
        {
            public Mesh torsoMale;
            public Mesh torsoFemale;
            public Mesh skirtCloak;
            public Mesh head;
            public Mesh hairCap;
            public Mesh hairPonytail;
            public Mesh turban;
            public Mesh bandana;
            public Mesh limb;
            public Mesh boot;
            public Mesh club;
            public Mesh lanternGlass;
            public Mesh lanternCap;
            public Mesh lanternBase;
            public Mesh pineCone;
            public Mesh treeTrunk;
            public Mesh tent;
            public Mesh rock;
            public Mesh flameCone;
            public Mesh gothicPillar;
            public Mesh cobwebQuad;
            public Mesh ratBody;
            public Mesh ratSnout;
            public Mesh spiderBody;
        }

        private struct MaterialPalette
        {
            public Material floor1;
            public Material floor2;
            public Material floor3;
            public Material wall;
            public Material darkWall;
            public Material stairsStone;
            public Material darkWood;
            public Material midWood;
            public Material mahoganyWood;
            public Material ebonyWood;
            public Material crateWood;
            public Material brassMetal;
            public Material ironMetal;
            public Material waxCandle;
            public Material linenWhite;
            public Material blanketRed;
            public Material rugCrimson;
            public Material rugSlate;
            public Material paintingPortrait;
            public Material paintingMansion;
            public Material paintingMoonTree;
            public Material paintingMirror;
            public Material cobwebMat;
            public Material spiderBodyMat;
            public Material spiderRedEyeUnlit;
            public Material spiritCyanUnlit;
            public Material spiritRedUnlit;
            public Material ratFurMat;
            public Material ratTailPinkMat;
            public Material bookRed;
            public Material bookBlue;
            public Material bookGreen;
            public Material maleCoat;
            public Material malePants;
            public Material maleHair;
            public Material femaleTunic;
            public Material femalePants;
            public Material femaleHair;
            public Material banditCloth;
            public Material banditTurban;
            public Material skin;
            public Material lampGlass;
            public Material oilFlask;
            public Material puzzlePieceGold;
            public Material parchmentPaper;
            public Material chestWood;
            public Material doorWood;
            public Material doorFrame;
            // Forest Act 1 materials
            public Material forestGrass;
            public Material pineLeaf;
            public Material tentCanvas;
            public Material campfireFlame;
        }

        [MenuItem("GameJam/Build Last Light")]
        public static void BuildLastLightScene()
        {
            EnsureDirectory("Assets/Scenes");
            EnsureDirectory(MaterialsDir);
            EnsureDirectory(MeshesDir);
            EnsureDirectory(SpritesDir);

            UIStyleBuilder.GothicUiAssets uiAssets = UIStyleBuilder.GenerateAll(SpritesDir);
            MaterialPalette mats = CreateStrictDarkMaterials(uiAssets);
            LowPolyMeshes meshes = CreateLowPolyMeshes();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigurePureBlackLighting();

            GameObject managersRoot = new GameObject("--- MANAGERS ---");
            GameObject worldRoot = new GameObject("--- WORLD ---");
            GameObject uiRoot = new GameObject("--- UI ---");

            // 1. AudioManager (eerie dark mansion wind/drone, campfire, ambush stinger, footsteps, creaks, heartbeat)
            GameObject audioObj = new GameObject("AudioManager");
            audioObj.transform.SetParent(managersRoot.transform);
            audioObj.AddComponent<AudioManager>();

            // 2. Player (Stylized Low-Poly Male & Female Humanoids + Handheld Antique Brass Lantern)
            PlayerController playerController = CreatePlayer(worldRoot.transform, mats, meshes,
                out LampController lampController, out PlayerCharacterVisual charVisual);

            // 3. First-Person Default Camera + Over-the-Shoulder 3P Toggle (+ 1P Antique Lantern in lower-right)
            CameraController cameraController = CreateMainCamera(playerController.transform, charVisual, mats, meshes, out LanternProp fpLantern);
            playerController.cameraController = cameraController;
            lampController.firstPersonLantern = fpLantern;

            // 4. Act 1 Forest Campfire Setpiece (X = -70, inactive outside Act 1!)
            CutsceneManager cutsceneManager = BuildForestCampfireAndCutsceneManager(
                managersRoot.transform, worldRoot.transform, mats, meshes, playerController, lampController, cameraController);

            // 5. Live 3D Rotating Character Preview Stage (X = -140, active ONLY on Character Select!)
            CharacterPreviewStage previewStage = BuildCharacterPreviewStage(
                worldRoot.transform, mats, meshes, out RenderTexture maleRT, out RenderTexture femaleRT);

            // 6. Build 3 Distinct Old Haveli Mansion Floors (Strict Lighting: ONLY Lamp + Dim Red Exit Door Light!)
            FloorController floor1 = BuildFloor1(worldRoot.transform, new Vector3(0f, 0f, 0f), mats, meshes, lampController, playerController.transform);
            FloorController floor2 = BuildFloor2(worldRoot.transform, new Vector3(65f, 0f, 0f), mats, meshes, lampController, playerController.transform);
            FloorController floor3 = BuildFloor3(worldRoot.transform, new Vector3(130f, 0f, 0f), mats, meshes, lampController, playerController.transform);

            floor1.gameObject.SetActive(true);
            floor2.gameObject.SetActive(false);
            floor3.gameObject.SetActive(false);

            // 7. Graphical Dark-Gothic Victorian UI (NO COMPASS, NO BOOST!)
            UIManager uiManager = BuildDarkGothicUI(uiRoot.transform, uiAssets, previewStage, maleRT, femaleRT, out InventoryManager inventoryManager);
            CreateEventSystem(uiRoot.transform);

            cutsceneManager.uiManager = uiManager;
            lampController.uiManager = uiManager;
            playerController.virtualJoystick = uiManager.virtualJoystick;
            playerController.lampController = lampController;
            playerController.uiManager = uiManager;

            // 8. IntroManager facade & MansionThreatManager (Bandit Patrol + Cursed Drawers + Spirit Flame Clues)
            GameObject introObj = new GameObject("IntroManager");
            introObj.transform.SetParent(managersRoot.transform);
            IntroManager introManager = introObj.AddComponent<IntroManager>();
            introManager.cutsceneManager = cutsceneManager;

            GameObject threatObj = new GameObject("MansionThreatManager");
            threatObj.transform.SetParent(managersRoot.transform);
            MansionThreatManager threatManager = threatObj.AddComponent<MansionThreatManager>();
            threatManager.lampController = lampController;
            threatManager.cameraController = cameraController;
            threatManager.uiManager = uiManager;
            threatManager.banditWarningVignetteGroup = uiManager.banditWarningVignetteGroup;
            threatManager.banditWarningText = uiManager.banditWarningText;
            threatManager.patrolInterval = 35f;
            threatManager.warningDuration = 5f;
            threatManager.banditSpottedOilPenalty = 15f;
            threatManager.cursedDrawersPerFloor = 2;

            // 9. GameManager
            GameObject gmObj = new GameObject("GameManager");
            gmObj.transform.SetParent(managersRoot.transform);
            GameManager gameManager = gmObj.AddComponent<GameManager>();
            gameManager.playerController = playerController;
            gameManager.lampController = lampController;
            gameManager.introManager = introManager;
            gameManager.uiManager = uiManager;
            gameManager.cameraController = cameraController;
            gameManager.inventoryManager = inventoryManager;
            gameManager.threatManager = threatManager;
            gameManager.floors = new[] { floor1, floor2, floor3 };

            // Initial backdrop pose on Floor 1's 4-poster bed
            if (floor1.introBedLyingPoint != null)
            {
                playerController.transform.position = floor1.introBedLyingPoint.position;
                playerController.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Last Light] Built POLYGON Horror Mansion edition at '{ScenePath}'. Press Play!");
            EditorUtility.DisplayDialog(
                "Last Light — Horror Mansion Built!",
                "Rebuilt 'Assets/Scenes/LastLight.unity' with:\n" +
                "• 4-Minute Linear Oil Drain (100% -> 0% in 240s, 100% max cap) & 2.5s Auto-Closing Drawers\n" +
                "• Blue Spirit Flame Toggle (cyan 4.0f radius, 1.5x drain — reveals hidden cyan puzzle clues & red cursed marks)\n" +
                "• Bandit Patrol every 35s ('EXTINGUISH LAMP!' 5s hide-in-dark event) + Quick LAMP ON/OFF toggle\n" +
                "• 2 Random Cursed Drawers per floor (leaping spider — tap CLOSE before auto-close or lose 10% oil!)\n" +
                "• Horror Atmosphere (Scurrying Rats + Squeaks, Cobwebs, Red-Eyed Spiders, Chandeliers, Paintings)\n" +
                "• Broken Wood Plank OPEN/CLOSE Button & Crumpled Paper Notes UI\n\n" +
                "Press PLAY to test or click GameJam > Build Android APK.",
                "Play!"
            );
        }

        [MenuItem("GameJam/Build Android APK")]
        public static void BuildAndroidApk()
        {
            if (!File.Exists(ScenePath)) BuildLastLightScene();
            else RegisterSceneInBuildSettings(ScenePath);

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                string msg = "Android Build Support is not installed for this Unity Editor version. Install it via Unity Hub, then click GameJam > Build Android APK again.";
                Debug.LogError("[Last Light] " + msg);
                EditorUtility.DisplayDialog("Android Module Missing", msg, "OK");
                return;
            }

            PlayerSettings.companyName = "LastLightStudio";
            PlayerSettings.productName = "Last Light";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.lastlight.game");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            EditorUserBuildSettings.buildAppBundle = false;

            string buildsDir = Path.GetDirectoryName(ApkOutputPath);
            if (!string.IsNullOrEmpty(buildsDir) && !Directory.Exists(buildsDir)) Directory.CreateDirectory(buildsDir);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkOutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                double mb = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log($"[Last Light] Android APK built successfully: {ApkOutputPath} ({mb:0.2f} MB)");
                EditorUtility.DisplayDialog("APK Build Succeeded", $"Exported Android APK to:\n{ApkOutputPath}\nSize: {mb:0.2f} MB", "OK");
            }
            else
            {
                Debug.LogError($"[Last Light] Android APK build failed: {summary.result} (Errors: {summary.totalErrors})");
                EditorUtility.DisplayDialog("APK Build Failed", $"Build finished with status: {summary.result}.\nCheck the Console for details.", "OK");
            }
        }

        // =========================================================================
        // STRICT PURE-BLACK LIGHTING & FIRST-PERSON DEFAULT CAMERA
        // =========================================================================

        private static void ConfigurePureBlackLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.fog = false;
        }

        private static CameraController CreateMainCamera(Transform playerTarget, PlayerCharacterVisual charVisual, MaterialPalette mats, LowPolyMeshes meshes, out LanternProp fpLanternProp)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";

            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.10f;
            cam.farClipPlane = 65f;

            camObj.AddComponent<AudioListener>();

            // Detailed Antique Brass Oil Lantern held in lower-right corner for First-Person default view
            GameObject fpLanternObj = BuildAntiqueLantern(camObj.transform, "FirstPersonAntiqueLantern", new Vector3(0.35f, -0.30f, 0.56f), 0.72f, mats, meshes, out fpLanternProp);
            fpLanternObj.SetActive(false);

            CameraController camCtrl = camObj.AddComponent<CameraController>();
            camCtrl.target = playerTarget;
            camCtrl.characterVisual = charVisual;
            camCtrl.firstPersonLanternRoot = fpLanternObj.transform;
            camCtrl.shoulderOffset = new Vector3(0.58f, 1.62f, -2.15f);
            camCtrl.pivotHeight = 1.52f;
            camCtrl.eyeHeight = 1.58f;
            camCtrl.collisionRadius = 0.24f;

            return camCtrl;
        }

        // =========================================================================
        // STYLIZED LOW-POLY CHARACTERS (NO BLOCKY CUBES) & ANTIQUE LANTERN
        // =========================================================================

        private static PlayerController CreatePlayer(Transform parent, MaterialPalette mats, LowPolyMeshes meshes,
            out LampController lampController, out PlayerCharacterVisual charVisual)
        {
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.SetParent(parent);
            playerObj.tag = "Player";
            playerObj.transform.position = Vector3.zero;

            CapsuleCollider col = playerObj.AddComponent<CapsuleCollider>();
            col.radius = 0.40f;
            col.height = 1.80f;
            col.center = new Vector3(0f, 0.90f, 0f);
            col.sharedMaterial = CreateZeroFrictionPhysicsMaterial();

            Rigidbody rb = playerObj.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

            charVisual = playerObj.AddComponent<PlayerCharacterVisual>();
            charVisual.maleRig = BuildStylizedHumanoid(playerObj.transform, "MaleModel", isFemale: false, mats, meshes, out LanternProp maleLantern);
            charVisual.femaleRig = BuildStylizedHumanoid(playerObj.transform, "FemaleModel", isFemale: true, mats, meshes, out LanternProp femaleLantern);
            charVisual.SetGender(CharacterGender.Male);

            // Warm yellow flickering handheld Lantern Point Light (THE primary mansion light source)
            GameObject lightObj = new GameObject("PlayerLanternPointLight");
            lightObj.transform.SetParent(playerObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0.26f, 1.38f, 0.45f);
            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.76f, 0.32f, 1f);
            pointLight.range = 6.5f;
            pointLight.intensity = 18.5f;
            pointLight.shadows = LightShadows.Soft;
            pointLight.renderMode = LightRenderMode.ForcePixel;

            lampController = playerObj.AddComponent<LampController>();
            lampController.lampLight = pointLight;
            lampController.thirdPersonMaleLantern = maleLantern;
            lampController.thirdPersonFemaleLantern = femaleLantern;
            lampController.startingOil = 75f;
            lampController.maxOil = 100f;
            lampController.drainRate = 100f / 240f; // Linear 4-minute full drain (240 seconds)
            lampController.spiritDrainMultiplier = 1.5f;
            lampController.oilPickupRestore = 20f;
            lampController.maxLightRange = 6.5f;
            lampController.spiritMaxLightRange = 4.0f;
            lampController.minLightRange = 1.5f;

            PlayerController controller = playerObj.AddComponent<PlayerController>();
            controller.standingHeightY = 0.0f;
            controller.moveSpeed = 4.9f;
            controller.interactRange = 2.85f;
            controller.lampController = lampController;
            controller.characterVisual = charVisual;

            return controller;
        }

        private static PlayerCharacterVisual.HumanoidRig BuildStylizedHumanoid(Transform parent, string rigName, bool isFemale,
            MaterialPalette mats, LowPolyMeshes meshes, out LanternProp heldLanternProp)
        {
            heldLanternProp = null;
            GameObject root = new GameObject(rigName);
            root.transform.SetParent(parent, false);

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            // Auto-Model Loader: checks Assets/Models/ and Assets/ for imported Humanoid/Generic .fbx/.glb/.obj/.prefab
            string[] keys = isFemale ? new[] { "playerfemale", "female", "woman", "girl" } : new[] { "playermale", "male", "man", "boy" };
            GameObject imported = TryInstantiateImportedModel(keys, visualRoot.transform, allowAnyHumanoidFallback: true);
            if (imported != null)
            {
                Transform handSocket = CharacterBuilder.FindOrCreateRightHandSocket(imported);
                BuildAntiqueLantern(handSocket, "HeldAntiqueLantern", Vector3.zero, 0.90f, mats, meshes, out heldLanternProp);
                var importedRig = new PlayerCharacterVisual.HumanoidRig { root = root, visualRoot = visualRoot.transform };
                CharacterBuilder.BindImportedAnimatorBones(imported, importedRig);
                return importedRig;
            }

            Material outfitMat = isFemale ? mats.femaleTunic : mats.maleCoat;
            Material pantsMat = isFemale ? mats.femalePants : mats.malePants;
            Material hairMat = isFemale ? mats.femaleHair : mats.maleHair;

            // Articulated Faceted Legs & Boots (NO blocky cubes!)
            float hipX = isFemale ? 0.11f : 0.13f;
            Transform leftLeg = CreateLimbPivot(visualRoot.transform, "LeftLegPivot", new Vector3(-hipX, 0.80f, 0f));
            CreateMeshChild("Thigh", meshes.limb, leftLeg, new Vector3(0f, -0.22f, 0f), new Vector3(0.95f, 0.95f, 0.95f), pantsMat);
            CreateMeshChild("Shin", meshes.limb, leftLeg, new Vector3(0f, -0.56f, 0f), new Vector3(0.80f, 0.90f, 0.80f), pantsMat);
            CreateMeshChild("Boot", meshes.boot, leftLeg, new Vector3(0f, -0.74f, 0.04f), Vector3.one, mats.darkWood);

            Transform rightLeg = CreateLimbPivot(visualRoot.transform, "RightLegPivot", new Vector3(hipX, 0.80f, 0f));
            CreateMeshChild("Thigh", meshes.limb, rightLeg, new Vector3(0f, -0.22f, 0f), new Vector3(0.95f, 0.95f, 0.95f), pantsMat);
            CreateMeshChild("Shin", meshes.limb, rightLeg, new Vector3(0f, -0.56f, 0f), new Vector3(0.80f, 0.90f, 0.80f), pantsMat);
            CreateMeshChild("Boot", meshes.boot, rightLeg, new Vector3(0f, -0.74f, 0.04f), Vector3.one, mats.darkWood);

            // Stylized Tapered Faceted Torso + Lapels / Cloak Skirt
            if (!isFemale)
            {
                CreateMeshChild("TorsoCoat", meshes.torsoMale, visualRoot.transform, new Vector3(0f, 1.12f, 0f), Vector3.one, outfitMat);
                CreateMeshChild("Belt", meshes.lanternBase, visualRoot.transform, new Vector3(0f, 0.84f, 0f), new Vector3(1.9f, 1.4f, 1.3f), mats.darkWood);
                CreateMeshChild("LapelLeft", meshes.limb, visualRoot.transform, new Vector3(-0.09f, 1.24f, 0.12f), new Vector3(0.42f, 0.65f, 0.25f), mats.mahoganyWood);
                CreateMeshChild("LapelRight", meshes.limb, visualRoot.transform, new Vector3(0.09f, 1.24f, 0.12f), new Vector3(0.42f, 0.65f, 0.25f), mats.mahoganyWood);
            }
            else
            {
                CreateMeshChild("TorsoTunic", meshes.torsoFemale, visualRoot.transform, new Vector3(0f, 1.15f, 0f), Vector3.one, outfitMat);
                CreateMeshChild("CloakSkirt", meshes.skirtCloak, visualRoot.transform, new Vector3(0f, 0.76f, 0f), Vector3.one, outfitMat);
                CreateMeshChild("GoldSash", meshes.lanternBase, visualRoot.transform, new Vector3(0f, 0.94f, 0f), new Vector3(1.55f, 1.1f, 1.25f), mats.brassMetal);
            }

            // Faceted Head & Stylized Hair
            CreateMeshChild("Neck", meshes.limb, visualRoot.transform, new Vector3(0f, 1.44f, 0f), new Vector3(0.55f, 0.28f, 0.55f), mats.skin);
            CreateMeshChild("Head", meshes.head, visualRoot.transform, new Vector3(0f, 1.59f, 0f), Vector3.one, mats.skin);

            if (!isFemale)
            {
                CreateMeshChild("HairCap", meshes.hairCap, visualRoot.transform, new Vector3(0f, 1.66f, -0.02f), Vector3.one, hairMat);
            }
            else
            {
                CreateMeshChild("HairCrown", meshes.hairCap, visualRoot.transform, new Vector3(0f, 1.66f, -0.03f), new Vector3(1.06f, 1.12f, 1.06f), hairMat);
                CreateMeshChild("Ponytail", meshes.hairPonytail, visualRoot.transform, new Vector3(0f, 1.47f, -0.16f), Vector3.one, hairMat);
            }

            // Articulated Arms
            float shoulderX = isFemale ? 0.23f : 0.28f;
            Transform leftArm = CreateLimbPivot(visualRoot.transform, "LeftArmPivot", new Vector3(-shoulderX, 1.34f, 0f));
            CreateMeshChild("ArmMesh", meshes.limb, leftArm, new Vector3(0f, -0.26f, 0f), new Vector3(0.72f, 1.18f, 0.72f), outfitMat);
            CreateMeshChild("Hand", meshes.head, leftArm, new Vector3(0f, -0.54f, 0f), new Vector3(0.36f, 0.34f, 0.36f), mats.skin);

            Transform rightArm = CreateLimbPivot(visualRoot.transform, "RightArmPivot", new Vector3(shoulderX, 1.34f, 0f));
            rightArm.localRotation = Quaternion.Euler(-36f, 0f, 0f);
            CreateMeshChild("ArmMesh", meshes.limb, rightArm, new Vector3(0f, -0.26f, 0f), new Vector3(0.72f, 1.18f, 0.72f), outfitMat);
            CreateMeshChild("Hand", meshes.head, rightArm, new Vector3(0f, -0.54f, 0f), new Vector3(0.36f, 0.34f, 0.36f), mats.skin);

            // Vintage Antique Brass Oil Lantern held in right hand
            GameObject lantern = BuildAntiqueLantern(rightArm, "HeldAntiqueLantern", new Vector3(0f, -0.66f, 0.12f), 0.92f, mats, meshes, out heldLanternProp);
            lantern.transform.localRotation = Quaternion.Euler(36f, 0f, 0f);

            return new PlayerCharacterVisual.HumanoidRig
            {
                root = root,
                visualRoot = visualRoot.transform,
                leftArm = leftArm,
                rightArm = rightArm,
                leftLeg = leftLeg,
                rightLeg = rightLeg
            };
        }

        private static GameObject BuildMaskedBandit(Transform parent, string name, Vector3 localPos, MaterialPalette mats, LowPolyMeshes meshes)
        {
            GameObject bandit = new GameObject(name);
            bandit.transform.SetParent(parent, false);
            bandit.transform.localPosition = localPos;

            if (TryInstantiateImportedModel(new[] { "bandit", "daku", "enemy", "thief" }, bandit.transform, allowAnyHumanoidFallback: false) != null)
            {
                return bandit;
            }

            // Stylized Masked Bandit ("Daku") with turban, face bandana, dark vest & club
            CreateMeshChild("LeftLeg", meshes.limb, bandit.transform, new Vector3(-0.13f, 0.42f, 0f), new Vector3(0.95f, 1.85f, 0.95f), mats.malePants);
            CreateMeshChild("RightLeg", meshes.limb, bandit.transform, new Vector3(0.13f, 0.42f, 0f), new Vector3(0.95f, 1.85f, 0.95f), mats.malePants);
            CreateMeshChild("TorsoVest", meshes.torsoMale, bandit.transform, new Vector3(0f, 1.14f, 0f), new Vector3(1.06f, 1.04f, 1.04f), mats.banditCloth);
            CreateMeshChild("Head", meshes.head, bandit.transform, new Vector3(0f, 1.62f, 0f), Vector3.one, mats.skin);
            CreateMeshChild("FaceBandana", meshes.bandana, bandit.transform, new Vector3(0f, 1.54f, 0.04f), Vector3.one, mats.banditTurban);
            CreateMeshChild("Turban", meshes.turban, bandit.transform, new Vector3(0f, 1.73f, 0f), Vector3.one, mats.banditTurban);

            CreateMeshChild("LeftArm", meshes.limb, bandit.transform, new Vector3(-0.30f, 1.10f, 0.08f), new Vector3(0.75f, 1.2f, 0.75f), mats.banditCloth);

            GameObject rightArm = new GameObject("WeaponArm");
            rightArm.transform.SetParent(bandit.transform, false);
            rightArm.transform.localPosition = new Vector3(0.30f, 1.32f, 0f);
            rightArm.transform.localRotation = Quaternion.Euler(-58f, 0f, 0f);
            CreateMeshChild("ArmMesh", meshes.limb, rightArm.transform, new Vector3(0f, -0.26f, 0f), new Vector3(0.75f, 1.18f, 0.75f), mats.banditCloth);
            CreateMeshChild("WoodenClub", meshes.club, rightArm.transform, new Vector3(0f, -0.65f, 0.22f), Vector3.one, mats.crateWood).transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            return bandit;
        }

        private static GameObject BuildAntiqueLantern(Transform parent, string name, Vector3 localPos, float scale, MaterialPalette mats, LowPolyMeshes meshes, out LanternProp prop)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localScale = Vector3.one * scale;

            GameObject pivot = new GameObject("SwayPivot");
            pivot.transform.SetParent(root.transform, false);

            // Octagonal vintage brass base, tapered glass core, domed chimney cap, handle ring & 4 cage struts
            CreateMeshChild("OctaBase", meshes.lanternBase, pivot.transform, new Vector3(0f, -0.14f, 0f), Vector3.one, mats.brassMetal);
            GameObject glassObj = CreateMeshChild("OctaGlassCore", meshes.lanternGlass, pivot.transform, new Vector3(0f, 0.0f, 0f), Vector3.one, mats.lampGlass);
            CreateMeshChild("ChimneyCap", meshes.lanternCap, pivot.transform, new Vector3(0f, 0.16f, 0f), Vector3.one, mats.brassMetal);
            CreateMeshChild("HandleRing", meshes.lanternBase, pivot.transform, new Vector3(0f, 0.26f, 0f), new Vector3(0.32f, 1.2f, 0.32f), mats.ironMetal);

            for (int r = 0; r < 4; r++)
            {
                float ang = (r * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 rp = new Vector3(Mathf.Cos(ang) * 0.105f, 0.0f, Mathf.Sin(ang) * 0.105f);
                CreateMeshChild($"CageStrut_{r}", meshes.limb, pivot.transform, rp, new Vector3(0.14f, 0.56f, 0.14f), mats.ironMetal);
            }

            prop = root.AddComponent<LanternProp>();
            prop.swayPivot = pivot.transform;
            prop.glassCoreRenderer = glassObj.GetComponent<Renderer>();
            return root;
        }

        private static GameObject CreateNightstandIntroLantern(Transform parent, Vector3 localPos, MaterialPalette mats, LowPolyMeshes meshes)
        {
            GameObject lamp = BuildAntiqueLantern(parent, "NightstandAntiqueLantern", localPos, 1.05f, mats, meshes, out _);

            // Temporary warm yellow light active ONLY during Act 2 Bed Wake-Up (disabled immediately on pickup!)
            GameObject lightObj = new GameObject("IntroBedsideLampLight");
            lightObj.transform.SetParent(lamp.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.76f, 0.30f, 1f);
            light.range = 5.5f;
            light.intensity = 14f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;

            GroundLampFlicker flicker = lamp.AddComponent<GroundLampFlicker>();
            flicker.lampLight = light;
            flicker.baseIntensity = 14f;
            flicker.baseRange = 5.5f;

            return lamp;
        }

        // =========================================================================
        // ACT 1 — FOREST CAMPFIRE SETPIECE & CUTSCENE MANAGER
        // =========================================================================

        private static CutsceneManager BuildForestCampfireAndCutsceneManager(
            Transform managersParent, Transform worldParent, MaterialPalette mats, LowPolyMeshes meshes,
            PlayerController playerController, LampController lampController, CameraController cameraController)
        {
            GameObject forestRoot = new GameObject("Act1_JungleCampfireSetpiece");
            forestRoot.transform.SetParent(worldParent);
            forestRoot.transform.position = new Vector3(-70f, 0f, 0f);

            // Forest floor clearing
            CreatePrimitiveChild("ForestGround", PrimitiveType.Cube, forestRoot.transform,
                new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.5f, 30f), mats.forestGrass, false);

            // Ring of stylized low-poly pine trees (stacked faceted cones!)
            float[] angles = { 0f, 25f, 52f, 78f, 105f, 132f, 160f, 188f, 214f, 242f, 270f, 298f, 326f };
            for (int i = 0; i < angles.Length; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                float dist = 7.2f + (i % 3) * 1.9f;
                Vector3 tp = new Vector3(Mathf.Cos(rad) * dist, 0f, Mathf.Sin(rad) * dist);
                CreateLowPolyPineTree(forestRoot.transform, $"PineTree_{i + 1}", tp, 0.9f + (i % 3) * 0.22f, mats, meshes);
            }

            // Low-poly Canvas Tent
            GameObject tent = CreateMeshChild("ForestTent", meshes.tent, forestRoot.transform, new Vector3(-3.4f, 0f, -2.8f), Vector3.one, mats.tentCanvas);
            tent.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);

            // Sitting Log beside the fire
            GameObject log = CreateMeshChild("SittingLog", meshes.treeTrunk, forestRoot.transform, new Vector3(0f, 0.26f, -2.0f), new Vector3(0.95f, 0.85f, 0.95f), mats.darkWood);
            log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Campfire Pit (faceted rocks + crossed firewood + flame cones + flickering campfire Point Light)
            Transform fireCenter = CreateMarker(forestRoot.transform, "CampfireCenter", new Vector3(0f, 0.25f, 0f));
            for (int r = 0; r < 8; r++)
            {
                float a = (r / 8f) * Mathf.PI * 2f;
                CreateMeshChild($"FireRock_{r}", meshes.rock, fireCenter, new Vector3(Mathf.Cos(a) * 0.65f, -0.10f, Mathf.Sin(a) * 0.65f), Vector3.one, mats.wall);
            }
            CreateMeshChild("FlameInner", meshes.flameCone, fireCenter, new Vector3(0f, 0.22f, 0f), new Vector3(0.85f, 1.15f, 0.85f), mats.campfireFlame);
            CreateMeshChild("FlameSide", meshes.flameCone, fireCenter, new Vector3(0.14f, 0.14f, -0.08f), new Vector3(0.55f, 0.80f, 0.55f), mats.campfireFlame);

            GameObject fireLightObj = new GameObject("CampfireLight");
            fireLightObj.transform.SetParent(fireCenter, false);
            fireLightObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            Light fireLight = fireLightObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.55f, 0.16f, 1f);
            fireLight.range = 13f;
            fireLight.intensity = 28f;
            fireLight.shadows = LightShadows.None;
            GroundLampFlicker fireFlicker = fireLightObj.AddComponent<GroundLampFlicker>();
            fireFlicker.lampLight = fireLight;
            fireFlicker.baseIntensity = 28f;
            fireFlicker.baseRange = 13f;

            // 2 Armed Masked Bandits ("Daku")
            Transform b1Start = CreateMarker(forestRoot.transform, "Bandit1Start", new Vector3(-6.2f, 0f, 4.4f));
            Transform b2Start = CreateMarker(forestRoot.transform, "Bandit2Start", new Vector3(6.2f, 0f, 4.2f));
            Transform b1End = CreateMarker(forestRoot.transform, "Bandit1Surround", new Vector3(-1.25f, 0f, -1.25f));
            Transform b2End = CreateMarker(forestRoot.transform, "Bandit2Surround", new Vector3(1.25f, 0f, -1.25f));

            GameObject bandit1 = BuildMaskedBandit(forestRoot.transform, "Bandit_Daku_1", b1Start.localPosition, mats, meshes);
            GameObject bandit2 = BuildMaskedBandit(forestRoot.transform, "Bandit_Daku_2", b2Start.localPosition, mats, meshes);

            Transform playerSit = CreateMarker(forestRoot.transform, "ForestPlayerSitPoint", new Vector3(0f, 0f, -1.95f));
            Transform camStart = CreateMarker(forestRoot.transform, "Act1CamStart", new Vector3(3.6f, 2.3f, -4.4f));
            Transform camEnd = CreateMarker(forestRoot.transform, "Act1CamEnd", new Vector3(-2.6f, 1.85f, -3.6f));

            forestRoot.SetActive(false); // Disabled outside Act 1 so zero lights ever leak into the mansion!

            GameObject csObj = new GameObject("CutsceneManager");
            csObj.transform.SetParent(managersParent);
            CutsceneManager cm = csObj.AddComponent<CutsceneManager>();
            cm.playerController = playerController;
            cm.lampController = lampController;
            cm.cameraController = cameraController;
            cm.forestSceneRoot = forestRoot;
            cm.forestPlayerSitPoint = playerSit;
            cm.campfireCenter = fireCenter;
            cm.bandit1Transform = bandit1.transform;
            cm.bandit2Transform = bandit2.transform;
            cm.bandit1StartPoint = b1Start;
            cm.bandit2StartPoint = b2Start;
            cm.bandit1SurroundPoint = b1End;
            cm.bandit2SurroundPoint = b2End;
            cm.act1CamStartPoint = camStart;
            cm.act1CamEndPoint = camEnd;

            return cm;
        }

        private static void CreateLowPolyPineTree(Transform parent, string name, Vector3 localPos, float scale, MaterialPalette mats, LowPolyMeshes meshes)
        {
            GameObject tree = new GameObject(name);
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = localPos;
            tree.transform.localScale = Vector3.one * scale;

            CreateMeshChild("Trunk", meshes.treeTrunk, tree.transform, new Vector3(0f, 1.0f, 0f), Vector3.one, mats.darkWood);
            CreateMeshChild("Tier1", meshes.pineCone, tree.transform, new Vector3(0f, 2.4f, 0f), new Vector3(1.15f, 1.0f, 1.15f), mats.pineLeaf);
            CreateMeshChild("Tier2", meshes.pineCone, tree.transform, new Vector3(0f, 3.6f, 0f), new Vector3(0.88f, 0.90f, 0.88f), mats.pineLeaf);
            CreateMeshChild("Tier3", meshes.pineCone, tree.transform, new Vector3(0f, 4.6f, 0f), new Vector3(0.58f, 0.78f, 0.58f), mats.pineLeaf);
        }

        // =========================================================================
        // LIVE 3D ROTATING CHARACTER PREVIEW STAGE (DARK CHARACTER SELECT)
        // =========================================================================

        private static CharacterPreviewStage BuildCharacterPreviewStage(
            Transform worldParent, MaterialPalette mats, LowPolyMeshes meshes,
            out RenderTexture maleRT, out RenderTexture femaleRT)
        {
            maleRT = CreatePreviewRenderTexture("RT_PreviewMale.renderTexture");
            femaleRT = CreatePreviewRenderTexture("RT_PreviewFemale.renderTexture");

            GameObject stageRoot = new GameObject("CharacterSelect_3DPreviewStage");
            stageRoot.transform.SetParent(worldParent);
            stageRoot.transform.position = new Vector3(-140f, 0f, 0f);

            // Male Preview Pod
            Transform malePivot = CreateMarker(stageRoot.transform, "MalePreviewPivot", new Vector3(-5f, 0f, 0f));
            BuildStylizedHumanoid(malePivot, "PreviewMale", false, mats, meshes, out _);
            CreatePreviewCamera(stageRoot.transform, "MalePreviewCam", new Vector3(-5f, 1.15f, 2.55f), new Vector3(-5f, 1.02f, 0f), maleRT);
            CreatePreviewLight(stageRoot.transform, "MalePreviewKeyLight", new Vector3(-3.8f, 2.2f, 2.2f));

            // Female Preview Pod
            Transform femalePivot = CreateMarker(stageRoot.transform, "FemalePreviewPivot", new Vector3(5f, 0f, 0f));
            BuildStylizedHumanoid(femalePivot, "PreviewFemale", true, mats, meshes, out _);
            CreatePreviewCamera(stageRoot.transform, "FemalePreviewCam", new Vector3(5f, 1.15f, 2.55f), new Vector3(5f, 1.02f, 0f), femaleRT);
            CreatePreviewLight(stageRoot.transform, "FemalePreviewKeyLight", new Vector3(6.2f, 2.2f, 2.2f));

            CharacterPreviewStage stage = stageRoot.AddComponent<CharacterPreviewStage>();
            stage.stageRoot = stageRoot;
            stage.malePreviewPivot = malePivot;
            stage.femalePreviewPivot = femalePivot;
            stageRoot.SetActive(false); // Active ONLY during Character Select!

            return stage;
        }

        private static RenderTexture CreatePreviewRenderTexture(string fileName)
        {
            string path = $"{SpritesDir}/{fileName}";
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (rt == null)
            {
                rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                AssetDatabase.CreateAsset(rt, path);
            }
            return rt;
        }

        private static void CreatePreviewCamera(Transform parent, string name, Vector3 localPos, Vector3 lookTargetLocal, RenderTexture targetRT)
        {
            GameObject camObj = new GameObject(name);
            camObj.transform.SetParent(parent, false);
            camObj.transform.localPosition = localPos;
            camObj.transform.LookAt(parent.TransformPoint(lookTargetLocal));

            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 15f;
            cam.targetTexture = targetRT;
        }

        private static void CreatePreviewLight(Transform parent, string name, Vector3 localPos)
        {
            GameObject lObj = new GameObject(name);
            lObj.transform.SetParent(parent, false);
            lObj.transform.localPosition = localPos;
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.85f, 0.58f, 1f);
            l.range = 8f;
            l.intensity = 16f;
            l.shadows = LightShadows.None;
        }

        // =========================================================================
        // 3 DISTINCT MANSION FLOORS (STRICT LIGHTING + RANDOMIZED PUZZLES)
        // =========================================================================

        private static FloorController BuildFloor1(Transform parent, Vector3 origin, MaterialPalette mats, LowPolyMeshes meshes, LampController playerLamp, Transform playerTf)
        {
            GameObject root = new GameObject("Floor_1_TheForgottenChamber");
            root.transform.SetParent(parent);
            root.transform.position = origin;

            FloorController fc = root.AddComponent<FloorController>();
            fc.floorNumber = 1;
            fc.floorSubtitle = "The Forgotten Chamber";
            fc.puzzleMode = FloorPuzzleMode.TileSortChestForKey;
            fc.requiredPuzzlePieces = 4;
            fc.totalClueNotes = 1;

            BuildRoomShellWithNorthDoorway(root.transform, 22f, 22f, 3.6f, mats.floor1, mats.wall);

            Transform arch = new GameObject("HaveliArchitecture").transform;
            arch.SetParent(root.transform, false);

            // Partitioned Bedroom (SW) & East Study with Interior Doorways
            CreateWall(arch, "BedroomNorthWall", new Vector3(-7.0f, 1.8f, -2.5f), new Vector3(7.6f, 3.6f, 0.45f), mats.wall);
            CreateWall(arch, "BedroomEastWall_South", new Vector3(-3.2f, 1.8f, -8.8f), new Vector3(0.45f, 3.6f, 4.4f), mats.wall);
            CreateWall(arch, "BedroomDoorLintel", new Vector3(-3.2f, 3.1f, -4.6f), new Vector3(0.50f, 1.0f, 4.0f), mats.doorFrame);

            CreateWall(arch, "StudyWestWall_South", new Vector3(3.6f, 1.8f, -6.5f), new Vector3(0.45f, 3.6f, 5.2f), mats.wall);
            CreateWall(arch, "StudyWestWall_North", new Vector3(3.6f, 1.8f, 3.2f), new Vector3(0.45f, 3.6f, 6.2f), mats.wall);
            CreateWall(arch, "StudyDoorLintel", new Vector3(3.6f, 3.1f, -1.9f), new Vector3(0.50f, 1.0f, 4.0f), mats.doorFrame);

            Transform decor = new GameObject("HaveliFurniture").transform;
            decor.SetParent(root.transform, false);

            // Carved 4-Poster Wooden Bed + Bedside Table with Antique Brass Oil Lantern for Act 2 Wake-Up
            CreateFourPosterBed(decor, "FourPosterBed_Intro", new Vector3(-8.6f, 0f, -8.2f), 0f, mats, meshes);
            CreateStaticNightstand(decor, "BedsideTable_IntroLamp", new Vector3(-6.4f, 0f, -9.2f), mats);
            fc.introBedLyingPoint = CreateMarker(root.transform, "IntroBedLyingPoint", new Vector3(-8.6f, 0.78f, -8.2f));
            fc.introBedStepOffPoint = CreateMarker(root.transform, "IntroBedStepOffPoint", new Vector3(-6.4f, 0f, -7.4f));
            fc.playerSpawnPoint = CreateMarker(root.transform, "PlayerSpawn", new Vector3(-5.6f, 0f, -6.6f));
            fc.nightstandLampObject = CreateNightstandIntroLantern(root.transform, new Vector3(-6.4f, 0.94f, -9.2f), mats, meshes);

            CreateRug(decor, "BedroomRug", new Vector3(-6.4f, 0.015f, -6.4f), new Vector2(3.8f, 3.2f), mats.rugCrimson, mats.darkWood);
            CreateRug(decor, "HallRug", new Vector3(0f, 0.015f, 2.2f), new Vector2(4.2f, 5.5f), mats.rugSlate, mats.darkWood);
            CreateBookshelf(decor, "Bookshelf_1", new Vector3(-10.1f, 0f, 4.2f), 90f, mats);
            CreateBookshelf(decor, "Bookshelf_2", new Vector3(10.1f, 0f, -1.0f), -90f, mats);
            CreateTable(decor, "StudyTable", new Vector3(7.0f, 0f, -4.2f), mats);
            CreateChair(decor, "StudyChair_1", new Vector3(7.0f, 0f, -5.5f), 0f, mats);
            CreateBrokenChair(decor, "BrokenChair_Study", new Vector3(6.8f, 0f, -2.8f), 145f, mats);
            // Varied Framed Oil Paintings / Portraits
            CreateFramedWallArt(decor, "Painting_Portrait", new Vector3(-10.55f, 1.95f, -6.8f), 90f, new Vector2(1.15f, 1.48f), mats.paintingPortrait, mats.darkWood);
            CreateFramedWallArt(decor, "Painting_Mansion", new Vector3(10.55f, 1.95f, 4.2f), -90f, new Vector2(1.65f, 1.10f), mats.paintingMansion, mats.mahoganyWood);
            CreateFramedWallArt(decor, "Painting_Mirror", new Vector3(-3.45f, 1.95f, -8.5f), -90f, new Vector2(1.05f, 1.40f), mats.paintingMirror, mats.darkWood);

            // Ceiling Timber Beams, Wall Wainscoting, Multi-Tiered Wrought-Iron Chandeliers, Cobwebs, Spiders & Rats
            CreateCeilingBeamsAndWainscoting(decor, 22f, 22f, 3.6f, mats.darkWood, mats.mahoganyWood);
            CreateWroughtIronChandelier(decor, "Chandelier_CenterHall", new Vector3(0f, 3.55f, 1.5f), mats, meshes, withSpider: true);
            CreateWroughtIronChandelier(decor, "Chandelier_Study", new Vector3(7.2f, 3.55f, -4.2f), mats, meshes, withSpider: false);
            PopulateHorrorAtmosphere(root.transform, 22f, 22f, 3.6f, mats, meshes, playerTf,
                new[]
                {
                    new[] { new Vector3(-1.8f, 0.08f, -8.5f), new Vector3(2.2f, 0.08f, -2.5f), new Vector3(-2.0f, 0.08f, 3.2f), new Vector3(2.0f, 0.08f, 8.2f) },
                    new[] { new Vector3(-8.5f, 0.08f, 1.8f), new Vector3(-5.0f, 0.08f, 7.2f), new Vector3(-8.2f, 0.08f, 6.2f) },
                    new[] { new Vector3(5.2f, 0.08f, -7.8f), new Vector3(8.5f, 0.08f, 1.2f), new Vector3(5.5f, 0.08f, 7.6f) }
                });

            // Floor 1 Puzzle: Tile-Sort Treasure Chest -> Exit Key
            fc.treasureChest = CreateTreasureChest(root.transform, "TreasureChest_Floor1", new Vector3(0f, 0f, 5.0f),
                ChestRewardKind.FloorExitKey, "HAVELI TREASURE CHEST — SORT 1 → 4", "", "", mats, fc);

            // 9 Openable & Closable Diverse Furniture Pieces (4 Puzzle Pieces + 4 Oil Flasks +20% + 1 Journal Note)
            List<InteractableContainer> containers = new List<InteractableContainer>
            {
                CreateContainer(root.transform, "Wardrobe_Piece1", new Vector3(-9.7f, 0f, -4.4f), 90f,
                    ContainerStyle.TallWardrobe, ContainerRewardType.PuzzlePiece, 1, 20f, "", "", mats, fc),
                CreateContainer(root.transform, "Nightstand_Oil1", new Vector3(-0.2f, 0f, -9.8f), 0f,
                    ContainerStyle.BedsideNightstand, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc),
                CreateContainer(root.transform, "WritingDesk_Note1", new Vector3(9.4f, 0f, -8.4f), -90f,
                    ContainerStyle.WritingDesk, ContainerRewardType.StoryNote, 0, 0f,
                    "HAVELI KEEPER'S DIARY — FLOOR I",
                    "The bandits drag travelers into this Forgotten Haveli, where only an antique oil lamp stands against the pitch dark.\n\n" +
                    "Four carved Puzzle Pieces are hidden inside the wardrobes, apothecary chests, and desks of this floor. " +
                    "Bring all 4 pieces to the locked Treasure Chest in the north hall to claim the Exit Key.\n\n" +
                    "Oil drains fast and caps at 100% — search every drawer carefully!",
                    mats, fc),
                CreateContainer(root.transform, "Apothecary_Piece2", new Vector3(-9.7f, 0f, 0.8f), 90f,
                    ContainerStyle.ApothecaryChest, ContainerRewardType.PuzzlePiece, 2, 20f, "", "", mats, fc),
                CreateContainer(root.transform, "WritingDesk_Oil2", new Vector3(9.4f, 0f, 2.8f), -90f,
                    ContainerStyle.WritingDesk, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc),
                CreateContainer(root.transform, "TallWardrobe_Piece3", new Vector3(-9.7f, 0f, 8.2f), 90f,
                    ContainerStyle.TallWardrobe, ContainerRewardType.PuzzlePiece, 3, 20f, "", "", mats, fc),
                CreateContainer(root.transform, "ThreeDrawer_Piece4", new Vector3(9.7f, 0f, 8.2f), -90f,
                    ContainerStyle.ThreeDrawerChest, ContainerRewardType.PuzzlePiece, 4, 20f, "", "", mats, fc),
                CreateContainer(root.transform, "Nightstand_Oil3", new Vector3(-4.8f, 0f, 9.8f), 180f,
                    ContainerStyle.BedsideNightstand, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc),
                CreateContainer(root.transform, "Apothecary_Oil4", new Vector3(4.8f, 0f, 9.8f), 180f,
                    ContainerStyle.ApothecaryChest, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc)
            };
            fc.containers = containers.ToArray();
            fc.dynamicClueContainers = new InteractableContainer[0];
            fc.spiritClueMarkers = BuildSpiritWallCluesForFloor(root.transform, fc.containers, mats);
            fc.pickups = new PickupItem[0];

            fc.exitDoor = CreateExitDoorWithStaircase(root.transform, "ExitDoor_Floor1", new Vector3(0f, 0f, 11.0f),
                DoorLockType.KeyLocked, "", "FLOOR 1 EXIT", "", mats, fc, out StaircaseTrigger stairTrig);
            fc.staircaseTrigger = stairTrig;

            fc.disorientationManager = SetupDisorientation(root, mats, meshes, playerLamp, playerTf,
                new[] { new Vector3(-5.8f, 0f, 2.2f), new Vector3(6.6f, 0f, 6.2f), new Vector3(0.4f, 0f, -3.6f), new Vector3(-6.2f, 0f, 6.4f), new Vector3(6.8f, 0f, -8.2f) },
                new[] { new Vector3(-5.8f, 0f, 2.2f), new Vector3(6.6f, 0f, 6.2f), new Vector3(0.4f, 0f, -3.6f), new Vector3(-6.2f, 0f, 6.4f), new Vector3(6.8f, 0f, -8.2f), new Vector3(-1.4f, 0f, 0.2f), new Vector3(1.4f, 0f, -6.2f), new Vector3(-5.4f, 0f, -0.4f), new Vector3(6.6f, 0f, 0.6f), new Vector3(5.2f, 0f, 8.6f) });

            return fc;
        }

        private static FloorController BuildFloor2(Transform parent, Vector3 origin, MaterialPalette mats, LowPolyMeshes meshes, LampController playerLamp, Transform playerTf)
        {
            GameObject root = new GameObject("Floor_2_TheShiftingHall");
            root.transform.SetParent(parent);
            root.transform.position = origin;

            FloorController fc = root.AddComponent<FloorController>();
            fc.floorNumber = 2;
            fc.floorSubtitle = "The Shifting Hall";
            fc.puzzleMode = FloorPuzzleMode.RandomizedMathKeypad;
            fc.requiredPuzzlePieces = 0;
            fc.totalClueNotes = 3;

            BuildRoomShellWithNorthDoorway(root.transform, 26f, 26f, 3.6f, mats.floor2, mats.wall);
            fc.playerSpawnPoint = CreateMarker(root.transform, "PlayerSpawn", new Vector3(0f, 0f, -10.5f));

            Transform arch = new GameObject("HaveliArchitecture").transform;
            arch.SetParent(root.transform, false);

            CreateWall(arch, "SouthFoyerBaffle", new Vector3(0f, 1.8f, -4.4f), new Vector3(7.2f, 3.6f, 0.45f), mats.wall);
            CreateWall(arch, "WestWingWall_South", new Vector3(-5.4f, 1.8f, -1.2f), new Vector3(0.45f, 3.6f, 6.4f), mats.wall);
            CreateWall(arch, "WestWingWall_North", new Vector3(-5.4f, 1.8f, 7.6f), new Vector3(0.45f, 3.6f, 4.4f), mats.wall);
            CreateWall(arch, "WestDoorLintel", new Vector3(-5.4f, 3.1f, 3.7f), new Vector3(0.50f, 1.0f, 3.6f), mats.doorFrame);

            CreateWall(arch, "EastWingWall_South", new Vector3(5.4f, 1.8f, -1.2f), new Vector3(0.45f, 3.6f, 6.4f), mats.wall);
            CreateWall(arch, "EastWingWall_North", new Vector3(5.4f, 1.8f, 7.6f), new Vector3(0.45f, 3.6f, 4.4f), mats.wall);
            CreateWall(arch, "EastDoorLintel", new Vector3(5.4f, 3.1f, 3.7f), new Vector3(0.50f, 1.0f, 3.6f), mats.doorFrame);
            CreateWall(arch, "CenterStudyWall", new Vector3(0f, 1.8f, 3.6f), new Vector3(5.8f, 3.6f, 0.45f), mats.wall);

            Transform decor = new GameObject("HaveliFurniture").transform;
            decor.SetParent(root.transform, false);

            CreateFourPosterBed(decor, "GuestFourPosterBed", new Vector3(-10.2f, 0f, 10.2f), 90f, mats, meshes);
            CreateBookshelf(decor, "LibraryShelf_1", new Vector3(-12.1f, 0f, -2.5f), 90f, mats);
            CreateBookshelf(decor, "LibraryShelf_2", new Vector3(12.1f, 0f, -2.5f), -90f, mats);
            CreateTable(decor, "HallTable", new Vector3(0f, 0f, -0.4f), mats);
            CreateChair(decor, "HallChair_L", new Vector3(-1.5f, 0f, -0.4f), 90f, mats);
            CreateBrokenChair(decor, "BrokenHallChair_R", new Vector3(1.6f, 0f, -0.5f), -75f, mats);
            CreateRug(decor, "FoyerRug", new Vector3(0f, 0.015f, -7.8f), new Vector2(4.4f, 4.0f), mats.rugCrimson, mats.darkWood);
            CreateRug(decor, "CenterRug", new Vector3(0f, 0.015f, 7.8f), new Vector2(4.8f, 4.2f), mats.rugSlate, mats.darkWood);
            CreateFramedWallArt(decor, "Painting_W", new Vector3(-12.55f, 1.95f, 4.8f), 90f, new Vector2(1.55f, 1.05f), mats.paintingMoonTree, mats.ebonyWood);
            CreateFramedWallArt(decor, "Painting_E", new Vector3(12.55f, 1.95f, 4.8f), -90f, new Vector2(1.15f, 1.50f), mats.paintingPortrait, mats.mahoganyWood);
            CreateFramedWallArt(decor, "Painting_Center", new Vector3(0f, 1.95f, 3.32f), 180f, new Vector2(1.60f, 1.12f), mats.paintingMansion, mats.darkWood);

            CreateCeilingBeamsAndWainscoting(decor, 26f, 26f, 3.6f, mats.ebonyWood, mats.darkWood);
            CreateWroughtIronChandelier(decor, "Chandelier_Foyer", new Vector3(0f, 3.55f, -7.8f), mats, meshes, withSpider: true);
            CreateWroughtIronChandelier(decor, "Chandelier_NorthHall", new Vector3(0f, 3.55f, 8.2f), mats, meshes, withSpider: true);
            PopulateHorrorAtmosphere(root.transform, 26f, 26f, 3.6f, mats, meshes, playerTf,
                new[]
                {
                    new[] { new Vector3(-9.5f, 0.08f, -9.5f), new Vector3(-7.2f, 0.08f, -1.5f), new Vector3(-9.5f, 0.08f, 6.2f) },
                    new[] { new Vector3(9.5f, 0.08f, -9.5f), new Vector3(7.4f, 0.08f, -0.5f), new Vector3(9.5f, 0.08f, 7.2f) },
                    new[] { new Vector3(-3.2f, 0.08f, -7.5f), new Vector3(3.2f, 0.08f, -7.5f), new Vector3(3.2f, 0.08f, 10.2f), new Vector3(-3.2f, 0.08f, 10.2f) }
                });

            // 3 Dynamic Randomized Math Riddle Note Containers + 5 Oil Flask Containers (+20% each)
            InteractableContainer note1 = CreateContainer(root.transform, "TallWardrobe_MathNote1", new Vector3(-11.7f, 0f, -8.2f), 90f,
                ContainerStyle.TallWardrobe, ContainerRewardType.StoryNote, 0, 0f, "MATH RIDDLE I", "", mats, fc);
            InteractableContainer oil1 = CreateContainer(root.transform, "Nightstand_Oil1", new Vector3(11.7f, 0f, -8.2f), -90f,
                ContainerStyle.BedsideNightstand, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);
            InteractableContainer note2 = CreateContainer(root.transform, "WritingDesk_MathNote2", new Vector3(-11.4f, 0f, 2.2f), 90f,
                ContainerStyle.WritingDesk, ContainerRewardType.StoryNote, 0, 0f, "MATH RIDDLE II", "", mats, fc);
            InteractableContainer oil2 = CreateContainer(root.transform, "Apothecary_Oil2", new Vector3(11.7f, 0f, 2.2f), -90f,
                ContainerStyle.ApothecaryChest, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);
            InteractableContainer oil3 = CreateContainer(root.transform, "Nightstand_Oil3", new Vector3(-7.6f, 0f, 11.7f), 180f,
                ContainerStyle.BedsideNightstand, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);
            InteractableContainer note3 = CreateContainer(root.transform, "WritingDesk_MathNote3", new Vector3(11.4f, 0f, 9.6f), -90f,
                ContainerStyle.WritingDesk, ContainerRewardType.StoryNote, 0, 0f, "MATH RIDDLE III", "", mats, fc);
            InteractableContainer oil4 = CreateContainer(root.transform, "ThreeDrawer_Oil4", new Vector3(0f, 0f, 2.5f), 180f,
                ContainerStyle.ThreeDrawerChest, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);
            InteractableContainer oil5 = CreateContainer(root.transform, "Apothecary_Oil5", new Vector3(7.6f, 0f, 11.7f), 180f,
                ContainerStyle.ApothecaryChest, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);

            fc.containers = new[] { note1, oil1, note2, oil2, oil3, note3, oil4, oil5 };
            fc.dynamicClueContainers = new[] { note1, note2, note3 };
            fc.spiritClueMarkers = BuildSpiritWallCluesForFloor(root.transform, fc.containers, mats);
            fc.pickups = new PickupItem[0];

            fc.exitDoor = CreateExitDoorWithStaircase(root.transform, "ExitDoor_Floor2", new Vector3(0f, 0f, 13.0f),
                DoorLockType.CodeLocked, "942", "FLOOR 2 — MATH RIDDLE LOCK", "Solve the 3 randomized riddles in your Notes Inventory",
                mats, fc, out StaircaseTrigger stairTrig);
            fc.staircaseTrigger = stairTrig;

            fc.disorientationManager = SetupDisorientation(root, mats, meshes, playerLamp, playerTf,
                new[] { new Vector3(-8.4f, 0f, -5.2f), new Vector3(8.4f, 0f, -5.2f), new Vector3(-8.4f, 0f, 5.6f), new Vector3(8.4f, 0f, 5.6f), new Vector3(-3.0f, 0f, 8.2f), new Vector3(3.0f, 0f, 8.2f) },
                new[] { new Vector3(-8.4f, 0f, -5.2f), new Vector3(8.4f, 0f, -5.2f), new Vector3(-8.4f, 0f, 5.6f), new Vector3(8.4f, 0f, 5.6f), new Vector3(-3.0f, 0f, 8.2f), new Vector3(3.0f, 0f, 8.2f), new Vector3(-8.4f, 0f, -10.0f), new Vector3(8.4f, 0f, -10.0f), new Vector3(-2.6f, 0f, 5.8f), new Vector3(2.6f, 0f, 5.8f), new Vector3(-8.6f, 0f, -0.5f), new Vector3(8.6f, 0f, -0.5f) });

            return fc;
        }

        private static FloorController BuildFloor3(Transform parent, Vector3 origin, MaterialPalette mats, LowPolyMeshes meshes, LampController playerLamp, Transform playerTf)
        {
            GameObject root = new GameObject("Floor_3_TheAbyssalSanctum");
            root.transform.SetParent(parent);
            root.transform.position = origin;

            FloorController fc = root.AddComponent<FloorController>();
            fc.floorNumber = 3;
            fc.floorSubtitle = "The Abyssal Sanctum";
            fc.puzzleMode = FloorPuzzleMode.RandomizedSymbolLock;
            fc.requiredPuzzlePieces = 0;
            fc.totalClueNotes = 4;

            BuildRoomShellWithNorthDoorway(root.transform, 26f, 26f, 3.8f, mats.floor3, mats.darkWall);
            fc.playerSpawnPoint = CreateMarker(root.transform, "PlayerSpawn", new Vector3(0f, 0f, -10.5f));

            Transform arch = new GameObject("SanctumArchitecture").transform;
            arch.SetParent(root.transform, false);

            // Octagonal Low-Poly Gothic Stone Pillars & Partitioned Sanctum Alcoves
            CreateGothicPillar(arch, "GothicPillar_SW", new Vector3(-4.8f, 0f, -4.8f), mats.darkWall, meshes);
            CreateGothicPillar(arch, "GothicPillar_SE", new Vector3(4.8f, 0f, -4.8f), mats.darkWall, meshes);
            CreateGothicPillar(arch, "GothicPillar_NW", new Vector3(-4.8f, 0f, 4.8f), mats.darkWall, meshes);
            CreateGothicPillar(arch, "GothicPillar_NE", new Vector3(4.8f, 0f, 4.8f), mats.darkWall, meshes);

            CreateWall(arch, "WestAlcoveDivider", new Vector3(-9.8f, 1.9f, 1.0f), new Vector3(5.6f, 3.8f, 0.45f), mats.darkWall);
            CreateWall(arch, "EastAlcoveDivider", new Vector3(9.8f, 1.9f, -1.0f), new Vector3(5.6f, 3.8f, 0.45f), mats.darkWall);

            Transform decor = new GameObject("HaveliFurniture").transform;
            decor.SetParent(root.transform, false);

            CreateRug(decor, "SanctumRug", new Vector3(0f, 0.015f, 0f), new Vector2(6.5f, 7.5f), mats.rugCrimson, mats.ebonyWood);
            CreateBookshelf(decor, "AbyssalShelf_W", new Vector3(-12.1f, 0f, -2.2f), 90f, mats);
            CreateBookshelf(decor, "AbyssalShelf_E", new Vector3(12.1f, 0f, 2.2f), -90f, mats);
            CreateTable(decor, "AltarTable", new Vector3(0f, 0f, 0.5f), mats);
            CreateBrokenChair(decor, "BrokenAltarChair", new Vector3(0.4f, 0f, -1.0f), 25f, mats);
            CreateFramedWallArt(decor, "Painting_Abyss1", new Vector3(-12.55f, 2.0f, 5.8f), 90f, new Vector2(1.25f, 1.55f), mats.paintingMirror, mats.ebonyWood);
            CreateFramedWallArt(decor, "Painting_Abyss2", new Vector3(12.55f, 2.0f, -5.8f), -90f, new Vector2(1.65f, 1.15f), mats.paintingMoonTree, mats.ebonyWood);
            CreateFramedWallArt(decor, "Painting_Abyss3", new Vector3(-12.55f, 2.0f, -9.8f), 90f, new Vector2(1.10f, 1.42f), mats.paintingPortrait, mats.ebonyWood);

            CreateCeilingBeamsAndWainscoting(decor, 26f, 26f, 3.8f, mats.ebonyWood, mats.darkWall);
            CreateWroughtIronChandelier(decor, "Chandelier_SanctumCenter", new Vector3(0f, 3.75f, 0.5f), mats, meshes, withSpider: true);
            CreateWroughtIronChandelier(decor, "Chandelier_SanctumNorth", new Vector3(0f, 3.75f, 8.8f), mats, meshes, withSpider: true);
            PopulateHorrorAtmosphere(root.transform, 26f, 26f, 3.8f, mats, meshes, playerTf,
                new[]
                {
                    new[] { new Vector3(-9.2f, 0.08f, -8.5f), new Vector3(-3.0f, 0.08f, -7.5f), new Vector3(-3.0f, 0.08f, 8.2f) },
                    new[] { new Vector3(9.2f, 0.08f, -8.5f), new Vector3(3.0f, 0.08f, -2.5f), new Vector3(9.2f, 0.08f, 8.2f) },
                    new[] { new Vector3(-8.5f, 0.08f, 3.2f), new Vector3(0f, 0.08f, 7.2f), new Vector3(8.5f, 0.08f, 3.2f) }
                });

            // 4 Dynamic Randomized Gothic Symbol Plaque Containers (Skull, Moon, Key, Crown) + 5 Oil Flasks (+20% each)
            InteractableContainer plaque1 = CreateContainer(root.transform, "Apothecary_Plaque1", new Vector3(-11.7f, 0f, -7.2f), 90f,
                ContainerStyle.ApothecaryChest, ContainerRewardType.StoryNote, 0, 0f, "SANCTUM PLAQUE I", "", mats, fc);
            InteractableContainer plaque2 = CreateContainer(root.transform, "ThreeDrawer_Plaque2", new Vector3(11.7f, 0f, -7.2f), -90f,
                ContainerStyle.ThreeDrawerChest, ContainerRewardType.StoryNote, 0, 0f, "SANCTUM PLAQUE II", "", mats, fc);
            InteractableContainer plaque3 = CreateContainer(root.transform, "Apothecary_Plaque3", new Vector3(-11.7f, 0f, 9.6f), 90f,
                ContainerStyle.ApothecaryChest, ContainerRewardType.StoryNote, 0, 0f, "SANCTUM PLAQUE III", "", mats, fc);
            InteractableContainer plaque4 = CreateContainer(root.transform, "TallWardrobe_Plaque4", new Vector3(11.7f, 0f, 9.6f), -90f,
                ContainerStyle.TallWardrobe, ContainerRewardType.StoryNote, 0, 0f, "SANCTUM PLAQUE IV", "", mats, fc);

            InteractableContainer oil1 = CreateContainer(root.transform, "Nightstand_Oil1", new Vector3(-5.2f, 0f, -11.7f), 0f,
                ContainerStyle.BedsideNightstand, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);
            InteractableContainer oil2 = CreateContainer(root.transform, "Nightstand_Oil2", new Vector3(5.2f, 0f, -11.7f), 0f,
                ContainerStyle.BedsideNightstand, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);
            InteractableContainer oil3 = CreateContainer(root.transform, "WritingDesk_Oil3", new Vector3(-11.4f, 0f, 4.2f), 90f,
                ContainerStyle.WritingDesk, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);
            InteractableContainer oil4 = CreateContainer(root.transform, "WritingDesk_Oil4", new Vector3(11.4f, 0f, 5.2f), -90f,
                ContainerStyle.WritingDesk, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);
            InteractableContainer oil5 = CreateContainer(root.transform, "Nightstand_Oil5", new Vector3(-6.8f, 0f, 11.7f), 180f,
                ContainerStyle.BedsideNightstand, ContainerRewardType.OilFlask, 0, 20f, "", "", mats, fc);

            fc.containers = new[] { plaque1, plaque2, plaque3, plaque4, oil1, oil2, oil3, oil4, oil5 };
            fc.dynamicClueContainers = new[] { plaque1, plaque2, plaque3, plaque4 };
            fc.spiritClueMarkers = BuildSpiritWallCluesForFloor(root.transform, fc.containers, mats);
            fc.pickups = new PickupItem[0];

            // Final Exit Gate: Randomized 4-Step Gothic Symbol Ancient Stone Lock!
            fc.exitDoor = CreateExitDoorWithStaircase(root.transform, "ExitDoor_Floor3", new Vector3(0f, 0f, 13.0f),
                DoorLockType.SymbolSequenceLocked, "", "ANCIENT STONE LOCK — FINAL GATE", "Input the 4-symbol order from the Sanctum Plaques",
                mats, fc, out StaircaseTrigger stairTrig);
            fc.staircaseTrigger = stairTrig;

            fc.disorientationManager = SetupDisorientation(root, mats, meshes, playerLamp, playerTf,
                new[] { new Vector3(-8.4f, 0f, -3.5f), new Vector3(8.4f, 0f, -3.5f), new Vector3(-8.4f, 0f, 6.2f), new Vector3(8.4f, 0f, 6.2f), new Vector3(-2.5f, 0f, 2.5f), new Vector3(6.2f, 0f, 9.8f) },
                new[] { new Vector3(-8.4f, 0f, -3.5f), new Vector3(8.4f, 0f, -3.5f), new Vector3(-8.4f, 0f, 6.2f), new Vector3(8.4f, 0f, 6.2f), new Vector3(-2.5f, 0f, 2.5f), new Vector3(6.2f, 0f, 9.8f), new Vector3(2.5f, 0f, 2.5f), new Vector3(-8.4f, 0f, -9.2f), new Vector3(8.4f, 0f, -9.2f), new Vector3(0f, 0f, -6.2f), new Vector3(-6.2f, 0f, 8.8f), new Vector3(8.2f, 0f, 1.5f) });

            return fc;
        }

        // =========================================================================
        // FURNITURE, 4-POSTER BED, CONTAINERS, CHEST, EXIT DOOR & STAIRCASE
        // =========================================================================

        private static void CreateFourPosterBed(Transform parent, string name, Vector3 localPos, float yRot, MaterialPalette mats, LowPolyMeshes meshes)
        {
            GameObject bed = new GameObject(name);
            bed.transform.SetParent(parent, false);
            bed.transform.localPosition = localPos;
            bed.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            BoxCollider col = bed.AddComponent<BoxCollider>();
            col.size = new Vector3(1.90f, 1.1f, 2.50f);
            col.center = new Vector3(0f, 0.55f, 0f);

            CreatePrimitiveChild("FrameBase", PrimitiveType.Cube, bed.transform, new Vector3(0f, 0.28f, 0f), new Vector3(1.80f, 0.28f, 2.38f), mats.darkWood, true);
            CreatePrimitiveChild("Headboard", PrimitiveType.Cube, bed.transform, new Vector3(0f, 0.85f, -1.14f), new Vector3(1.86f, 1.25f, 0.14f), mats.mahoganyWood, true);
            CreatePrimitiveChild("Footboard", PrimitiveType.Cube, bed.transform, new Vector3(0f, 0.50f, 1.14f), new Vector3(1.86f, 0.65f, 0.12f), mats.mahoganyWood, true);
            CreatePrimitiveChild("Mattress", PrimitiveType.Cube, bed.transform, new Vector3(0f, 0.54f, 0f), new Vector3(1.68f, 0.30f, 2.18f), mats.linenWhite, true);
            CreatePrimitiveChild("Blanket", PrimitiveType.Cube, bed.transform, new Vector3(0f, 0.58f, 0.28f), new Vector3(1.72f, 0.28f, 1.54f), mats.blanketRed, true);
            CreatePrimitiveChild("Pillow", PrimitiveType.Cube, bed.transform, new Vector3(0f, 0.74f, -0.78f), new Vector3(1.18f, 0.18f, 0.50f), mats.linenWhite, true);

            // 4 Carved Gothic Bedposts + Canopy Frame
            Vector3[] posts = { new Vector3(-0.88f, 1.22f, -1.14f), new Vector3(0.88f, 1.22f, -1.14f), new Vector3(-0.88f, 1.22f, 1.14f), new Vector3(0.88f, 1.22f, 1.14f) };
            for (int i = 0; i < posts.Length; i++)
            {
                CreateMeshChild($"BedPost_{i + 1}", meshes.gothicPillar, bed.transform, posts[i], new Vector3(0.11f, 0.65f, 0.11f), mats.darkWood);
            }
            CreatePrimitiveChild("CanopyRailL", PrimitiveType.Cube, bed.transform, new Vector3(-0.88f, 2.40f, 0f), new Vector3(0.08f, 0.10f, 2.38f), mats.mahoganyWood, true);
            CreatePrimitiveChild("CanopyRailR", PrimitiveType.Cube, bed.transform, new Vector3(0.88f, 2.40f, 0f), new Vector3(0.08f, 0.10f, 2.38f), mats.mahoganyWood, true);
            CreatePrimitiveChild("CanopyRailF", PrimitiveType.Cube, bed.transform, new Vector3(0f, 2.40f, 1.14f), new Vector3(1.84f, 0.10f, 0.08f), mats.mahoganyWood, true);
            CreatePrimitiveChild("CanopyRailB", PrimitiveType.Cube, bed.transform, new Vector3(0f, 2.40f, -1.14f), new Vector3(1.84f, 0.10f, 0.08f), mats.mahoganyWood, true);
        }

        private static void CreateGothicPillar(Transform parent, string name, Vector3 localPos, Material mat, LowPolyMeshes meshes)
        {
            GameObject p = new GameObject(name);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = localPos;
            BoxCollider col = p.AddComponent<BoxCollider>();
            col.size = new Vector3(1.3f, 3.8f, 1.3f);
            col.center = new Vector3(0f, 1.9f, 0f);
            CreateMeshChild("OctaShaft", meshes.gothicPillar, p.transform, new Vector3(0f, 1.9f, 0f), Vector3.one, mat);
            CreatePrimitiveChild("Plinth", PrimitiveType.Cube, p.transform, new Vector3(0f, 0.18f, 0f), new Vector3(1.45f, 0.36f, 1.45f), mat, true);
            CreatePrimitiveChild("Capital", PrimitiveType.Cube, p.transform, new Vector3(0f, 3.62f, 0f), new Vector3(1.45f, 0.36f, 1.45f), mat, true);
        }

        private static InteractableContainer CreateContainer(
            Transform parent, string name, Vector3 localPos, float yRot,
            ContainerStyle style, ContainerRewardType rewardType, int pieceNumber, float oilAmount,
            string noteTitle, string noteBody, MaterialPalette mats, FloorController floor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            BoxCollider col = go.AddComponent<BoxCollider>();
            Transform movingPart;
            GameObject rewardVisual;
            ContainerKind kind;
            string displayName;

            switch (style)
            {
                case ContainerStyle.TallWardrobe:
                    kind = ContainerKind.Cupboard;
                    displayName = "WARDROBE";
                    col.size = new Vector3(1.55f, 2.3f, 0.90f);
                    col.center = new Vector3(0f, 1.15f, 0f);

                    CreatePrimitiveChild("WardrobeBody", PrimitiveType.Cube, go.transform, new Vector3(0f, 1.10f, -0.05f), new Vector3(1.48f, 2.20f, 0.78f), mats.mahoganyWood, true);
                    CreatePrimitiveChild("CrownMolding", PrimitiveType.Cube, go.transform, new Vector3(0f, 2.24f, 0f), new Vector3(1.62f, 0.14f, 0.90f), mats.darkWood, true);
                    CreatePrimitiveChild("PlinthBase", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1.56f, 0.16f, 0.86f), mats.darkWood, true);
                    CreatePrimitiveChild("RightDoorStatic", PrimitiveType.Cube, go.transform, new Vector3(0.35f, 1.12f, 0.36f), new Vector3(0.66f, 1.88f, 0.07f), mats.midWood, true);
                    CreatePrimitiveChild("RightHandle", PrimitiveType.Sphere, go.transform, new Vector3(0.12f, 1.12f, 0.42f), new Vector3(0.08f, 0.08f, 0.08f), mats.brassMetal, true);

                    GameObject wardPivot = new GameObject("LeftDoorPivot");
                    wardPivot.transform.SetParent(go.transform, false);
                    wardPivot.transform.localPosition = new Vector3(-0.68f, 1.12f, 0.36f);
                    CreatePrimitiveChild("LeftDoorPanel", PrimitiveType.Cube, wardPivot.transform, new Vector3(0.33f, 0f, 0f), new Vector3(0.66f, 1.88f, 0.07f), mats.midWood, true);
                    CreatePrimitiveChild("LeftHandle", PrimitiveType.Sphere, wardPivot.transform, new Vector3(0.56f, 0f, 0.06f), new Vector3(0.08f, 0.08f, 0.08f), mats.brassMetal, true);

                    movingPart = wardPivot.transform;
                    rewardVisual = CreateContainerRewardVisual(go.transform, new Vector3(-0.25f, 1.05f, 0.20f), rewardType, mats);
                    break;

                case ContainerStyle.ThreeDrawerChest:
                    kind = ContainerKind.Drawer;
                    displayName = "CHEST DRAWER";
                    col.size = new Vector3(1.50f, 1.18f, 0.88f);
                    col.center = new Vector3(0f, 0.59f, 0f);

                    CreatePrimitiveChild("ChestCarcass", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.55f, -0.02f), new Vector3(1.42f, 1.10f, 0.78f), mats.darkWood, true);
                    CreatePrimitiveChild("ChestTop", PrimitiveType.Cube, go.transform, new Vector3(0f, 1.13f, 0f), new Vector3(1.54f, 0.08f, 0.88f), mats.mahoganyWood, true);
                    CreatePrimitiveChild("BottomDrawerFront", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.22f, 0.39f), new Vector3(1.26f, 0.28f, 0.06f), mats.midWood, true);
                    CreatePrimitiveChild("BottomPull", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.22f, 0.44f), new Vector3(0.24f, 0.05f, 0.05f), mats.brassMetal, true);
                    CreatePrimitiveChild("MidDrawerFront", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.54f, 0.39f), new Vector3(1.26f, 0.28f, 0.06f), mats.midWood, true);
                    CreatePrimitiveChild("MidPull", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.54f, 0.44f), new Vector3(0.24f, 0.05f, 0.05f), mats.brassMetal, true);

                    GameObject topDrawer = new GameObject("TopSlidingDrawer");
                    topDrawer.transform.SetParent(go.transform, false);
                    topDrawer.transform.localPosition = new Vector3(0f, 0.86f, 0.08f);
                    CreatePrimitiveChild("TopDrawerFront", PrimitiveType.Cube, topDrawer.transform, new Vector3(0f, 0f, 0.31f), new Vector3(1.26f, 0.28f, 0.06f), mats.midWood, true);
                    CreatePrimitiveChild("TopDrawerTray", PrimitiveType.Cube, topDrawer.transform, new Vector3(0f, -0.09f, 0f), new Vector3(1.16f, 0.06f, 0.58f), mats.darkWood, true);
                    CreatePrimitiveChild("TopPull", PrimitiveType.Cube, topDrawer.transform, new Vector3(0f, 0f, 0.36f), new Vector3(0.24f, 0.05f, 0.05f), mats.brassMetal, true);

                    movingPart = topDrawer.transform;
                    rewardVisual = CreateContainerRewardVisual(topDrawer.transform, new Vector3(0f, 0.05f, 0.05f), rewardType, mats);
                    break;

                case ContainerStyle.BedsideNightstand:
                    kind = ContainerKind.Drawer;
                    displayName = "NIGHTSTAND";
                    col.size = new Vector3(0.95f, 0.82f, 0.80f);
                    col.center = new Vector3(0f, 0.41f, 0f);

                    CreatePrimitiveChild("LegFL", PrimitiveType.Cube, go.transform, new Vector3(-0.36f, 0.38f, 0.28f), new Vector3(0.08f, 0.76f, 0.08f), mats.darkWood, true);
                    CreatePrimitiveChild("LegFR", PrimitiveType.Cube, go.transform, new Vector3(0.36f, 0.38f, 0.28f), new Vector3(0.08f, 0.76f, 0.08f), mats.darkWood, true);
                    CreatePrimitiveChild("LegBL", PrimitiveType.Cube, go.transform, new Vector3(-0.36f, 0.38f, -0.28f), new Vector3(0.08f, 0.76f, 0.08f), mats.darkWood, true);
                    CreatePrimitiveChild("LegBR", PrimitiveType.Cube, go.transform, new Vector3(0.36f, 0.38f, -0.28f), new Vector3(0.08f, 0.76f, 0.08f), mats.darkWood, true);
                    CreatePrimitiveChild("LowerShelf", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.20f, 0f), new Vector3(0.76f, 0.05f, 0.60f), mats.midWood, true);
                    CreatePrimitiveChild("UpperBox", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.58f, -0.02f), new Vector3(0.80f, 0.32f, 0.62f), mats.darkWood, true);
                    CreatePrimitiveChild("TopSlab", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.78f, 0f), new Vector3(0.92f, 0.06f, 0.74f), mats.mahoganyWood, true);

                    GameObject nsDrawer = new GameObject("NightstandDrawer");
                    nsDrawer.transform.SetParent(go.transform, false);
                    nsDrawer.transform.localPosition = new Vector3(0f, 0.58f, 0.08f);
                    CreatePrimitiveChild("DrawerFront", PrimitiveType.Cube, nsDrawer.transform, new Vector3(0f, 0f, 0.24f), new Vector3(0.68f, 0.24f, 0.05f), mats.midWood, true);
                    CreatePrimitiveChild("DrawerTray", PrimitiveType.Cube, nsDrawer.transform, new Vector3(0f, -0.08f, 0f), new Vector3(0.62f, 0.05f, 0.44f), mats.darkWood, true);
                    CreatePrimitiveChild("BrassKnob", PrimitiveType.Sphere, nsDrawer.transform, new Vector3(0f, 0f, 0.29f), new Vector3(0.09f, 0.09f, 0.09f), mats.brassMetal, true);

                    movingPart = nsDrawer.transform;
                    rewardVisual = CreateContainerRewardVisual(nsDrawer.transform, new Vector3(0f, 0.04f, 0.03f), rewardType, mats);
                    break;

                case ContainerStyle.ApothecaryChest:
                    kind = ContainerKind.Drawer;
                    displayName = "APOTHECARY CHEST";
                    col.size = new Vector3(1.52f, 1.65f, 0.84f);
                    col.center = new Vector3(0f, 0.825f, 0f);

                    CreatePrimitiveChild("ApothecaryBody", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.80f, -0.04f), new Vector3(1.44f, 1.56f, 0.72f), mats.ebonyWood, true);
                    CreatePrimitiveChild("ApothecaryCrown", PrimitiveType.Cube, go.transform, new Vector3(0f, 1.62f, 0f), new Vector3(1.58f, 0.10f, 0.84f), mats.mahoganyWood, true);
                    CreatePrimitiveChild("ApothecaryBase", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1.52f, 0.16f, 0.80f), mats.darkWood, true);

                    // 3 rows x 3 cols of antique apothecary mini-cubbies (center row slides open!)
                    float[] rowYs = { 0.34f, 0.82f, 1.28f };
                    float[] colXs = { -0.44f, 0f, 0.44f };
                    for (int r = 0; r < rowYs.Length; r++)
                    {
                        if (r == 1) continue; // middle row is the sliding drawer
                        for (int c = 0; c < colXs.Length; c++)
                        {
                            CreatePrimitiveChild($"Cubby_{r}_{c}", PrimitiveType.Cube, go.transform,
                                new Vector3(colXs[c], rowYs[r], 0.34f), new Vector3(0.40f, 0.34f, 0.06f), mats.mahoganyWood, true);
                            CreatePrimitiveChild($"Pull_{r}_{c}", PrimitiveType.Sphere, go.transform,
                                new Vector3(colXs[c], rowYs[r], 0.39f), new Vector3(0.065f, 0.065f, 0.065f), mats.brassMetal, true);
                        }
                    }

                    GameObject apothDrawer = new GameObject("ApothecarySlidingDrawer");
                    apothDrawer.transform.SetParent(go.transform, false);
                    apothDrawer.transform.localPosition = new Vector3(0f, 0.82f, 0.08f);
                    CreatePrimitiveChild("ApothTray", PrimitiveType.Cube, apothDrawer.transform, new Vector3(0f, -0.10f, 0f), new Vector3(1.26f, 0.06f, 0.54f), mats.darkWood, true);
                    for (int c = 0; c < colXs.Length; c++)
                    {
                        CreatePrimitiveChild($"SlideFront_{c}", PrimitiveType.Cube, apothDrawer.transform,
                            new Vector3(colXs[c], 0f, 0.27f), new Vector3(0.40f, 0.34f, 0.06f), mats.midWood, true);
                        CreatePrimitiveChild($"SlidePull_{c}", PrimitiveType.Sphere, apothDrawer.transform,
                            new Vector3(colXs[c], 0f, 0.32f), new Vector3(0.065f, 0.065f, 0.065f), mats.brassMetal, true);
                    }

                    movingPart = apothDrawer.transform;
                    rewardVisual = CreateContainerRewardVisual(apothDrawer.transform, new Vector3(0f, 0.04f, 0.04f), rewardType, mats);
                    break;

                default: // WritingDesk
                    kind = ContainerKind.Drawer;
                    displayName = "WRITING DESK";
                    col.size = new Vector3(1.85f, 0.90f, 0.98f);
                    col.center = new Vector3(0f, 0.45f, 0f);

                    CreatePrimitiveChild("DeskTop", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.84f, 0f), new Vector3(1.82f, 0.08f, 0.94f), mats.mahoganyWood, true);
                    CreatePrimitiveChild("LeftPedestal", PrimitiveType.Cube, go.transform, new Vector3(-0.68f, 0.40f, 0f), new Vector3(0.38f, 0.80f, 0.80f), mats.darkWood, true);
                    CreatePrimitiveChild("RightPedestal", PrimitiveType.Cube, go.transform, new Vector3(0.68f, 0.40f, 0f), new Vector3(0.38f, 0.80f, 0.80f), mats.darkWood, true);
                    CreatePrimitiveChild("BackApron", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.56f, -0.36f), new Vector3(1.10f, 0.46f, 0.06f), mats.darkWood, true);
                    CreatePrimitiveChild("DeskBook", PrimitiveType.Cube, go.transform, new Vector3(-0.55f, 0.91f, -0.10f), new Vector3(0.32f, 0.06f, 0.24f), mats.bookRed, true);
                    CreatePrimitiveChild("Inkwell", PrimitiveType.Cylinder, go.transform, new Vector3(0.58f, 0.92f, -0.15f), new Vector3(0.09f, 0.06f, 0.09f), mats.ironMetal, true);

                    GameObject deskDrawer = new GameObject("DeskCenterDrawer");
                    deskDrawer.transform.SetParent(go.transform, false);
                    deskDrawer.transform.localPosition = new Vector3(0f, 0.70f, 0.12f);
                    CreatePrimitiveChild("DeskDrawerFront", PrimitiveType.Cube, deskDrawer.transform, new Vector3(0f, 0f, 0.30f), new Vector3(0.92f, 0.18f, 0.05f), mats.midWood, true);
                    CreatePrimitiveChild("DeskDrawerTray", PrimitiveType.Cube, deskDrawer.transform, new Vector3(0f, -0.06f, 0f), new Vector3(0.84f, 0.05f, 0.56f), mats.darkWood, true);
                    CreatePrimitiveChild("DeskPull", PrimitiveType.Cube, deskDrawer.transform, new Vector3(0f, 0f, 0.34f), new Vector3(0.20f, 0.04f, 0.04f), mats.brassMetal, true);

                    movingPart = deskDrawer.transform;
                    rewardVisual = CreateContainerRewardVisual(deskDrawer.transform, new Vector3(0f, 0.03f, 0.04f), rewardType, mats);
                    break;
            }

            if (rewardVisual != null) rewardVisual.SetActive(false);

            // Cursed Drawer Leaping Spider Visual (hidden until cursed drawer is opened)
            float frontY = Mathf.Clamp(col.size.y * 0.62f, 0.58f, 1.10f);
            float frontZ = col.size.z * 0.5f + 0.04f;
            GameObject spiderTrapVis = CreateContainerSpiderTrap(go.transform, new Vector3(0f, frontY, 0.16f), mats);
            spiderTrapVis.SetActive(false);

            // Blue Spirit Flame Indicators (hidden unless Blue Spirit Flame is active; unlit, ZERO scene light emitted!)
            GameObject spiritCyanArrow = CreateSpiritCyanArrow(go.transform, new Vector3(0f, col.size.y + 0.40f, 0.18f), Quaternion.identity, mats);
            spiritCyanArrow.SetActive(false);

            GameObject spiritRedMark = CreateSpiritRedXMark(go.transform, new Vector3(0f, frontY, frontZ), mats);
            spiritRedMark.SetActive(false);

            InteractableContainer ic = go.AddComponent<InteractableContainer>();
            ic.containerKind = kind;
            ic.containerDisplayName = displayName;
            ic.rewardType = rewardType;
            ic.puzzlePieceNumber = pieceNumber;
            ic.oilRestoreAmount = oilAmount;
            ic.noteTitle = noteTitle;
            ic.noteBody = noteBody;
            ic.ownerFloor = floor;
            ic.movingPart = movingPart;
            ic.rewardVisual = rewardVisual;
            ic.cursedSpiderVisual = spiderTrapVis;
            ic.spiritCyanMarkVisual = spiritCyanArrow;
            ic.spiritRedMarkVisual = spiritRedMark;
            ic.autoCloseDelay = 2.5f;
            ic.cursedOilDrain = 10f;

            return ic;
        }

        private static GameObject CreateContainerSpiderTrap(Transform parent, Vector3 localPos, MaterialPalette mats)
        {
            GameObject spider = new GameObject("CursedSpiderTrapVisual");
            spider.transform.SetParent(parent, false);
            spider.transform.localPosition = localPos;
            spider.transform.localScale = Vector3.one * 1.15f;

            CreatePrimitiveChild("Abdomen", PrimitiveType.Sphere, spider.transform, new Vector3(0f, 0.02f, -0.09f), new Vector3(0.18f, 0.14f, 0.22f), mats.spiderBodyMat, true);
            CreatePrimitiveChild("Head", PrimitiveType.Sphere, spider.transform, new Vector3(0f, 0f, 0.07f), new Vector3(0.13f, 0.11f, 0.14f), mats.spiderBodyMat, true);

            // 8 splayed spider legs
            float[] legZ = { 0.08f, 0.03f, -0.03f, -0.08f };
            float[] legYaw = { 34f, 12f, -12f, -34f };
            for (int side = -1; side <= 1; side += 2)
            {
                for (int l = 0; l < 4; l++)
                {
                    GameObject leg = CreatePrimitiveChild($"TrapLeg_{(side < 0 ? "L" : "R")}_{l}", PrimitiveType.Cube, spider.transform,
                        new Vector3(side * 0.16f, 0.02f, legZ[l]), new Vector3(0.26f, 0.024f, 0.024f), mats.spiderBodyMat, true);
                    leg.transform.localRotation = Quaternion.Euler(0f, side * legYaw[l], -side * 18f);
                }
            }

            // Tiny unlit red eyes (zero scene light emitted)
            CreatePrimitiveChild("TrapEyeL", PrimitiveType.Sphere, spider.transform, new Vector3(-0.032f, 0.022f, 0.13f), new Vector3(0.030f, 0.030f, 0.030f), mats.spiderRedEyeUnlit, true);
            CreatePrimitiveChild("TrapEyeR", PrimitiveType.Sphere, spider.transform, new Vector3(0.032f, 0.022f, 0.13f), new Vector3(0.030f, 0.030f, 0.030f), mats.spiderRedEyeUnlit, true);
            return spider;
        }

        private static GameObject CreateSpiritCyanArrow(Transform parent, Vector3 localPos, Quaternion localRot, MaterialPalette mats)
        {
            // Glowing cyan downward chevron/arrow (unlit cyan, zero scene light)
            GameObject arrow = new GameObject("SpiritCyanClueArrow");
            arrow.transform.SetParent(parent, false);
            arrow.transform.localPosition = localPos;
            arrow.transform.localRotation = localRot;

            CreatePrimitiveChild("Shaft", PrimitiveType.Cube, arrow.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.08f, 0.34f, 0.04f), mats.spiritCyanUnlit, true);
            GameObject wingL = CreatePrimitiveChild("HeadL", PrimitiveType.Cube, arrow.transform, new Vector3(-0.10f, -0.02f, 0f), new Vector3(0.26f, 0.07f, 0.04f), mats.spiritCyanUnlit, true);
            wingL.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            GameObject wingR = CreatePrimitiveChild("HeadR", PrimitiveType.Cube, arrow.transform, new Vector3(0.10f, -0.02f, 0f), new Vector3(0.26f, 0.07f, 0.04f), mats.spiritCyanUnlit, true);
            wingR.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return arrow;
        }

        private static GameObject CreateSpiritRedXMark(Transform parent, Vector3 localPos, MaterialPalette mats)
        {
            // Faint crimson 'X' mark revealed on cursed/empty drawers under Blue Spirit Flame (unlit, zero light)
            GameObject mark = new GameObject("SpiritRedWarningMark");
            mark.transform.SetParent(parent, false);
            mark.transform.localPosition = localPos;

            GameObject barA = CreatePrimitiveChild("SlashA", PrimitiveType.Cube, mark.transform, Vector3.zero, new Vector3(0.38f, 0.045f, 0.02f), mats.spiritRedUnlit, true);
            barA.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            GameObject barB = CreatePrimitiveChild("SlashB", PrimitiveType.Cube, mark.transform, Vector3.zero, new Vector3(0.38f, 0.045f, 0.02f), mats.spiritRedUnlit, true);
            barB.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            return mark;
        }

        private static SpiritClueMarker[] BuildSpiritWallCluesForFloor(Transform floorRoot, InteractableContainer[] containers, MaterialPalette mats)
        {
            Transform spiritRoot = new GameObject("SpiritFlameWallClues").transform;
            spiritRoot.SetParent(floorRoot, false);

            List<SpiritClueMarker> markers = new List<SpiritClueMarker>();
            if (containers == null) return markers.ToArray();

            for (int i = 0; i < containers.Length; i++)
            {
                InteractableContainer c = containers[i];
                if (c == null) continue;
                if (c.rewardType != ContainerRewardType.PuzzlePiece && c.rewardType != ContainerRewardType.StoryNote) continue;

                // Place a glowing cyan Spirit Rune / Arrow high on the wall right behind the puzzle container pointing down toward it
                Vector3 backOffset = -(c.transform.localRotation * Vector3.forward) * 0.42f;
                Vector3 wallPos = c.transform.localPosition + backOffset + new Vector3(0f, 2.55f, 0f);
                GameObject wallArrow = CreateSpiritCyanArrow(spiritRoot, wallPos, c.transform.localRotation, mats);
                wallArrow.name = $"SpiritWallArrow_{c.name}";
                wallArrow.transform.localScale = Vector3.one * 1.25f;

                SpiritClueMarker marker = wallArrow.AddComponent<SpiritClueMarker>();
                marker.linkedContainer = c;
                marker.pulseScale = true;
                markers.Add(marker);
            }

            return markers.ToArray();
        }

        private static GameObject CreateContainerRewardVisual(Transform parent, Vector3 localPos, ContainerRewardType rewardType, MaterialPalette mats)
        {
            if (rewardType == ContainerRewardType.Empty) return null;

            GameObject vis = new GameObject("RewardVisual");
            vis.transform.SetParent(parent, false);
            vis.transform.localPosition = localPos;

            switch (rewardType)
            {
                case ContainerRewardType.OilFlask:
                    CreatePrimitiveChild("Flask", PrimitiveType.Sphere, vis.transform, Vector3.zero, new Vector3(0.24f, 0.30f, 0.24f), mats.oilFlask, true);
                    CreatePrimitiveChild("Neck", PrimitiveType.Cylinder, vis.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.08f, 0.07f, 0.08f), mats.brassMetal, true);
                    break;
                case ContainerRewardType.PuzzlePiece:
                    CreatePrimitiveChild("Tile", PrimitiveType.Cube, vis.transform, Vector3.zero, new Vector3(0.28f, 0.06f, 0.28f), mats.puzzlePieceGold, true);
                    break;
                case ContainerRewardType.StoryNote:
                    CreatePrimitiveChild("NotePage", PrimitiveType.Cube, vis.transform, Vector3.zero, new Vector3(0.32f, 0.02f, 0.24f), mats.parchmentPaper, true);
                    break;
            }
            return vis;
        }

        private static void CreateStaticNightstand(Transform parent, string name, Vector3 localPos, MaterialPalette mats)
        {
            GameObject ns = new GameObject(name);
            ns.transform.SetParent(parent, false);
            ns.transform.localPosition = localPos;
            BoxCollider col = ns.AddComponent<BoxCollider>();
            col.size = new Vector3(0.85f, 0.78f, 0.75f);
            col.center = new Vector3(0f, 0.39f, 0f);

            CreatePrimitiveChild("Body", PrimitiveType.Cube, ns.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.78f, 0.74f, 0.68f), mats.darkWood, true);
            CreatePrimitiveChild("Top", PrimitiveType.Cube, ns.transform, new Vector3(0f, 0.76f, 0f), new Vector3(0.88f, 0.06f, 0.78f), mats.mahoganyWood, true);
            CreatePrimitiveChild("Knob", PrimitiveType.Sphere, ns.transform, new Vector3(0f, 0.56f, 0.36f), new Vector3(0.08f, 0.08f, 0.08f), mats.brassMetal, true);
        }

        private static void CreateBookshelf(Transform parent, string name, Vector3 localPos, float yRot, MaterialPalette mats)
        {
            GameObject shelf = new GameObject(name);
            shelf.transform.SetParent(parent, false);
            shelf.transform.localPosition = localPos;
            shelf.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            BoxCollider col = shelf.AddComponent<BoxCollider>();
            col.size = new Vector3(1.85f, 2.4f, 0.60f);
            col.center = new Vector3(0f, 1.2f, 0f);

            CreatePrimitiveChild("BackPanel", PrimitiveType.Cube, shelf.transform, new Vector3(0f, 1.2f, -0.22f), new Vector3(1.80f, 2.36f, 0.08f), mats.darkWood, true);
            CreatePrimitiveChild("SideLeft", PrimitiveType.Cube, shelf.transform, new Vector3(-0.88f, 1.2f, 0f), new Vector3(0.10f, 2.38f, 0.52f), mats.mahoganyWood, true);
            CreatePrimitiveChild("SideRight", PrimitiveType.Cube, shelf.transform, new Vector3(0.88f, 1.2f, 0f), new Vector3(0.10f, 2.38f, 0.52f), mats.mahoganyWood, true);

            Material[] bookMats = { mats.bookRed, mats.bookBlue, mats.bookGreen, mats.mahoganyWood };
            for (int row = 0; row < 4; row++)
            {
                float sy = 0.28f + row * 0.55f;
                CreatePrimitiveChild($"Shelf_{row}", PrimitiveType.Cube, shelf.transform, new Vector3(0f, sy, 0f), new Vector3(1.76f, 0.06f, 0.50f), mats.midWood, true);
                for (int b = 0; b < 5; b++)
                {
                    float bx = -0.62f + b * 0.30f;
                    CreatePrimitiveChild($"Book_{row}_{b}", PrimitiveType.Cube, shelf.transform,
                        new Vector3(bx, sy + 0.19f, 0.02f), new Vector3(0.22f, 0.32f, 0.34f), bookMats[(row + b) % bookMats.Length], true);
                }
            }
            CreatePrimitiveChild("ShelfTop", PrimitiveType.Cube, shelf.transform, new Vector3(0f, 2.38f, 0f), new Vector3(1.90f, 0.10f, 0.58f), mats.darkWood, true);
        }

        private static void CreateTable(Transform parent, string name, Vector3 localPos, MaterialPalette mats)
        {
            GameObject tbl = new GameObject(name);
            tbl.transform.SetParent(parent, false);
            tbl.transform.localPosition = localPos;
            BoxCollider col = tbl.AddComponent<BoxCollider>();
            col.size = new Vector3(2.0f, 0.85f, 1.25f);
            col.center = new Vector3(0f, 0.42f, 0f);

            CreatePrimitiveChild("TableTop", PrimitiveType.Cube, tbl.transform, new Vector3(0f, 0.80f, 0f), new Vector3(2.0f, 0.09f, 1.22f), mats.mahoganyWood, true);
            CreatePrimitiveChild("Leg1", PrimitiveType.Cube, tbl.transform, new Vector3(-0.85f, 0.38f, -0.48f), new Vector3(0.10f, 0.76f, 0.10f), mats.darkWood, true);
            CreatePrimitiveChild("Leg2", PrimitiveType.Cube, tbl.transform, new Vector3(0.85f, 0.38f, -0.48f), new Vector3(0.10f, 0.76f, 0.10f), mats.darkWood, true);
            CreatePrimitiveChild("Leg3", PrimitiveType.Cube, tbl.transform, new Vector3(-0.85f, 0.38f, 0.48f), new Vector3(0.10f, 0.76f, 0.10f), mats.darkWood, true);
            CreatePrimitiveChild("Leg4", PrimitiveType.Cube, tbl.transform, new Vector3(0.85f, 0.38f, 0.48f), new Vector3(0.10f, 0.76f, 0.10f), mats.darkWood, true);
        }

        private static GameObject CreateChair(Transform parent, string name, Vector3 localPos, float yRot, MaterialPalette mats)
        {
            GameObject chair = new GameObject(name);
            chair.transform.SetParent(parent, false);
            chair.transform.localPosition = localPos;
            chair.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            BoxCollider col = chair.AddComponent<BoxCollider>();
            col.size = new Vector3(0.70f, 1.1f, 0.70f);
            col.center = new Vector3(0f, 0.55f, 0f);

            CreatePrimitiveChild("Seat", PrimitiveType.Cube, chair.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.62f, 0.08f, 0.62f), mats.midWood, true);
            CreatePrimitiveChild("BackTop", PrimitiveType.Cube, chair.transform, new Vector3(0f, 0.98f, -0.27f), new Vector3(0.62f, 0.12f, 0.07f), mats.darkWood, true);
            CreatePrimitiveChild("BackSlatL", PrimitiveType.Cube, chair.transform, new Vector3(-0.22f, 0.72f, -0.27f), new Vector3(0.08f, 0.48f, 0.06f), mats.darkWood, true);
            CreatePrimitiveChild("BackSlatR", PrimitiveType.Cube, chair.transform, new Vector3(0.22f, 0.72f, -0.27f), new Vector3(0.08f, 0.48f, 0.06f), mats.darkWood, true);
            CreatePrimitiveChild("Leg1", PrimitiveType.Cube, chair.transform, new Vector3(-0.24f, 0.23f, -0.24f), new Vector3(0.07f, 0.46f, 0.07f), mats.darkWood, true);
            CreatePrimitiveChild("Leg2", PrimitiveType.Cube, chair.transform, new Vector3(0.24f, 0.23f, -0.24f), new Vector3(0.07f, 0.46f, 0.07f), mats.darkWood, true);
            CreatePrimitiveChild("Leg3", PrimitiveType.Cube, chair.transform, new Vector3(-0.24f, 0.23f, 0.24f), new Vector3(0.07f, 0.46f, 0.07f), mats.darkWood, true);
            CreatePrimitiveChild("Leg4", PrimitiveType.Cube, chair.transform, new Vector3(0.24f, 0.23f, 0.24f), new Vector3(0.07f, 0.46f, 0.07f), mats.darkWood, true);
            return chair;
        }

        private static void CreateBrokenChair(Transform parent, string name, Vector3 localPos, float yRot, MaterialPalette mats)
        {
            GameObject chair = new GameObject(name);
            chair.transform.SetParent(parent, false);
            chair.transform.localPosition = localPos;
            chair.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            BoxCollider col = chair.AddComponent<BoxCollider>();
            col.size = new Vector3(0.85f, 0.72f, 0.85f);
            col.center = new Vector3(0f, 0.36f, 0f);

            // Toppled & splintered antique chair
            GameObject body = new GameObject("ToppledBody");
            body.transform.SetParent(chair.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            body.transform.localRotation = Quaternion.Euler(72f, 14f, 22f);

            CreatePrimitiveChild("Seat", PrimitiveType.Cube, body.transform, Vector3.zero, new Vector3(0.62f, 0.08f, 0.62f), mats.midWood, true);
            CreatePrimitiveChild("BackTop", PrimitiveType.Cube, body.transform, new Vector3(0f, 0.50f, -0.27f), new Vector3(0.58f, 0.11f, 0.07f), mats.darkWood, true);
            CreatePrimitiveChild("BackSlatL", PrimitiveType.Cube, body.transform, new Vector3(-0.21f, 0.25f, -0.27f), new Vector3(0.08f, 0.46f, 0.06f), mats.darkWood, true);
            CreatePrimitiveChild("Leg1", PrimitiveType.Cube, body.transform, new Vector3(-0.24f, -0.23f, -0.24f), new Vector3(0.07f, 0.46f, 0.07f), mats.darkWood, true);
            CreatePrimitiveChild("Leg2", PrimitiveType.Cube, body.transform, new Vector3(0.24f, -0.23f, -0.24f), new Vector3(0.07f, 0.46f, 0.07f), mats.darkWood, true);
            CreatePrimitiveChild("Leg3", PrimitiveType.Cube, body.transform, new Vector3(-0.24f, -0.23f, 0.24f), new Vector3(0.07f, 0.46f, 0.07f), mats.darkWood, true);

            // Snapped wooden leg lying nearby on the floorboards
            GameObject snapped = CreatePrimitiveChild("SnappedLeg", PrimitiveType.Cube, chair.transform,
                new Vector3(0.36f, 0.04f, 0.28f), new Vector3(0.07f, 0.42f, 0.07f), mats.darkWood, true);
            snapped.transform.localRotation = Quaternion.Euler(90f, 38f, 0f);
        }

        private static void CreateRug(Transform parent, string name, Vector3 localPos, Vector2 size, Material mainMat, Material trimMat)
        {
            GameObject rug = new GameObject(name);
            rug.transform.SetParent(parent, false);
            rug.transform.localPosition = localPos;
            CreatePrimitiveChild("RugBorder", PrimitiveType.Cube, rug.transform, Vector3.zero, new Vector3(size.x, 0.02f, size.y), trimMat, true);
            CreatePrimitiveChild("RugCenter", PrimitiveType.Cube, rug.transform, new Vector3(0f, 0.008f, 0f), new Vector3(size.x - 0.24f, 0.02f, size.y - 0.24f), mainMat, true);
        }

        private static void CreateFramedWallArt(Transform parent, string name, Vector3 localPos, float yRot, Vector2 frameSize, Material canvasMat, Material frameMat)
        {
            GameObject art = new GameObject(name);
            art.transform.SetParent(parent, false);
            art.transform.localPosition = localPos;
            art.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            float w = Mathf.Max(0.6f, frameSize.x);
            float h = Mathf.Max(0.6f, frameSize.y);
            float border = 0.10f;

            // Ornate molded outer frame + inner brass lip
            CreatePrimitiveChild("FrameBacking", PrimitiveType.Cube, art.transform, Vector3.zero, new Vector3(w, h, 0.07f), frameMat, true);
            CreatePrimitiveChild("FrameTop", PrimitiveType.Cube, art.transform, new Vector3(0f, h * 0.5f - border * 0.5f, 0.04f), new Vector3(w + 0.04f, border, 0.05f), frameMat, true);
            CreatePrimitiveChild("FrameBot", PrimitiveType.Cube, art.transform, new Vector3(0f, -h * 0.5f + border * 0.5f, 0.04f), new Vector3(w + 0.04f, border, 0.05f), frameMat, true);
            CreatePrimitiveChild("FrameLeft", PrimitiveType.Cube, art.transform, new Vector3(-w * 0.5f + border * 0.5f, 0f, 0.04f), new Vector3(border, h, 0.05f), frameMat, true);
            CreatePrimitiveChild("FrameRight", PrimitiveType.Cube, art.transform, new Vector3(w * 0.5f - border * 0.5f, 0f, 0.04f), new Vector3(border, h, 0.05f), frameMat, true);

            // Front-facing Quad canvas (+Z normal via 180 deg Y rotation) so procedural gothic oil art renders upright
            GameObject canvas = CreatePrimitiveChild("OilCanvas", PrimitiveType.Quad, art.transform,
                new Vector3(0f, 0f, 0.042f), new Vector3(w - border * 2.0f, h - border * 2.0f, 1f), canvasMat, true);
            canvas.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private static void CreateCeilingBeamsAndWainscoting(Transform parent, float width, float depth, float wallHeight, Material beamMat, Material wainscotMat)
        {
            Transform trimRoot = new GameObject("CeilingBeamsAndWainscoting").transform;
            trimRoot.SetParent(parent, false);

            // Heavy gothic timber beams running along X across ceiling
            int beamCount = 4;
            for (int i = 0; i < beamCount; i++)
            {
                float t = (i + 1f) / (beamCount + 1f);
                float bz = Mathf.Lerp(-depth * 0.5f, depth * 0.5f, t);
                CreatePrimitiveChild($"CeilingBeam_X_{i + 1}", PrimitiveType.Cube, trimRoot,
                    new Vector3(0f, wallHeight - 0.14f, bz), new Vector3(width - 0.2f, 0.28f, 0.26f), beamMat, true);
            }
            // Spine ridge beam along Z
            CreatePrimitiveChild("CeilingBeam_Spine", PrimitiveType.Cube, trimRoot,
                new Vector3(0f, wallHeight - 0.12f, 0f), new Vector3(0.30f, 0.24f, depth - 0.2f), beamMat, true);

            // Lower timber wainscoting panels & chair rail along perimeter walls (strictly non-colliding decorative trim)
            float halfW = width * 0.5f - 0.44f;
            float halfD = depth * 0.5f - 0.44f;
            float wainH = 1.05f;
            CreatePrimitiveChild("Wainscot_South", PrimitiveType.Cube, trimRoot, new Vector3(0f, wainH * 0.5f, -halfD), new Vector3(width - 0.8f, wainH, 0.06f), wainscotMat, true);
            CreatePrimitiveChild("Wainscot_West", PrimitiveType.Cube, trimRoot, new Vector3(-halfW, wainH * 0.5f, 0f), new Vector3(0.06f, wainH, depth - 0.8f), wainscotMat, true);
            CreatePrimitiveChild("Wainscot_East", PrimitiveType.Cube, trimRoot, new Vector3(halfW, wainH * 0.5f, 0f), new Vector3(0.06f, wainH, depth - 0.8f), wainscotMat, true);

            // Chair rail molding cap
            CreatePrimitiveChild("ChairRail_South", PrimitiveType.Cube, trimRoot, new Vector3(0f, wainH + 0.04f, -halfD + 0.03f), new Vector3(width - 0.8f, 0.08f, 0.10f), beamMat, true);
            CreatePrimitiveChild("ChairRail_West", PrimitiveType.Cube, trimRoot, new Vector3(-halfW + 0.03f, wainH + 0.04f, 0f), new Vector3(0.10f, 0.08f, depth - 0.8f), beamMat, true);
            CreatePrimitiveChild("ChairRail_East", PrimitiveType.Cube, trimRoot, new Vector3(halfW - 0.03f, wainH + 0.04f, 0f), new Vector3(0.10f, 0.08f, depth - 0.8f), beamMat, true);
        }

        private static void CreateWroughtIronChandelier(Transform parent, string name, Vector3 ceilingPos, MaterialPalette mats, LowPolyMeshes meshes, bool withSpider)
        {
            // Unlit multi-tiered gothic wrought-iron chandelier (ZERO lights emitted!)
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = ceilingPos;

            // Ceiling rosette & hanging iron chain
            CreatePrimitiveChild("CeilingRosette", PrimitiveType.Cylinder, root.transform, new Vector3(0f, -0.04f, 0f), new Vector3(0.38f, 0.05f, 0.38f), mats.ironMetal, true);
            CreatePrimitiveChild("HangingRod", PrimitiveType.Cylinder, root.transform, new Vector3(0f, -0.58f, 0f), new Vector3(0.06f, 0.54f, 0.06f), mats.ironMetal, true);
            CreatePrimitiveChild("CenterHub", PrimitiveType.Sphere, root.transform, new Vector3(0f, -1.10f, 0f), new Vector3(0.24f, 0.30f, 0.24f), mats.ironMetal, true);

            // Lower wide tier (6 iron spokes + unlit wax candles) + Upper narrow tier (4 spokes)
            BuildChandelierTier(root.transform, "LowerTier", -1.04f, 0.86f, 6, mats);
            BuildChandelierTier(root.transform, "UpperTier", -0.68f, 0.48f, 4, mats);

            // Draped translucent cobwebs hanging between chandelier spokes
            CreateCobweb(root.transform, "ChandelierWeb_1", new Vector3(0.36f, -0.82f, 0.22f), Quaternion.Euler(18f, 35f, -12f), 0.72f, mats, meshes);
            CreateCobweb(root.transform, "ChandelierWeb_2", new Vector3(-0.34f, -0.88f, -0.24f), Quaternion.Euler(-15f, -140f, 16f), 0.68f, mats, meshes);

            if (withSpider)
            {
                CreateRedEyedSpider(root.transform, "ChandelierSpider", new Vector3(0.18f, -1.42f, 0.14f), 0.85f, mats, meshes, addSilkThread: true);
            }
        }

        private static void BuildChandelierTier(Transform parent, string name, float yOffset, float radius, int arms, MaterialPalette mats)
        {
            GameObject tier = new GameObject(name);
            tier.transform.SetParent(parent, false);
            tier.transform.localPosition = new Vector3(0f, yOffset, 0f);

            for (int i = 0; i < arms; i++)
            {
                float ang = (i / (float)arms) * Mathf.PI * 2f;
                float nx = Mathf.Cos(ang);
                float nz = Mathf.Sin(ang);

                GameObject arm = CreatePrimitiveChild($"Arm_{i}", PrimitiveType.Cube, tier.transform,
                    new Vector3(nx * radius * 0.5f, 0f, nz * radius * 0.5f), new Vector3(radius, 0.045f, 0.045f), mats.ironMetal, true);
                arm.transform.localRotation = Quaternion.Euler(0f, -ang * Mathf.Rad2Deg, 0f);

                // Iron ring segment connecting adjacent arms
                float nextAng = ((i + 1) / (float)arms) * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(nx * radius, 0f, nz * radius);
                Vector3 p1 = new Vector3(Mathf.Cos(nextAng) * radius, 0f, Mathf.Sin(nextAng) * radius);
                Vector3 mid = (p0 + p1) * 0.5f;
                float segLen = Vector3.Distance(p0, p1);
                GameObject rim = CreatePrimitiveChild($"Rim_{i}", PrimitiveType.Cube, tier.transform,
                    mid, new Vector3(segLen, 0.05f, 0.04f), mats.ironMetal, true);
                rim.transform.localRotation = Quaternion.LookRotation(p1 - p0, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);

                // Candle cup + unlit melted wax candle (ZERO emission/light)
                CreatePrimitiveChild($"Cup_{i}", PrimitiveType.Cylinder, tier.transform,
                    p0 + new Vector3(0f, 0.04f, 0f), new Vector3(0.12f, 0.025f, 0.12f), mats.brassMetal, true);
                CreatePrimitiveChild($"Candle_{i}", PrimitiveType.Cylinder, tier.transform,
                    p0 + new Vector3(0f, 0.14f, 0f), new Vector3(0.055f, 0.10f, 0.055f), mats.waxCandle, true);
            }
        }

        private static void PopulateHorrorAtmosphere(
            Transform floorRoot, float width, float depth, float wallHeight,
            MaterialPalette mats, LowPolyMeshes meshes, Transform playerTf, Vector3[][] ratPaths)
        {
            Transform horrorRoot = new GameObject("HorrorAtmosphere_CrittersAndWebs").transform;
            horrorRoot.SetParent(floorRoot, false);

            float cw = width * 0.5f - 0.65f;
            float cd = depth * 0.5f - 0.65f;
            float webY = wallHeight - 0.72f;

            // Upper wall corner cobwebs (4 corners + ceiling beam drapes)
            CreateCobweb(horrorRoot, "CornerWeb_SW", new Vector3(-cw, webY, -cd), Quaternion.Euler(14f, 45f, 0f), 1.45f, mats, meshes);
            CreateCobweb(horrorRoot, "CornerWeb_SE", new Vector3(cw, webY, -cd), Quaternion.Euler(14f, -45f, 0f), 1.45f, mats, meshes);
            CreateCobweb(horrorRoot, "CornerWeb_NW", new Vector3(-cw, webY, cd), Quaternion.Euler(14f, 135f, 0f), 1.45f, mats, meshes);
            CreateCobweb(horrorRoot, "CornerWeb_NE", new Vector3(cw, webY, cd), Quaternion.Euler(14f, -135f, 0f), 1.45f, mats, meshes);

            // Red-eyed 8-legged Spiders hanging/crawling near upper corner webs (unlit red eyes, ZERO scene light!)
            CreateRedEyedSpider(horrorRoot, "CornerSpider_SW", new Vector3(-cw + 0.45f, wallHeight - 1.15f, -cd + 0.45f), 0.95f, mats, meshes, addSilkThread: true);
            CreateRedEyedSpider(horrorRoot, "CornerSpider_NE", new Vector3(cw - 0.45f, wallHeight - 1.25f, cd - 0.45f), 1.05f, mats, meshes, addSilkThread: true);
            CreateRedEyedSpider(horrorRoot, "CornerSpider_NW", new Vector3(-cw + 0.50f, wallHeight - 1.08f, cd - 0.50f), 0.82f, mats, meshes, addSilkThread: true);

            // Animated Scurrying Floor Rats darting along floorboards/walls + squeaking when within 4.5 units
            if (ratPaths != null)
            {
                for (int i = 0; i < ratPaths.Length; i++)
                {
                    if (ratPaths[i] == null || ratPaths[i].Length < 2) continue;
                    CreateScurryingRat(horrorRoot, $"ScurryingRat_{i + 1}", ratPaths[i], mats, meshes, playerTf);
                }
            }
        }

        private static void CreateCobweb(Transform parent, string name, Vector3 localPos, Quaternion localRot, float scale, MaterialPalette mats, LowPolyMeshes meshes)
        {
            GameObject web = CreateMeshChild(name, meshes.cobwebQuad, parent, localPos, Vector3.one * scale, mats.cobwebMat);
            web.transform.localRotation = localRot;
            MeshRenderer mr = web.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        private static void CreateRedEyedSpider(Transform parent, string name, Vector3 localPos, float scale, MaterialPalette mats, LowPolyMeshes meshes, bool addSilkThread)
        {
            GameObject spider = new GameObject(name);
            spider.transform.SetParent(parent, false);
            spider.transform.localPosition = localPos;
            spider.transform.localScale = Vector3.one * scale;

            if (addSilkThread)
            {
                CreatePrimitiveChild("SilkThread", PrimitiveType.Cylinder, spider.transform,
                    new Vector3(0f, 0.55f, 0f), new Vector3(0.008f, 0.55f, 0.008f), mats.cobwebMat, true);
            }

            // Faceted dark abdomen + cephalothorax
            CreateMeshChild("Abdomen", meshes.spiderBody, spider.transform, new Vector3(0f, 0.02f, -0.10f), new Vector3(1.15f, 0.95f, 1.30f), mats.spiderBodyMat);
            CreateMeshChild("HeadThorax", meshes.spiderBody, spider.transform, new Vector3(0f, 0f, 0.08f), new Vector3(0.75f, 0.68f, 0.80f), mats.spiderBodyMat);

            // 8 articulated bent spider legs (4 left, 4 right)
            float[] legZ = { 0.09f, 0.03f, -0.03f, -0.09f };
            float[] legYaw = { 34f, 12f, -12f, -34f };
            for (int side = -1; side <= 1; side += 2)
            {
                for (int l = 0; l < 4; l++)
                {
                    GameObject legPivot = new GameObject($"Leg_{(side < 0 ? "L" : "R")}_{l + 1}");
                    legPivot.transform.SetParent(spider.transform, false);
                    legPivot.transform.localPosition = new Vector3(side * 0.06f, 0f, legZ[l]);
                    legPivot.transform.localRotation = Quaternion.Euler(0f, side * legYaw[l], 0f);

                    GameObject upper = CreatePrimitiveChild("Femur", PrimitiveType.Cube, legPivot.transform,
                        new Vector3(side * 0.09f, 0.05f, 0f), new Vector3(0.18f, 0.022f, 0.022f), mats.spiderBodyMat, true);
                    upper.transform.localRotation = Quaternion.Euler(0f, 0f, side * 32f);

                    GameObject lower = CreatePrimitiveChild("Tibia", PrimitiveType.Cube, legPivot.transform,
                        new Vector3(side * 0.21f, -0.02f, 0f), new Vector3(0.20f, 0.018f, 0.018f), mats.spiderBodyMat, true);
                    lower.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 54f);
                }
            }

            // Tiny UNLIT RED EYES (strictly unlit red shader — ZERO scene light emitted!)
            CreatePrimitiveChild("RedEye_L", PrimitiveType.Sphere, spider.transform, new Vector3(-0.032f, 0.018f, 0.145f), new Vector3(0.026f, 0.026f, 0.026f), mats.spiderRedEyeUnlit, true);
            CreatePrimitiveChild("RedEye_R", PrimitiveType.Sphere, spider.transform, new Vector3(0.032f, 0.018f, 0.145f), new Vector3(0.026f, 0.026f, 0.026f), mats.spiderRedEyeUnlit, true);
            CreatePrimitiveChild("RedEye_SubL", PrimitiveType.Sphere, spider.transform, new Vector3(-0.056f, 0.012f, 0.132f), new Vector3(0.016f, 0.016f, 0.016f), mats.spiderRedEyeUnlit, true);
            CreatePrimitiveChild("RedEye_SubR", PrimitiveType.Sphere, spider.transform, new Vector3(0.056f, 0.012f, 0.132f), new Vector3(0.016f, 0.016f, 0.016f), mats.spiderRedEyeUnlit, true);

            CeilingSpider comp = spider.AddComponent<CeilingSpider>();
            comp.bobAmplitude = addSilkThread ? 0.24f : 0.05f;
            comp.bobSpeed = 1.0f + (Mathf.Abs(localPos.x) % 0.6f);
        }

        private static void CreateScurryingRat(Transform parent, string name, Vector3[] localWaypoints, MaterialPalette mats, LowPolyMeshes meshes, Transform playerTf)
        {
            GameObject rat = new GameObject(name);
            rat.transform.SetParent(parent, false);
            rat.transform.localPosition = localWaypoints[0];

            // Low-poly rat body, pointed snout, rounded ears, dark eyes & animated tail
            CreateMeshChild("RatBody", meshes.ratBody, rat.transform, new Vector3(0f, 0.06f, 0f), Vector3.one, mats.ratFurMat);
            GameObject snout = CreateMeshChild("RatSnout", meshes.ratSnout, rat.transform, new Vector3(0f, 0.055f, 0.18f), Vector3.one, mats.ratFurMat);
            snout.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitiveChild("Ear_L", PrimitiveType.Sphere, rat.transform, new Vector3(-0.055f, 0.115f, 0.10f), new Vector3(0.048f, 0.052f, 0.022f), mats.ratTailPinkMat, true);
            CreatePrimitiveChild("Ear_R", PrimitiveType.Sphere, rat.transform, new Vector3(0.055f, 0.115f, 0.10f), new Vector3(0.048f, 0.052f, 0.022f), mats.ratTailPinkMat, true);
            CreatePrimitiveChild("NoseTip", PrimitiveType.Sphere, rat.transform, new Vector3(0f, 0.055f, 0.255f), new Vector3(0.026f, 0.026f, 0.026f), mats.ratTailPinkMat, true);

            Transform tailPivot = CreateLimbPivot(rat.transform, "TailPivot", new Vector3(0f, 0.05f, -0.15f));
            GameObject tail = CreatePrimitiveChild("RatTail", PrimitiveType.Cylinder, tailPivot, new Vector3(0f, -0.01f, -0.15f), new Vector3(0.020f, 0.16f, 0.020f), mats.ratTailPinkMat, true);
            tail.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            ScurryingRat comp = rat.AddComponent<ScurryingRat>();
            comp.localWaypoints = localWaypoints;
            comp.playerTransform = playerTf;
            comp.tailPivot = tailPivot;
            comp.moveSpeed = 3.1f;
            comp.squeakRadius = 4.5f;
        }

        private static TreasureChest CreateTreasureChest(
            Transform parent, string name, Vector3 localPos, ChestRewardKind rewardKind,
            string puzzleTitle, string rewardNoteTitle, string rewardNoteBody,
            MaterialPalette mats, FloorController floor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(1.7f, 1.1f, 1.15f);
            col.center = new Vector3(0f, 0.55f, 0f);

            CreatePrimitiveChild("ChestBase", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.36f, 0f), new Vector3(1.55f, 0.72f, 0.98f), mats.chestWood, true);
            CreatePrimitiveChild("BandLeft", PrimitiveType.Cube, go.transform, new Vector3(-0.52f, 0.36f, 0f), new Vector3(0.11f, 0.74f, 1.02f), mats.brassMetal, true);
            CreatePrimitiveChild("BandRight", PrimitiveType.Cube, go.transform, new Vector3(0.52f, 0.36f, 0f), new Vector3(0.11f, 0.74f, 1.02f), mats.brassMetal, true);
            CreatePrimitiveChild("FrontLock", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.54f, -0.51f), new Vector3(0.24f, 0.26f, 0.08f), mats.brassMetal, true);

            GameObject lidPivot = new GameObject("LidPivot");
            lidPivot.transform.SetParent(go.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.72f, 0.49f);
            CreatePrimitiveChild("LidBox", PrimitiveType.Cube, lidPivot.transform, new Vector3(0f, 0.13f, -0.49f), new Vector3(1.58f, 0.26f, 1.02f), mats.chestWood, true);
            CreatePrimitiveChild("LidTrim", PrimitiveType.Cube, lidPivot.transform, new Vector3(0f, 0.15f, -0.49f), new Vector3(0.16f, 0.28f, 1.06f), mats.brassMetal, true);

            GameObject treasureVis = CreatePrimitiveChild("InnerTreasure", PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.68f, 0f), new Vector3(0.50f, 0.14f, 0.36f), mats.puzzlePieceGold, true);
            treasureVis.SetActive(false);

            TreasureChest chest = go.AddComponent<TreasureChest>();
            chest.ownerFloor = floor;
            chest.lidPivot = lidPivot.transform;
            chest.innerTreasureVisual = treasureVis;
            chest.rewardKind = rewardKind;
            chest.puzzleTitle = puzzleTitle;
            chest.rewardNoteTitle = rewardNoteTitle;
            chest.rewardNoteBody = rewardNoteBody;

            return chest;
        }

        private static ExitDoor CreateExitDoorWithStaircase(
            Transform parent, string name, Vector3 localPos,
            DoorLockType lockType, string code, string lockTitle, string lockSub,
            MaterialPalette mats, FloorController floor, out StaircaseTrigger staircaseTrigger)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            BoxCollider interactCol = go.AddComponent<BoxCollider>();
            interactCol.isTrigger = true;
            interactCol.size = new Vector3(3.0f, 2.8f, 1.6f);
            interactCol.center = new Vector3(0f, 1.4f, -0.8f);

            CreatePrimitiveChild("PillarLeft", PrimitiveType.Cube, go.transform, new Vector3(-1.55f, 1.55f, 0f), new Vector3(0.42f, 3.1f, 0.85f), mats.doorFrame, false);
            CreatePrimitiveChild("PillarRight", PrimitiveType.Cube, go.transform, new Vector3(1.55f, 1.55f, 0f), new Vector3(0.42f, 3.1f, 0.85f), mats.doorFrame, false);
            CreatePrimitiveChild("Lintel", PrimitiveType.Cube, go.transform, new Vector3(0f, 3.05f, 0f), new Vector3(3.6f, 0.50f, 0.90f), mats.doorFrame, false);

            GameObject hinge = new GameObject("DoorHingePivot");
            hinge.transform.SetParent(go.transform, false);
            hinge.transform.localPosition = new Vector3(-1.32f, 0f, 0f);

            GameObject doorSlab = CreatePrimitiveChild("DoorSlab", PrimitiveType.Cube, hinge.transform, new Vector3(1.32f, 1.42f, 0f), new Vector3(2.64f, 2.84f, 0.24f), mats.doorWood, false);
            CreatePrimitiveChild("IronBandTop", PrimitiveType.Cube, hinge.transform, new Vector3(1.32f, 2.15f, -0.14f), new Vector3(2.40f, 0.14f, 0.04f), mats.ironMetal, true);
            CreatePrimitiveChild("IronBandBot", PrimitiveType.Cube, hinge.transform, new Vector3(1.32f, 0.65f, -0.14f), new Vector3(2.40f, 0.14f, 0.04f), mats.ironMetal, true);
            CreatePrimitiveChild("DoorHandle", PrimitiveType.Sphere, hinge.transform, new Vector3(2.25f, 1.25f, -0.18f), new Vector3(0.16f, 0.16f, 0.16f), mats.brassMetal, true);

            // Subtle Dim Red Point Light above the Exit Door (THE ONLY non-player light inside the mansion!)
            GameObject redFixture = CreatePrimitiveChild("RedLightFixture", PrimitiveType.Cylinder, go.transform,
                new Vector3(0f, 2.72f, -0.52f), new Vector3(0.22f, 0.08f, 0.22f), mats.ironMetal, true);
            GameObject lightObj = new GameObject("ExitDoorRedPointLight");
            lightObj.transform.SetParent(redFixture.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, -0.35f, -0.15f);
            Light redLight = lightObj.AddComponent<Light>();
            redLight.type = LightType.Point;
            redLight.color = new Color(0.85f, 0.08f, 0.06f, 1f);
            redLight.range = 3.2f;
            redLight.intensity = 3.2f;
            redLight.shadows = LightShadows.None;
            redLight.renderMode = LightRenderMode.ForcePixel;

            // Unlit 3D Descending Staircase
            GameObject stairRoot = new GameObject("DescendingStaircase");
            stairRoot.transform.SetParent(go.transform, false);
            stairRoot.transform.localPosition = new Vector3(0f, 0f, 0.4f);

            CreateWall(stairRoot.transform, "StairWall_Left", new Vector3(-1.65f, 0.8f, 3.0f), new Vector3(0.45f, 4.4f, 6.2f), mats.wall);
            CreateWall(stairRoot.transform, "StairWall_Right", new Vector3(1.65f, 0.8f, 3.0f), new Vector3(0.45f, 4.4f, 6.2f), mats.wall);
            CreateWall(stairRoot.transform, "StairWall_End", new Vector3(0f, 0.6f, 6.0f), new Vector3(3.6f, 4.6f, 0.5f), mats.darkWall);

            for (int s = 0; s < 6; s++)
            {
                float stepY = -0.22f - s * 0.28f;
                float stepZ = 0.65f + s * 0.82f;
                CreatePrimitiveChild($"Step_{s + 1}", PrimitiveType.Cube, stairRoot.transform,
                    new Vector3(0f, stepY, stepZ), new Vector3(2.85f, 0.28f, 0.84f), mats.stairsStone, true);
            }

            GameObject trigObj = new GameObject("StaircaseTransitionTrigger");
            trigObj.transform.SetParent(stairRoot.transform, false);
            trigObj.transform.localPosition = new Vector3(0f, 0.6f, 2.3f);
            BoxCollider trigCol = trigObj.AddComponent<BoxCollider>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector3(2.8f, 3.0f, 2.2f);

            staircaseTrigger = trigObj.AddComponent<StaircaseTrigger>();
            staircaseTrigger.ownerFloor = floor;

            ExitDoor door = go.AddComponent<ExitDoor>();
            door.ownerFloor = floor;
            door.lockType = lockType;
            door.requiredCode = code;
            door.lockModalTitle = lockTitle;
            door.lockModalSubtitle = lockSub;
            door.doorHingePivot = hinge.transform;
            door.doorBlockingCollider = doorSlab.GetComponent<Collider>();
            door.redDoorLight = redLight;

            return door;
        }

        private static DisorientationManager SetupDisorientation(
            GameObject floorRoot, MaterialPalette mats, LowPolyMeshes meshes, LampController playerLamp, Transform playerTf,
            Vector3[] initialPropLocalPositions, Vector3[] safeLocalSpots)
        {
            Transform propGroup = new GameObject("DisorientingDecorProps").transform;
            propGroup.SetParent(floorRoot.transform, false);

            List<Transform> props = new List<Transform>();
            for (int i = 0; i < initialPropLocalPositions.Length; i++)
            {
                Vector3 pos = initialPropLocalPositions[i];
                int style = i % 3;
                GameObject prop;
                if (style == 0)
                    prop = CreatePrimitiveChild($"ShiftingCrate_{i + 1}", PrimitiveType.Cube, propGroup, new Vector3(pos.x, 0.50f, pos.z), new Vector3(1.0f, 1.0f, 1.0f), mats.crateWood, false);
                else if (style == 1)
                    prop = CreateChair(propGroup, $"ShiftingChair_{i + 1}", pos, 35f, mats);
                else
                {
                    prop = new GameObject($"ShiftingColumn_{i + 1}");
                    prop.transform.SetParent(propGroup, false);
                    prop.transform.localPosition = new Vector3(pos.x, 0f, pos.z);
                    BoxCollider bc = prop.AddComponent<BoxCollider>();
                    bc.size = new Vector3(0.8f, 1.7f, 0.8f);
                    bc.center = new Vector3(0f, 0.85f, 0f);
                    CreateMeshChild("DebrisCol", meshes.gothicPillar, prop.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.60f, 0.45f, 0.60f), mats.darkWall);
                }
                props.Add(prop.transform);
            }

            DisorientationManager dm = floorRoot.AddComponent<DisorientationManager>();
            dm.playerLamp = playerLamp;
            dm.playerTransform = playerTf;
            dm.decorativeProps = props.ToArray();
            dm.safeLocalSpots = safeLocalSpots;
            dm.checkInterval = 2.6f;
            dm.extraSafeBuffer = 3.0f;
            return dm;
        }

        private static void BuildRoomShellWithNorthDoorway(Transform parent, float width, float depth, float wallHeight, Material floorMat, Material wallMat)
        {
            Transform shell = new GameObject("RoomShell").transform;
            shell.SetParent(parent, false);

            int tiles = 4;
            float tileW = width / tiles;
            float tileD = depth / tiles;
            Transform floorGroup = new GameObject("FloorTiles").transform;
            floorGroup.SetParent(shell, false);

            for (int ix = 0; ix < tiles; ix++)
            {
                for (int iz = 0; iz < tiles; iz++)
                {
                    float px = -width * 0.5f + tileW * (ix + 0.5f);
                    float pz = -depth * 0.5f + tileD * (iz + 0.5f);
                    CreatePrimitiveChild($"FloorTile_{ix}_{iz}", PrimitiveType.Cube, floorGroup,
                        new Vector3(px, -0.25f, pz), new Vector3(tileW - 0.03f, 0.5f, tileD - 0.03f), floorMat, false);
                }
            }

            CreatePrimitiveChild("Ceiling", PrimitiveType.Cube, shell,
                new Vector3(0f, wallHeight + 0.25f, 0f), new Vector3(width + 0.8f, 0.5f, depth + 0.8f), wallMat, true);

            float t = 0.8f;
            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;
            float y = wallHeight * 0.5f;
            float doorOpening = 3.0f;
            float sideLen = (width - doorOpening) * 0.5f;

            CreateWall(shell, "Wall_NorthLeft", new Vector3(-(doorOpening * 0.5f + sideLen * 0.5f), y, halfD), new Vector3(sideLen + t * 0.5f, wallHeight, t), wallMat);
            CreateWall(shell, "Wall_NorthRight", new Vector3(doorOpening * 0.5f + sideLen * 0.5f, y, halfD), new Vector3(sideLen + t * 0.5f, wallHeight, t), wallMat);
            CreateWall(shell, "Wall_South", new Vector3(0f, y, -halfD), new Vector3(width + t, wallHeight, t), wallMat);
            CreateWall(shell, "Wall_West", new Vector3(-halfW, y, 0f), new Vector3(t, wallHeight, depth + t), wallMat);
            CreateWall(shell, "Wall_East", new Vector3(halfW, y, 0f), new Vector3(t, wallHeight, depth + t), wallMat);
        }

        // =========================================================================
        // GRAPHICAL DARK-GOTHIC VICTORIAN UI (NO COMPASS, NO BOOST!)
        // =========================================================================

        private static UIManager BuildDarkGothicUI(
            Transform parent, UIStyleBuilder.GothicUiAssets uiAssets,
            CharacterPreviewStage previewStage, RenderTexture maleRT, RenderTexture femaleRT,
            out InventoryManager inventoryManager)
        {
            GameObject canvasObj = new GameObject("GameCanvas");
            canvasObj.transform.SetParent(parent, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            UIManager ui = canvasObj.AddComponent<UIManager>();
            ui.characterPreviewStage = previewStage;

            // 1. Mobile HUD (NO Compass, NO Boost — ornate Oil Vial, Inventory, 1P/3P, Options Gear)
            GameObject hud = CreateFullStretchPanel("HUD_Panel", canvasObj.transform, Color.clear, false);
            ui.hudPanel = hud;
            BuildGothicHud(hud.transform, ui, uiAssets, out Text notesBadgeText);

            // 2. Scrollable Dark-Gothic Notes Inventory Panel
            inventoryManager = BuildGothicInventoryPanel(canvasObj.transform, uiAssets, notesBadgeText);
            ui.inventoryManager = inventoryManager;

            // 3. Story Note Parchment Modal
            GameObject noteModal = CreateFullStretchPanel("StoryNote_Modal", canvasObj.transform, new Color(0f, 0f, 0f, 0.78f), true);
            ui.storyNotePanel = noteModal;
            BuildStoryNoteModal(noteModal.transform, ui, uiAssets);

            // 4. Floor 1: Tile-Sort Puzzle Minigame UI
            ui.tileSortPuzzleUI = BuildTileSortPuzzleUI(canvasObj.transform, uiAssets);

            // 5. Floor 2: 3-Digit Math Riddle Keypad UI
            ui.keypadPuzzleUI = BuildKeypadPuzzleUI(canvasObj.transform, uiAssets);

            // 6. Floor 3: Ancient Stone 4-Symbol Sequence Lock UI
            ui.symbolSequencePuzzleUI = BuildSymbolSequencePuzzleUI(canvasObj.transform, uiAssets);

            // 7. Options Modal (Resume / Restart Level / Save & Exit)
            GameObject optionsModal = CreateFullStretchPanel("Options_Modal", canvasObj.transform, new Color(0f, 0f, 0f, 0.82f), true);
            ui.optionsModalPanel = optionsModal;
            BuildOptionsModal(optionsModal.transform, ui, uiAssets);

            // 8. Live Cutscene Overlay + Skip Button
            GameObject introRoot = CreateFullStretchPanel("Cutscene_Overlay", canvasObj.transform, Color.clear, false);
            ui.introRootPanel = introRoot;
            BuildCutsceneOverlay(introRoot.transform, ui, uiAssets);

            // 9. Dark-Gothic Character Select with Live Rotating 3D Previews
            GameObject charSelect = CreateFullStretchPanel("CharacterSelect_GothicPanel", canvasObj.transform, new Color(0.02f, 0.02f, 0.03f, 0.96f), true);
            ui.characterSelectPanel = charSelect;
            BuildGothicCharacterSelect(charSelect.transform, ui, uiAssets, maleRT, femaleRT);

            // 10. Start Screen
            GameObject startScreen = CreateFullStretchPanel("StartScreen_Panel", canvasObj.transform, new Color(0.02f, 0.02f, 0.03f, 0.94f), true);
            ui.startScreenPanel = startScreen;
            BuildStartScreen(startScreen.transform, ui, uiAssets);

            // 11. Game Over Panel
            GameObject gameOver = CreateFullStretchPanel("GameOver_Panel", canvasObj.transform, new Color(0.02f, 0.01f, 0.01f, 0.94f), true);
            ui.gameOverPanel = gameOver;
            BuildGameOverPanel(gameOver.transform, ui, uiAssets);

            // 12. Victory Panel
            GameObject victory = CreateFullStretchPanel("Victory_Panel", canvasObj.transform, new Color(0.03f, 0.025f, 0.015f, 0.94f), true);
            ui.victoryPanel = victory;
            BuildVictoryPanel(victory.transform, ui, uiAssets);

            // 13. Transition Fade & Level Banner
            GameObject fadeObj = CreateFullStretchPanel("ScreenFade_Overlay", canvasObj.transform, Color.black, false);
            CanvasGroup fadeGroup = fadeObj.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
            ui.screenFadeGroup = fadeGroup;

            Text bannerTitle = CreateText("LevelBannerTitle", fadeObj.transform, uiAssets.font, "", 66, FontStyle.Bold,
                new Color(1f, 0.84f, 0.38f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(bannerTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(1500f, 110f));
            ui.levelBannerTitleText = bannerTitle;

            Text bannerSub = CreateText("LevelBannerSub", fadeObj.transform, uiAssets.font, "", 32, FontStyle.Italic,
                new Color(0.84f, 0.78f, 0.68f, 0.92f), TextAnchor.MiddleCenter);
            SetAnchoredRect(bannerSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(1300f, 65f));
            ui.levelBannerSubText = bannerSub;

            // Default initial visibility
            startScreen.SetActive(true);
            charSelect.SetActive(false);
            introRoot.SetActive(false);
            hud.SetActive(false);
            inventoryManager.inventoryPanelRoot.SetActive(false);
            noteModal.SetActive(false);
            optionsModal.SetActive(false);
            ui.tileSortPuzzleUI.panelRoot.SetActive(false);
            ui.keypadPuzzleUI.panelRoot.SetActive(false);
            ui.symbolSequencePuzzleUI.panelRoot.SetActive(false);
            gameOver.SetActive(false);
            victory.SetActive(false);

            return ui;
        }

        private static void BuildStartScreen(Transform parent, UIManager ui, UIStyleBuilder.GothicUiAssets uiAssets)
        {
            GameObject plaque = UIStyleBuilder.CreateBeveledPanel("TitlePlaque", parent, uiAssets.gothicFrame, new Color(0.08f, 0.08f, 0.10f, 0.92f));
            SetAnchoredRect(plaque.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 165f), new Vector2(1020f, 260f));

            Text title = CreateText("TitleText", plaque.transform, uiAssets.font, "LAST LIGHT", 112, FontStyle.Bold,
                new Color(0.98f, 0.82f, 0.38f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(960f, 130f));
            AddShadow(title.gameObject, new Color(0f, 0f, 0f, 0.8f), new Vector2(0f, -5f));

            Text subtitle = CreateText("SubtitleText", plaque.transform, uiAssets.font, "Find your way... or don't.", 38, FontStyle.Italic,
                new Color(0.78f, 0.74f, 0.66f, 0.92f), TextAnchor.MiddleCenter);
            SetAnchoredRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -66f), new Vector2(900f, 60f));

            Button continueBtn = UIStyleBuilder.CreateGothicButton("ContinueButton", parent, uiAssets.gothicButton, uiAssets.font, "CONTINUE", 38,
                new Color(0.14f, 0.14f, 0.17f, 1f), new Color(1f, 0.85f, 0.44f, 1f));
            SetAnchoredRect(continueBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(410f, 92f));
            continueBtn.gameObject.SetActive(false);
            ui.continueButton = continueBtn;

            Button startBtn = UIStyleBuilder.CreateGothicButton("StartButton", parent, uiAssets.gothicButton, uiAssets.font, "START", 42,
                new Color(0.72f, 0.50f, 0.14f, 1f), new Color(0.06f, 0.04f, 0.02f, 1f));
            SetAnchoredRect(startBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -178f), new Vector2(410f, 100f));
            ui.startButton = startBtn;

            Text hint = CreateText("ControlsHint", parent, uiAssets.font, "Move: Joystick / WASD   •   Open/Close: E   •   Spirit Flame: Q   •   Lamp ON/OFF: F   •   Hide from Bandits!", 22, FontStyle.Normal,
                new Color(0.58f, 0.54f, 0.48f, 0.80f), TextAnchor.MiddleCenter);
            SetAnchoredRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(1650f, 44f));
        }

        private static void BuildGothicCharacterSelect(Transform parent, UIManager ui, UIStyleBuilder.GothicUiAssets uiAssets, RenderTexture maleRT, RenderTexture femaleRT)
        {
            Color warmGold = new Color(1f, 0.84f, 0.40f, 1f);
            Color offWhite = new Color(0.84f, 0.79f, 0.72f, 0.92f);
            Color cardTint = new Color(0.09f, 0.09f, 0.12f, 0.98f);
            Color btnTint = new Color(0.16f, 0.15f, 0.15f, 1f);

            Text header = CreateText("CharSelectTitle", parent, uiAssets.font, "CHOOSE YOUR SURVIVOR", 62, FontStyle.Bold, warmGold, TextAnchor.MiddleCenter);
            SetAnchoredRect(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 395f), new Vector2(1300f, 84f));

            Text sub = CreateText("CharSelectSub", parent, uiAssets.font, "Who carries the antique lantern through the Forgotten Haveli?", 30, FontStyle.Italic, offWhite, TextAnchor.MiddleCenter);
            SetAnchoredRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 326f), new Vector2(1300f, 48f));

            // MALE Card with Live Rotating 3D Preview
            GameObject maleCard = UIStyleBuilder.CreateBeveledPanel("MaleCard", parent, uiAssets.gothicFrame, cardTint);
            SetAnchoredRect(maleCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-295f, -35f), new Vector2(490f, 610f));

            CreatePreviewPortrait("Male3DPreview", maleCard.transform, maleRT, new Vector2(0f, 86f), new Vector2(360f, 340f));
            Text maleTitle = CreateText("MaleTitle", maleCard.transform, uiAssets.font, "MALE", 38, FontStyle.Bold, warmGold, TextAnchor.MiddleCenter);
            SetAnchoredRect(maleTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -118f), new Vector2(420f, 48f));
            Text maleDesc = CreateText("MaleDesc", maleCard.transform, uiAssets.font, "Slate Explorer Coat • Antique Brass Lantern", 22, FontStyle.Normal, offWhite, TextAnchor.MiddleCenter);
            SetAnchoredRect(maleDesc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -162f), new Vector2(440f, 38f));

            Button maleBtn = UIStyleBuilder.CreateGothicButton("SelectMaleButton", maleCard.transform, uiAssets.gothicButton, uiAssets.font, "SELECT MALE", 28, btnTint, warmGold);
            SetAnchoredRect(maleBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -236f), new Vector2(340f, 76f));
            ui.selectMaleButton = maleBtn;

            // FEMALE Card with Live Rotating 3D Preview
            GameObject femaleCard = UIStyleBuilder.CreateBeveledPanel("FemaleCard", parent, uiAssets.gothicFrame, cardTint);
            SetAnchoredRect(femaleCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(295f, -35f), new Vector2(490f, 610f));

            CreatePreviewPortrait("Female3DPreview", femaleCard.transform, femaleRT, new Vector2(0f, 86f), new Vector2(360f, 340f));
            Text femaleTitle = CreateText("FemaleTitle", femaleCard.transform, uiAssets.font, "FEMALE", 38, FontStyle.Bold, warmGold, TextAnchor.MiddleCenter);
            SetAnchoredRect(femaleTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -118f), new Vector2(420f, 48f));
            Text femaleDesc = CreateText("FemaleDesc", femaleCard.transform, uiAssets.font, "Crimson Gothic Cloak • Antique Brass Lantern", 22, FontStyle.Normal, offWhite, TextAnchor.MiddleCenter);
            SetAnchoredRect(femaleDesc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -162f), new Vector2(440f, 38f));

            Button femaleBtn = UIStyleBuilder.CreateGothicButton("SelectFemaleButton", femaleCard.transform, uiAssets.gothicButton, uiAssets.font, "SELECT FEMALE", 28, btnTint, warmGold);
            SetAnchoredRect(femaleBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -236f), new Vector2(340f, 76f));
            ui.selectFemaleButton = femaleBtn;
        }

        private static void CreatePreviewPortrait(string name, Transform parent, RenderTexture rt, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RawImage raw = go.AddComponent<RawImage>();
            raw.texture = rt;
            SetAnchoredRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        }

        private static void BuildCutsceneOverlay(Transform parent, UIManager ui, UIStyleBuilder.GothicUiAssets uiAssets)
        {
            GameObject backdrop = CreateFullStretchPanel("CutsceneBackdrop", parent, Color.black, false);
            CanvasGroup bgGroup = backdrop.AddComponent<CanvasGroup>();
            bgGroup.alpha = 1f;
            bgGroup.blocksRaycasts = false;
            ui.introBackdropGroup = bgGroup;

            Text subtitle = CreateText("CutsceneSubtitle", backdrop.transform, uiAssets.font, "", 48, FontStyle.Italic,
                new Color(0.95f, 0.89f, 0.74f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 220f));
            ui.introStoryText = subtitle;

            Button skipBtn = UIStyleBuilder.CreateGothicButton("SkipCutsceneButton", parent, uiAssets.gothicButton, uiAssets.font, "SKIP >>", 30,
                new Color(0.14f, 0.13f, 0.13f, 0.90f), new Color(1f, 0.85f, 0.46f, 1f));
            SetAnchoredRect(skipBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-135f, -70f), new Vector2(210f, 72f));
            ui.skipIntroButton = skipBtn;
        }

        private static void BuildGothicHud(Transform parent, UIManager ui, UIStyleBuilder.GothicUiAssets uiAssets, out Text notesBadgeText)
        {
            // --- FULL-SCREEN PULSING RED BANDIT PATROL WARNING VIGNETTE (Non-blocking behind HUD controls) ---
            GameObject vignetteObj = CreateImageBox("BanditWarningVignette", parent, uiAssets.redVignette, Color.white, Image.Type.Simple);
            Image vigImg = vignetteObj.GetComponent<Image>();
            vigImg.raycastTarget = false;
            StretchToParent(vignetteObj.GetComponent<RectTransform>());
            CanvasGroup vigGroup = vignetteObj.AddComponent<CanvasGroup>();
            vigGroup.alpha = 0f;
            vigGroup.blocksRaycasts = false;
            vigGroup.interactable = false;
            ui.banditWarningVignetteGroup = vigGroup;

            Text banditWarnTxt = CreateText("BanditWarningText", vignetteObj.transform, uiAssets.font, "", 38, FontStyle.Bold,
                new Color(1f, 0.24f, 0.20f, 1f), TextAnchor.MiddleCenter);
            banditWarnTxt.raycastTarget = false;
            SetAnchoredRect(banditWarnTxt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(1380f, 74f));
            AddShadow(banditWarnTxt.gameObject, Color.black, new Vector2(2.5f, -2.5f));
            ui.banditWarningText = banditWarnTxt;

            // --- TOP-LEFT: Ornate Gothic Oil Vial Gauge (Capped at 100%) & Objective ---
            GameObject oilVial = UIStyleBuilder.CreateBeveledPanel("OrnateOilVialGauge", parent, uiAssets.oilVialFrame, Color.white);
            SetAnchoredRect(oilVial.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(235f, -66f), new Vector2(410f, 70f));

            GameObject oilTrack = CreateImageBox("OilTrack", oilVial.transform, uiAssets.whiteSolid, new Color(0.14f, 0.11f, 0.09f, 0.96f), Image.Type.Simple);
            SetAnchoredRect(oilTrack.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(-28f, 38f));

            GameObject oilFillObj = CreateImageBox("OilBarFill", oilTrack.transform, uiAssets.whiteSolid, new Color(0.98f, 0.75f, 0.22f, 1f), Image.Type.Filled);
            Image oilFillImg = oilFillObj.GetComponent<Image>();
            oilFillImg.fillMethod = Image.FillMethod.Horizontal;
            oilFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            oilFillImg.fillAmount = 0.75f;
            StretchToParent(oilFillObj.GetComponent<RectTransform>());
            ui.oilBarFill = oilFillImg;

            Text oilText = CreateText("OilValueText", oilVial.transform, uiAssets.font, "OIL  75%", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchToParent(oilText.rectTransform);
            AddShadow(oilText.gameObject, Color.black, new Vector2(1.5f, -1.5f));
            ui.oilValueText = oilText;

            Text objective = CreateText("ObjectiveText", parent, uiAssets.font, "FIND PUZZLE PIECES IN DRAWERS: 0 / 4", 23, FontStyle.Bold,
                new Color(0.96f, 0.86f, 0.56f, 0.96f), TextAnchor.MiddleLeft);
            SetAnchoredRect(objective.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(375f, -126f), new Vector2(680f, 44f));
            AddShadow(objective.gameObject, Color.black, new Vector2(1.5f, -1.5f));
            ui.objectiveText = objective;

            // --- TOP-CENTER: Beveled Floor Header ---
            GameObject floorPill = UIStyleBuilder.CreateBeveledPanel("FloorHeaderPill", parent, uiAssets.gothicFrame, new Color(0.10f, 0.10f, 0.12f, 0.88f));
            SetAnchoredRect(floorPill.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-45f, -66f), new Vector2(510f, 66f));

            Text floorText = CreateText("FloorHeaderText", floorPill.transform, uiAssets.font, "FLOOR 1  •  THE FORGOTTEN CHAMBER", 24, FontStyle.Bold,
                new Color(0.98f, 0.86f, 0.56f, 1f), TextAnchor.MiddleCenter);
            StretchToParent(floorText.rectTransform);
            ui.floorHeaderText = floorText;

            // --- TOP-CENTER TOAST NOTIFICATION ---
            GameObject toastObj = new GameObject("NotificationToast", typeof(RectTransform));
            toastObj.transform.SetParent(parent, false);
            CanvasGroup toastGroup = toastObj.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            SetAnchoredRect(toastObj.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -162f), new Vector2(1150f, 64f));

            Text toastText = CreateText("ToastText", toastObj.transform, uiAssets.font, "", 29, FontStyle.Bold,
                new Color(1f, 0.88f, 0.4f, 1f), TextAnchor.MiddleCenter);
            StretchToParent(toastText.rectTransform);
            AddShadow(toastText.gameObject, Color.black, new Vector2(2f, -2f));
            ui.notificationText = toastText;
            ui.notificationGroup = toastGroup;

            // --- TOP-RIGHT GOTHIC BAR (NO COMPASS!): INVENTORY, 1P/3P TOGGLE, GEAR OPTIONS ---
            Button optionsBtn = UIStyleBuilder.CreateGothicButton("OptionsGearButton", parent, uiAssets.gothicButton, uiAssets.font, "", 10,
                new Color(0.14f, 0.14f, 0.17f, 0.92f), Color.white);
            SetAnchoredRect(optionsBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-68f, -66f), new Vector2(76f, 68f));
            GameObject gearImgObj = CreateImageBox("GearGraphic", optionsBtn.transform, uiAssets.gearIcon, Color.white, Image.Type.Simple);
            gearImgObj.GetComponent<Image>().raycastTarget = false;
            SetAnchoredRect(gearImgObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
            ui.optionsButton = optionsBtn;

            Button camToggleBtn = UIStyleBuilder.CreateGothicButton("CameraToggleButton", parent, uiAssets.gothicButton, uiAssets.font, "1P / 3P  [1P]", 22,
                new Color(0.14f, 0.14f, 0.17f, 0.92f), new Color(0.98f, 0.86f, 0.52f, 1f));
            SetAnchoredRect(camToggleBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-216f, -66f), new Vector2(196f, 68f));
            ui.cameraToggleButton = camToggleBtn;
            ui.cameraToggleLabel = camToggleBtn.GetComponentInChildren<Text>();

            Button invBtn = UIStyleBuilder.CreateGothicButton("InventoryNotesButton", parent, uiAssets.gothicButton, uiAssets.font, "INVENTORY (0)", 22,
                new Color(0.14f, 0.14f, 0.17f, 0.92f), new Color(0.98f, 0.86f, 0.52f, 1f));
            SetAnchoredRect(invBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-432f, -66f), new Vector2(212f, 68f));
            ui.inventoryButton = invBtn;
            notesBadgeText = invBtn.GetComponentInChildren<Text>();

            // --- BOTTOM-LEFT: Floating Virtual Joystick ---
            GameObject joyZone = new GameObject("VirtualJoystick_Zone");
            joyZone.transform.SetParent(parent, false);
            Image joyZoneImg = joyZone.AddComponent<Image>();
            joyZoneImg.color = new Color(0f, 0f, 0f, 0f);
            joyZoneImg.raycastTarget = true;
            RectTransform joyZoneRt = joyZone.GetComponent<RectTransform>();
            SetAnchoredRect(joyZoneRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(280f, 230f), new Vector2(520f, 420f));

            GameObject joyBg = CreateImageBox("JoystickBackground", joyZone.transform, uiAssets.circleRing, new Color(0.90f, 0.74f, 0.36f, 0.80f), Image.Type.Simple);
            joyBg.GetComponent<Image>().raycastTarget = false;
            CanvasGroup joyVisualGroup = joyBg.AddComponent<CanvasGroup>();
            joyVisualGroup.alpha = 0.45f;
            joyVisualGroup.blocksRaycasts = false;
            SetAnchoredRect(joyBg.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-85f, -40f), new Vector2(210f, 210f));

            GameObject joyInnerTint = CreateImageBox("JoystickInnerFill", joyBg.transform, uiAssets.circleFill, new Color(0.08f, 0.07f, 0.06f, 0.46f), Image.Type.Simple);
            joyInnerTint.GetComponent<Image>().raycastTarget = false;
            StretchToParent(joyInnerTint.GetComponent<RectTransform>());

            GameObject joyHandle = CreateImageBox("JoystickHandle", joyBg.transform, uiAssets.circleFill, new Color(0.94f, 0.76f, 0.34f, 0.92f), Image.Type.Simple);
            joyHandle.GetComponent<Image>().raycastTarget = false;
            SetAnchoredRect(joyHandle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 88f));

            VirtualJoystick joystick = joyZone.AddComponent<VirtualJoystick>();
            joystick.touchArea = joyZoneRt;
            joystick.background = joyBg.GetComponent<RectTransform>();
            joystick.handle = joyHandle.GetComponent<RectTransform>();
            joystick.visualGroup = joyVisualGroup;
            joystick.handleRange = 85f;
            ui.virtualJoystick = joystick;

            // --- BOTTOM-RIGHT: Quick "LAMP: ON/OFF" & "FLAME: WARM/SPIRIT" Toggle Buttons ---
            Button lampPowerBtn = UIStyleBuilder.CreateGothicButton("LampPowerToggleButton", parent, uiAssets.gothicButton, uiAssets.font, "LAMP: ON", 23,
                new Color(0.42f, 0.28f, 0.10f, 0.94f), new Color(1.0f, 0.88f, 0.48f, 1f));
            SetAnchoredRect(lampPowerBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-145f, 286f), new Vector2(226f, 74f));
            ui.lampPowerButton = lampPowerBtn;
            ui.lampPowerLabel = lampPowerBtn.GetComponentInChildren<Text>();

            Button flameToggleBtn = UIStyleBuilder.CreateGothicButton("FlameModeToggleButton", parent, uiAssets.gothicButton, uiAssets.font, "FLAME: WARM", 22,
                new Color(0.24f, 0.18f, 0.10f, 0.94f), new Color(1.0f, 0.85f, 0.44f, 1f));
            SetAnchoredRect(flameToggleBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-396f, 286f), new Vector2(246f, 74f));
            ui.flameToggleButton = flameToggleBtn;
            ui.flameToggleLabel = flameToggleBtn.GetComponentInChildren<Text>();

            // --- BOTTOM-RIGHT: Broken Wood Plank Contextual "OPEN / CLOSE" Button ---
            Button interactBtn = UIStyleBuilder.CreateGothicButton("ContextualInteractButton", parent, uiAssets.brokenWoodPlank, uiAssets.font, "OPEN", 27,
                Color.white, new Color(0.98f, 0.92f, 0.76f, 1f));
            Image plankImg = interactBtn.GetComponent<Image>();
            if (plankImg != null) plankImg.type = Image.Type.Simple;
            SetAnchoredRect(interactBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-235f, 150f), new Vector2(360f, 118f));
            Text interactTxt = interactBtn.GetComponentInChildren<Text>();
            if (interactTxt != null) AddShadow(interactTxt.gameObject, new Color(0f, 0f, 0f, 0.92f), new Vector2(2f, -2f));
            interactBtn.gameObject.SetActive(false);

            ui.interactButton = interactBtn;
            ui.interactButtonLabel = interactTxt;
        }

        private static InventoryManager BuildGothicInventoryPanel(Transform canvasTransform, UIStyleBuilder.GothicUiAssets uiAssets, Text badgeText)
        {
            GameObject root = CreateFullStretchPanel("NotesInventory_Modal", canvasTransform, new Color(0f, 0f, 0f, 0.84f), true);

            GameObject card = UIStyleBuilder.CreateBeveledPanel("InventoryCard", root.transform, uiAssets.gothicFrame, new Color(0.09f, 0.09f, 0.12f, 0.98f));
            SetAnchoredRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 790f));

            Text header = CreateText("InventoryHeader", card.transform, uiAssets.font, "NOTES & CLUES INVENTORY", 40, FontStyle.Bold,
                new Color(1f, 0.84f, 0.40f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(1000f, 58f));

            // Left Scrollable List
            GameObject scrollObj = UIStyleBuilder.CreateBeveledPanel("NotesScrollView", card.transform, uiAssets.gothicFrame, new Color(0.06f, 0.06f, 0.08f, 1f));
            SetAnchoredRect(scrollObj.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(285f, -15f), new Vector2(500f, 540f));
            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollObj.transform, false);
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(16f, 16f);
            vpRt.offsetMax = new Vector2(-16f, -16f);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRt;
            scrollRect.content = contentRt;

            Text emptyText = CreateText("EmptyListText", scrollObj.transform, uiAssets.font, "No clues collected yet.", 26, FontStyle.Italic,
                new Color(0.55f, 0.52f, 0.48f, 1f), TextAnchor.MiddleCenter);
            StretchToParent(emptyText.rectTransform);

            // Right Reading Pane (Crumpled Aged Parchment Sheet with Dark Espresso Ink)
            GameObject readPane = CreateImageBox("NoteReadingPane", card.transform, uiAssets.crumpledPaper, Color.white, Image.Type.Simple);
            SetAnchoredRect(readPane.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-425f, -15f), new Vector2(770f, 540f));

            Text selTitle = CreateText("SelectedNoteTitle", readPane.transform, uiAssets.font, "NO NOTES COLLECTED", 31, FontStyle.Bold,
                new Color(0.20f, 0.11f, 0.05f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(selTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(660f, 56f));

            GameObject paneDivider = CreateImageBox("PaneDivider", readPane.transform, uiAssets.whiteSolid, new Color(0.38f, 0.25f, 0.14f, 0.55f), Image.Type.Simple);
            SetAnchoredRect(paneDivider.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(560f, 3f));

            Text selBody = CreateText("SelectedNoteBody", readPane.transform, uiAssets.font, "", 26, FontStyle.Italic,
                new Color(0.14f, 0.08f, 0.04f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(selBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(640f, 350f));

            Button closeBtn = UIStyleBuilder.CreateGothicButton("CloseInventoryBtn", card.transform, uiAssets.gothicButton, uiAssets.font, "CLOSE  (OIL PAUSED)", 26,
                new Color(0.18f, 0.16f, 0.14f, 1f), new Color(1f, 0.86f, 0.48f, 1f));
            SetAnchoredRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(380f, 68f));

            InventoryManager inv = root.AddComponent<InventoryManager>();
            inv.inventoryPanelRoot = root;
            inv.listContentRoot = contentRt;
            inv.selectedNoteTitleText = selTitle;
            inv.selectedNoteBodyText = selBody;
            inv.emptyStateText = emptyText;
            inv.noteCountBadgeText = badgeText;
            inv.closeButton = closeBtn;
            inv.buttonSprite = uiAssets.gothicButton;
            inv.uiFont = uiAssets.font;

            return inv;
        }

        private static SymbolSequencePuzzleUI BuildSymbolSequencePuzzleUI(Transform canvasTransform, UIStyleBuilder.GothicUiAssets uiAssets)
        {
            GameObject root = CreateFullStretchPanel("SymbolStoneLock_Modal", canvasTransform, new Color(0f, 0f, 0f, 0.84f), true);

            GameObject card = UIStyleBuilder.CreateBeveledPanel("StoneLockCard", root.transform, uiAssets.gothicFrame, new Color(0.09f, 0.09f, 0.11f, 0.98f));
            SetAnchoredRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 760f));

            Text title = CreateText("StoneLockTitle", card.transform, uiAssets.font, "ANCIENT STONE LOCK — FINAL GATE", 40, FontStyle.Bold,
                new Color(1f, 0.84f, 0.38f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(980f, 56f));

            Text sub = CreateText("StoneLockSub", card.transform, uiAssets.font, "Invoke the 4 gothic symbols in the order revealed by the Sanctum Plaques", 24, FontStyle.Italic,
                new Color(0.80f, 0.76f, 0.68f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(980f, 40f));

            // Top row: 4 Entered Sequence Slots
            Image[] slotFrames = new Image[4];
            Image[] slotIcons = new Image[4];
            Text[] slotLabels = new Text[4];

            float slotStartX = -315f;
            float slotStep = 210f;
            for (int i = 0; i < 4; i++)
            {
                GameObject slot = UIStyleBuilder.CreateBeveledPanel($"SeqSlot_{i + 1}", card.transform, uiAssets.gothicFrame, new Color(0.10f, 0.10f, 0.12f, 1f));
                SetAnchoredRect(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(slotStartX + i * slotStep, 128f), new Vector2(178f, 152f));
                slotFrames[i] = slot.GetComponent<Image>();

                GameObject iconObj = CreateImageBox("SlotIcon", slot.transform, uiAssets.symbolSkull, Color.white, Image.Type.Simple);
                SetAnchoredRect(iconObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(78f, 78f));
                Image ic = iconObj.GetComponent<Image>();
                ic.enabled = false;
                slotIcons[i] = ic;

                Text lbl = CreateText("SlotLabel", slot.transform, uiAssets.font, $"STEP {i + 1}", 22, FontStyle.Bold, new Color(0.55f, 0.52f, 0.48f, 1f), TextAnchor.MiddleCenter);
                SetAnchoredRect(lbl.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(160f, 34f));
                slotLabels[i] = lbl;
            }

            Text feedback = CreateText("StoneFeedback", card.transform, uiAssets.font, "Select the 4 ancient symbols in exact order", 26, FontStyle.Bold,
                new Color(0.84f, 0.78f, 0.68f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(feedback.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(960f, 44f));

            // Bottom row: 4 Carved Stone Symbol Buttons (Skull, Moon, Key, Crown)
            Sprite[] symSprites = { uiAssets.symbolSkull, uiAssets.symbolMoon, uiAssets.symbolKey, uiAssets.symbolCrown };
            string[] symNames = { "SKULL", "MOON", "KEY", "CROWN" };
            Button[] symButtons = new Button[4];

            for (int i = 0; i < 4; i++)
            {
                Button sb = UIStyleBuilder.CreateGothicButton($"SymBtn_{symNames[i]}", card.transform, uiAssets.gothicButton, uiAssets.font, "", 10,
                    new Color(0.18f, 0.17f, 0.19f, 1f), Color.white);
                SetAnchoredRect(sb.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(slotStartX + i * slotStep, -125f), new Vector2(182f, 182f));

                GameObject icObj = CreateImageBox("Icon", sb.transform, symSprites[i], Color.white, Image.Type.Simple);
                icObj.GetComponent<Image>().raycastTarget = false;
                SetAnchoredRect(icObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(96f, 96f));

                Text nameTxt = CreateText("SymName", sb.transform, uiAssets.font, symNames[i], 24, FontStyle.Bold, new Color(1f, 0.86f, 0.44f, 1f), TextAnchor.MiddleCenter);
                nameTxt.raycastTarget = false;
                SetAnchoredRect(nameTxt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(165f, 34f));

                symButtons[i] = sb;
            }

            Button resetBtn = UIStyleBuilder.CreateGothicButton("ResetSymbolsBtn", card.transform, uiAssets.gothicButton, uiAssets.font, "RESET", 26,
                new Color(0.42f, 0.16f, 0.14f, 1f), Color.white);
            SetAnchoredRect(resetBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 62f), new Vector2(240f, 68f));

            Button closeBtn = UIStyleBuilder.CreateGothicButton("CloseStoneLockBtn", card.transform, uiAssets.gothicButton, uiAssets.font, "CLOSE", 26,
                new Color(0.18f, 0.17f, 0.16f, 1f), new Color(1f, 0.86f, 0.48f, 1f));
            SetAnchoredRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150f, 62f), new Vector2(240f, 68f));

            SymbolSequencePuzzleUI uiComp = root.AddComponent<SymbolSequencePuzzleUI>();
            uiComp.panelRoot = root;
            uiComp.titleText = title;
            uiComp.subtitleText = sub;
            uiComp.feedbackText = feedback;
            uiComp.slotFrames = slotFrames;
            uiComp.slotIconImages = slotIcons;
            uiComp.slotLabels = slotLabels;
            uiComp.symbolButtons = symButtons;
            uiComp.symbolSprites = symSprites;
            uiComp.resetButton = resetBtn;
            uiComp.closeButton = closeBtn;

            return uiComp;
        }

        private static void BuildOptionsModal(Transform parent, UIManager ui, UIStyleBuilder.GothicUiAssets uiAssets)
        {
            GameObject card = UIStyleBuilder.CreateBeveledPanel("OptionsCard", parent, uiAssets.gothicFrame, new Color(0.09f, 0.09f, 0.12f, 0.98f));
            SetAnchoredRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(590f, 550f));

            Text title = CreateText("OptionsTitle", card.transform, uiAssets.font, "OPTIONS", 46, FontStyle.Bold,
                new Color(1f, 0.84f, 0.40f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(480f, 62f));

            Text sub = CreateText("OptionsSub", card.transform, uiAssets.font, "Game & Lantern Oil Paused", 24, FontStyle.Italic,
                new Color(0.72f, 0.68f, 0.62f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(460f, 40f));

            Button resumeBtn = UIStyleBuilder.CreateGothicButton("OptionsResumeBtn", card.transform, uiAssets.gothicButton, uiAssets.font, "RESUME", 30,
                new Color(0.76f, 0.54f, 0.16f, 1f), new Color(0.06f, 0.04f, 0.02f, 1f));
            SetAnchoredRect(resumeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(410f, 82f));
            ui.optionsResumeButton = resumeBtn;

            Button restartBtn = UIStyleBuilder.CreateGothicButton("OptionsRestartBtn", card.transform, uiAssets.gothicButton, uiAssets.font, "RESTART LEVEL", 28,
                new Color(0.16f, 0.16f, 0.18f, 1f), new Color(1f, 0.85f, 0.45f, 1f));
            SetAnchoredRect(restartBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(410f, 82f));
            ui.optionsRestartButton = restartBtn;

            Button saveExitBtn = UIStyleBuilder.CreateGothicButton("OptionsSaveExitBtn", card.transform, uiAssets.gothicButton, uiAssets.font, "SAVE & EXIT", 28,
                new Color(0.32f, 0.14f, 0.13f, 1f), new Color(0.98f, 0.88f, 0.78f, 1f));
            SetAnchoredRect(saveExitBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -162f), new Vector2(410f, 82f));
            ui.optionsSaveExitButton = saveExitBtn;
        }

        private static void BuildStoryNoteModal(Transform parent, UIManager ui, UIStyleBuilder.GothicUiAssets uiAssets)
        {
            // Crumpled Aged Sepia Parchment with Fold Creases & Torn/Burnt Edges
            GameObject card = CreateImageBox("ParchmentCard", parent, uiAssets.crumpledPaper, Color.white, Image.Type.Simple);
            SetAnchoredRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1020f, 700f));

            Text title = CreateText("NoteTitle", card.transform, uiAssets.font, "TORN JOURNAL PAGE", 36, FontStyle.Bold,
                new Color(0.18f, 0.10f, 0.04f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(840f, 60f));
            ui.noteTitleText = title;

            GameObject divider = CreateImageBox("Divider", card.transform, uiAssets.whiteSolid, new Color(0.42f, 0.28f, 0.16f, 0.65f), Image.Type.Simple);
            SetAnchoredRect(divider.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(700f, 3f));

            Text body = CreateText("NoteBody", card.transform, uiAssets.font, "", 28, FontStyle.Italic,
                new Color(0.13f, 0.08f, 0.04f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(800f, 340f));
            ui.noteBodyText = body;

            Button closeBtn = UIStyleBuilder.CreateGothicButton("CloseNoteButton", card.transform, uiAssets.gothicButton, uiAssets.font, "SAVE TO INVENTORY & CLOSE", 24,
                new Color(0.20f, 0.13f, 0.08f, 1f), new Color(0.98f, 0.90f, 0.72f, 1f));
            SetAnchoredRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(460f, 74f));
            ui.noteCloseButton = closeBtn;
        }

        private static TileSortPuzzleUI BuildTileSortPuzzleUI(Transform canvasTransform, UIStyleBuilder.GothicUiAssets uiAssets)
        {
            GameObject root = CreateFullStretchPanel("TileSortPuzzle_Modal", canvasTransform, new Color(0f, 0f, 0f, 0.82f), true);

            GameObject card = UIStyleBuilder.CreateBeveledPanel("TileSortCard", root.transform, uiAssets.gothicFrame, new Color(0.09f, 0.09f, 0.11f, 0.98f));
            SetAnchoredRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 640f));

            Text title = CreateText("PuzzleTitle", card.transform, uiAssets.font, "RELIC CHEST LOCK", 42, FontStyle.Bold,
                new Color(1f, 0.84f, 0.38f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(940f, 60f));

            Text status = CreateText("PuzzleStatus", card.transform, uiAssets.font, "Tap two tiles to swap them into order:  1  →  2  →  3  →  4", 27, FontStyle.Normal,
                new Color(0.84f, 0.80f, 0.72f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(960f, 50f));

            Button[] tileBtns = new Button[4];
            Image[] tileBgs = new Image[4];
            Text[] tileTxts = new Text[4];

            float startX = -330f;
            float spacing = 220f;
            for (int i = 0; i < 4; i++)
            {
                Button tb = UIStyleBuilder.CreateGothicButton($"TileSlot_{i + 1}", card.transform, uiAssets.gothicButton, uiAssets.font, $"{i + 1}", 30,
                    new Color(0.32f, 0.22f, 0.14f, 1f), Color.white);
                SetAnchoredRect(tb.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(startX + i * spacing, -10f), new Vector2(190f, 225f));
                tileBtns[i] = tb;
                tileBgs[i] = tb.GetComponent<Image>();
                tileTxts[i] = tb.GetComponentInChildren<Text>();
            }

            Button closeBtn = UIStyleBuilder.CreateGothicButton("CloseTilePuzzle", card.transform, uiAssets.gothicButton, uiAssets.font, "CLOSE", 26,
                new Color(0.18f, 0.17f, 0.16f, 1f), new Color(1f, 0.86f, 0.48f, 1f));
            SetAnchoredRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(260f, 72f));

            TileSortPuzzleUI puzzle = root.AddComponent<TileSortPuzzleUI>();
            puzzle.panelRoot = root;
            puzzle.titleText = title;
            puzzle.statusText = status;
            puzzle.tileButtons = tileBtns;
            puzzle.tileBackgrounds = tileBgs;
            puzzle.tileLabels = tileTxts;
            puzzle.closeButton = closeBtn;

            return puzzle;
        }

        private static KeypadPuzzleUI BuildKeypadPuzzleUI(Transform canvasTransform, UIStyleBuilder.GothicUiAssets uiAssets)
        {
            GameObject root = CreateFullStretchPanel("KeypadPuzzle_Modal", canvasTransform, new Color(0f, 0f, 0f, 0.82f), true);

            GameObject card = UIStyleBuilder.CreateBeveledPanel("KeypadCard", root.transform, uiAssets.gothicFrame, new Color(0.09f, 0.09f, 0.11f, 0.98f));
            SetAnchoredRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(690f, 850f));

            Text title = CreateText("KeypadTitle", card.transform, uiAssets.font, "EXIT DOOR KEYPAD", 36, FontStyle.Bold,
                new Color(1f, 0.84f, 0.38f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(620f, 50f));

            Text sub = CreateText("KeypadSub", card.transform, uiAssets.font, "Solve the 3 randomized math riddles", 23, FontStyle.Italic,
                new Color(0.75f, 0.72f, 0.68f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(620f, 40f));

            GameObject displayBg = UIStyleBuilder.CreateBeveledPanel("CodeDisplayBg", card.transform, uiAssets.gothicFrame, new Color(0.05f, 0.09f, 0.07f, 1f));
            SetAnchoredRect(displayBg.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(460f, 88f));

            Text displayText = CreateText("CodeDisplay", displayBg.transform, uiAssets.font, "_   _   _", 52, FontStyle.Bold,
                new Color(0.45f, 1f, 0.68f, 1f), TextAnchor.MiddleCenter);
            StretchToParent(displayText.rectTransform);

            Text feedback = CreateText("FeedbackText", card.transform, uiAssets.font, "Enter the 3-digit code", 23, FontStyle.Bold,
                new Color(0.84f, 0.80f, 0.72f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(feedback.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -246f), new Vector2(610f, 40f));

            Button[] digits = new Button[10];
            float colSpacing = 152f;
            float rowSpacing = 108f;
            float topRowY = 92f;

            for (int n = 1; n <= 9; n++)
            {
                int row = (n - 1) / 3;
                int col = (n - 1) % 3;
                Button b = UIStyleBuilder.CreateGothicButton($"Digit_{n}", card.transform, uiAssets.gothicButton, uiAssets.font, n.ToString(), 36,
                    new Color(0.18f, 0.19f, 0.22f, 1f), Color.white);
                SetAnchoredRect(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((col - 1) * colSpacing, topRowY - row * rowSpacing), new Vector2(130f, 90f));
                digits[n] = b;
            }

            float botY = topRowY - 3 * rowSpacing;
            Button clearBtn = UIStyleBuilder.CreateGothicButton("ClearBtn", card.transform, uiAssets.gothicButton, uiAssets.font, "CLR", 28,
                new Color(0.48f, 0.16f, 0.14f, 1f), Color.white);
            SetAnchoredRect(clearBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-colSpacing, botY), new Vector2(130f, 90f));

            Button btn0 = UIStyleBuilder.CreateGothicButton("Digit_0", card.transform, uiAssets.gothicButton, uiAssets.font, "0", 36,
                new Color(0.18f, 0.19f, 0.22f, 1f), Color.white);
            SetAnchoredRect(btn0.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, botY), new Vector2(130f, 90f));
            digits[0] = btn0;

            Button enterBtn = UIStyleBuilder.CreateGothicButton("EnterBtn", card.transform, uiAssets.gothicButton, uiAssets.font, "OK", 30,
                new Color(0.16f, 0.50f, 0.30f, 1f), Color.white);
            SetAnchoredRect(enterBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(colSpacing, botY), new Vector2(130f, 90f));

            Button closeBtn = UIStyleBuilder.CreateGothicButton("CloseKeypad", card.transform, uiAssets.gothicButton, uiAssets.font, "CLOSE", 25,
                new Color(0.18f, 0.17f, 0.16f, 1f), new Color(1f, 0.86f, 0.48f, 1f));
            SetAnchoredRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(240f, 64f));

            KeypadPuzzleUI keypad = root.AddComponent<KeypadPuzzleUI>();
            keypad.panelRoot = root;
            keypad.titleText = title;
            keypad.subtitleText = sub;
            keypad.codeDisplayText = displayText;
            keypad.feedbackText = feedback;
            keypad.digitButtons = digits;
            keypad.clearButton = clearBtn;
            keypad.enterButton = enterBtn;
            keypad.closeButton = closeBtn;

            return keypad;
        }

        private static void BuildGameOverPanel(Transform parent, UIManager ui, UIStyleBuilder.GothicUiAssets uiAssets)
        {
            GameObject card = UIStyleBuilder.CreateBeveledPanel("GameOverCard", parent, uiAssets.gothicFrame, new Color(0.08f, 0.06f, 0.06f, 0.96f));
            SetAnchoredRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 500f));

            Text title = CreateText("GameOverTitle", card.transform, uiAssets.font, "You are lost.", 92, FontStyle.Bold,
                new Color(0.92f, 0.26f, 0.22f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(820f, 130f));
            ui.gameOverTitleText = title;

            Text sub = CreateText("GameOverSub", card.transform, uiAssets.font, "Your lantern died in the Forgotten Haveli.", 32, FontStyle.Italic,
                new Color(0.76f, 0.70f, 0.65f, 0.92f), TextAnchor.MiddleCenter);
            SetAnchoredRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(800f, 60f));
            ui.gameOverSubText = sub;

            Button retryBtn = UIStyleBuilder.CreateGothicButton("RetryFloorButton", card.transform, uiAssets.gothicButton, uiAssets.font, "RETRY FLOOR", 36,
                new Color(0.76f, 0.52f, 0.16f, 1f), new Color(0.06f, 0.04f, 0.02f, 1f));
            SetAnchoredRect(retryBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(400f, 96f));
            ui.retryFloorButton = retryBtn;
        }

        private static void BuildVictoryPanel(Transform parent, UIManager ui, UIStyleBuilder.GothicUiAssets uiAssets)
        {
            GameObject card = UIStyleBuilder.CreateBeveledPanel("VictoryCard", parent, uiAssets.gothicFrame, new Color(0.09f, 0.08f, 0.07f, 0.96f));
            SetAnchoredRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 620f));

            Text title = CreateText("VictoryTitle", card.transform, uiAssets.font, "HAVELI ESCAPED", 84, FontStyle.Bold,
                new Color(1f, 0.84f, 0.36f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 185f), new Vector2(920f, 120f));

            Text sub = CreateText("VictorySubtitle", card.transform, uiAssets.font, "Your antique lantern carried you back into the dawn.", 32, FontStyle.Italic,
                new Color(0.85f, 0.80f, 0.72f, 0.92f), TextAnchor.MiddleCenter);
            SetAnchoredRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 96f), new Vector2(900f, 54f));

            Text timeTxt = CreateText("VictoryTimeText", card.transform, uiAssets.font, "Time Taken:  00:00.0", 38, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            SetAnchoredRect(timeTxt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(820f, 58f));
            ui.victoryTimeText = timeTxt;

            Text oilTxt = CreateText("VictoryOilText", card.transform, uiAssets.font, "Oil Remaining:  75%", 38, FontStyle.Bold,
                new Color(1f, 0.82f, 0.35f, 1f), TextAnchor.MiddleCenter);
            SetAnchoredRect(oilTxt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(820f, 58f));
            ui.victoryOilText = oilTxt;

            Button playAgainBtn = UIStyleBuilder.CreateGothicButton("PlayAgainButton", card.transform, uiAssets.gothicButton, uiAssets.font, "PLAY AGAIN", 36,
                new Color(0.76f, 0.54f, 0.16f, 1f), new Color(0.06f, 0.04f, 0.02f, 1f));
            SetAnchoredRect(playAgainBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -182f), new Vector2(400f, 96f));
            ui.playAgainButton = playAgainBtn;
        }

        private static void CreateEventSystem(Transform parent)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.transform.SetParent(parent, false);
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = esObj.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
        }

        // =========================================================================
        // LOW-POLY MESH ASSETS & STRICT NON-EMISSIVE MATERIALS
        // =========================================================================

        private static LowPolyMeshes CreateLowPolyMeshes()
        {
            return new LowPolyMeshes
            {
                torsoMale = SaveMeshAsset("LP_TorsoMale.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_TorsoMale", 6, 0.20f, 0.13f, 0.27f, 0.16f, 0.62f)),
                torsoFemale = SaveMeshAsset("LP_TorsoFemale.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_TorsoFemale", 6, 0.16f, 0.11f, 0.21f, 0.13f, 0.52f)),
                skirtCloak = SaveMeshAsset("LP_SkirtCloak.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_SkirtCloak", 8, 0.28f, 0.24f, 0.17f, 0.13f, 0.44f)),
                head = SaveMeshAsset("LP_Head.asset", CharacterBuilder.CreateFacetedEllipsoidMesh("LP_Head", 7, 4, new Vector3(0.13f, 0.155f, 0.14f))),
                hairCap = SaveMeshAsset("LP_HairCap.asset", CharacterBuilder.CreateFacetedEllipsoidMesh("LP_HairCap", 7, 3, new Vector3(0.145f, 0.105f, 0.155f))),
                hairPonytail = SaveMeshAsset("LP_HairPonytail.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_HairPonytail", 6, 0.04f, 0.04f, 0.075f, 0.075f, 0.34f)),
                turban = SaveMeshAsset("LP_Turban.asset", CharacterBuilder.CreateFacetedEllipsoidMesh("LP_Turban", 8, 3, new Vector3(0.175f, 0.115f, 0.18f))),
                bandana = SaveMeshAsset("LP_Bandana.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_Bandana", 6, 0.12f, 0.12f, 0.145f, 0.145f, 0.13f)),
                limb = SaveMeshAsset("LP_Limb.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_Limb", 6, 0.068f, 0.068f, 0.088f, 0.088f, 0.44f)),
                boot = SaveMeshAsset("LP_Boot.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_Boot", 6, 0.08f, 0.13f, 0.065f, 0.08f, 0.12f)),
                club = SaveMeshAsset("LP_Club.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_Club", 6, 0.032f, 0.032f, 0.082f, 0.082f, 0.78f)),
                lanternGlass = SaveMeshAsset("LP_LanternGlass.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_LanternGlass", 8, 0.085f, 0.085f, 0.072f, 0.072f, 0.24f)),
                lanternCap = SaveMeshAsset("LP_LanternCap.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_LanternCap", 8, 0.12f, 0.12f, 0.035f, 0.035f, 0.11f)),
                lanternBase = SaveMeshAsset("LP_LanternBase.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_LanternBase", 8, 0.125f, 0.125f, 0.105f, 0.105f, 0.06f)),
                pineCone = SaveMeshAsset("LP_PineCone.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_PineCone", 7, 1.45f, 1.45f, 0.04f, 0.04f, 2.2f)),
                treeTrunk = SaveMeshAsset("LP_TreeTrunk.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_TreeTrunk", 7, 0.28f, 0.28f, 0.18f, 0.18f, 2.2f)),
                tent = SaveMeshAsset("LP_Tent.asset", CharacterBuilder.CreateWedgeTentMesh("LP_Tent", 2.6f, 1.95f, 3.2f)),
                rock = SaveMeshAsset("LP_Rock.asset", CharacterBuilder.CreateFacetedEllipsoidMesh("LP_Rock", 6, 3, new Vector3(0.24f, 0.16f, 0.22f))),
                flameCone = SaveMeshAsset("LP_FlameCone.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_FlameCone", 5, 0.26f, 0.26f, 0.02f, 0.02f, 0.72f)),
                gothicPillar = SaveMeshAsset("LP_GothicPillar.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_GothicPillar", 8, 0.56f, 0.56f, 0.48f, 0.48f, 3.6f)),
                cobwebQuad = SaveMeshAsset("LP_CobwebQuad.asset", CharacterBuilder.CreateDoubleSidedWebQuad("LP_CobwebQuad", 1.0f)),
                ratBody = SaveMeshAsset("LP_RatBody.asset", CharacterBuilder.CreateFacetedEllipsoidMesh("LP_RatBody", 6, 3, new Vector3(0.085f, 0.065f, 0.165f))),
                ratSnout = SaveMeshAsset("LP_RatSnout.asset", CharacterBuilder.CreateTaperedPrismMesh("LP_RatSnout", 5, 0.062f, 0.052f, 0.015f, 0.015f, 0.14f)),
                spiderBody = SaveMeshAsset("LP_SpiderBody.asset", CharacterBuilder.CreateFacetedEllipsoidMesh("LP_SpiderBody", 6, 3, new Vector3(0.085f, 0.072f, 0.095f)))
            };
        }

        private static Mesh SaveMeshAsset(string fileName, Mesh mesh)
        {
            string path = $"{MeshesDir}/{fileName}";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                existing.SetVertices(mesh.vertices);
                if (mesh.uv != null && mesh.uv.Length == mesh.vertexCount)
                {
                    existing.uv = mesh.uv;
                }
                existing.SetTriangles(mesh.triangles, 0);
                existing.RecalculateNormals();
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static MaterialPalette CreateStrictDarkMaterials(UIStyleBuilder.GothicUiAssets uiAssets)
        {
            return new MaterialPalette
            {
                floor1 = CreateMat("Mat_Floor1.mat", new Color(0.24f, 0.22f, 0.20f, 1f), 0.04f, 0.20f),
                floor2 = CreateMat("Mat_Floor2.mat", new Color(0.20f, 0.20f, 0.22f, 1f), 0.05f, 0.18f),
                floor3 = CreateMat("Mat_Floor3.mat", new Color(0.15f, 0.15f, 0.17f, 1f), 0.08f, 0.16f),
                wall = CreateMat("Mat_Wall.mat", new Color(0.30f, 0.28f, 0.26f, 1f), 0.04f, 0.14f),
                darkWall = CreateMat("Mat_DarkWall.mat", new Color(0.21f, 0.20f, 0.22f, 1f), 0.06f, 0.15f),
                stairsStone = CreateMat("Mat_StairsStone.mat", new Color(0.32f, 0.30f, 0.28f, 1f), 0.04f, 0.18f),
                darkWood = CreateMat("Mat_DarkWood.mat", new Color(0.22f, 0.14f, 0.09f, 1f), 0.04f, 0.26f),
                midWood = CreateMat("Mat_MidWood.mat", new Color(0.35f, 0.23f, 0.14f, 1f), 0.04f, 0.30f),
                mahoganyWood = CreateMat("Mat_MahoganyWood.mat", new Color(0.28f, 0.13f, 0.09f, 1f), 0.06f, 0.36f),
                ebonyWood = CreateMat("Mat_EbonyWood.mat", new Color(0.11f, 0.09f, 0.08f, 1f), 0.06f, 0.32f),
                crateWood = CreateMat("Mat_CrateWood.mat", new Color(0.40f, 0.27f, 0.15f, 1f), 0.0f, 0.16f),
                brassMetal = CreateMat("Mat_BrassMetal.mat", new Color(0.76f, 0.58f, 0.24f, 1f), 0.82f, 0.65f),
                ironMetal = CreateMat("Mat_IronMetal.mat", new Color(0.18f, 0.18f, 0.20f, 1f), 0.78f, 0.42f),
                waxCandle = CreateMat("Mat_WaxCandle.mat", new Color(0.84f, 0.78f, 0.64f, 1f), 0.0f, 0.24f),
                linenWhite = CreateMat("Mat_LinenWhite.mat", new Color(0.76f, 0.74f, 0.69f, 1f), 0.0f, 0.12f),
                blanketRed = CreateMat("Mat_BlanketRed.mat", new Color(0.42f, 0.14f, 0.15f, 1f), 0.0f, 0.14f),
                rugCrimson = CreateMat("Mat_RugCrimson.mat", new Color(0.36f, 0.12f, 0.13f, 1f), 0.0f, 0.10f),
                rugSlate = CreateMat("Mat_RugSlate.mat", new Color(0.18f, 0.21f, 0.26f, 1f), 0.0f, 0.10f),
                // Varied eerie Gothic Oil Paintings (non-emissive textured URP Lit)
                paintingPortrait = CreateTexturedMat("Mat_PaintingPortrait.mat", uiAssets.paintingPortrait, Color.white, 0.0f, 0.18f),
                paintingMansion = CreateTexturedMat("Mat_PaintingMansion.mat", uiAssets.paintingMansion, Color.white, 0.0f, 0.18f),
                paintingMoonTree = CreateTexturedMat("Mat_PaintingMoonTree.mat", uiAssets.paintingMoonTree, Color.white, 0.0f, 0.18f),
                paintingMirror = CreateTexturedMat("Mat_PaintingMirror.mat", uiAssets.paintingMirror, Color.white, 0.20f, 0.65f),
                // Horror Critters & Cobwebs (strictly ZERO scene light emitted!)
                cobwebMat = CreateCobwebMat("Mat_Cobweb.mat", uiAssets.cobwebTexture),
                spiderBodyMat = CreateMat("Mat_SpiderBody.mat", new Color(0.05f, 0.045f, 0.055f, 1f), 0.08f, 0.28f),
                spiderRedEyeUnlit = CreateUnlitMat("Mat_SpiderRedEyeUnlit.mat", new Color(0.96f, 0.07f, 0.06f, 1f)),
                spiritCyanUnlit = CreateUnlitMat("Mat_SpiritCyanClue.mat", new Color(0.14f, 0.94f, 1.0f, 1f)),
                spiritRedUnlit = CreateUnlitMat("Mat_SpiritRedMark.mat", new Color(0.76f, 0.10f, 0.10f, 1f)),
                ratFurMat = CreateMat("Mat_RatFur.mat", new Color(0.24f, 0.21f, 0.19f, 1f), 0.0f, 0.14f),
                ratTailPinkMat = CreateMat("Mat_RatTailPink.mat", new Color(0.56f, 0.36f, 0.36f, 1f), 0.0f, 0.20f),
                bookRed = CreateMat("Mat_BookRed.mat", new Color(0.44f, 0.14f, 0.14f, 1f), 0.0f, 0.22f),
                bookBlue = CreateMat("Mat_BookBlue.mat", new Color(0.16f, 0.22f, 0.36f, 1f), 0.0f, 0.22f),
                bookGreen = CreateMat("Mat_BookGreen.mat", new Color(0.16f, 0.30f, 0.20f, 1f), 0.0f, 0.22f),
                maleCoat = CreateMat("Mat_MaleCoat.mat", new Color(0.20f, 0.26f, 0.36f, 1f), 0.04f, 0.24f),
                malePants = CreateMat("Mat_MalePants.mat", new Color(0.14f, 0.15f, 0.18f, 1f), 0.0f, 0.18f),
                maleHair = CreateMat("Mat_MaleHair.mat", new Color(0.14f, 0.10f, 0.07f, 1f), 0.0f, 0.25f),
                femaleTunic = CreateMat("Mat_FemaleTunic.mat", new Color(0.50f, 0.17f, 0.24f, 1f), 0.04f, 0.26f),
                femalePants = CreateMat("Mat_FemalePants.mat", new Color(0.15f, 0.14f, 0.17f, 1f), 0.0f, 0.18f),
                femaleHair = CreateMat("Mat_FemaleHair.mat", new Color(0.22f, 0.12f, 0.07f, 1f), 0.0f, 0.30f),
                banditCloth = CreateMat("Mat_BanditCloth.mat", new Color(0.14f, 0.12f, 0.11f, 1f), 0.02f, 0.18f),
                banditTurban = CreateMat("Mat_BanditTurban.mat", new Color(0.48f, 0.12f, 0.10f, 1f), 0.0f, 0.18f),
                skin = CreateMat("Mat_Skin.mat", new Color(0.82f, 0.68f, 0.55f, 1f), 0.0f, 0.20f),
                // Strictly NON-EMISSIVE on all mansion props & pickups!
                lampGlass = CreateMat("Mat_LampGlass.mat", new Color(0.95f, 0.78f, 0.36f, 1f), 0.05f, 0.85f),
                oilFlask = CreateMat("Mat_OilFlask.mat", new Color(0.42f, 0.66f, 0.30f, 1f), 0.10f, 0.78f),
                puzzlePieceGold = CreateMat("Mat_PuzzlePieceGold.mat", new Color(0.92f, 0.76f, 0.26f, 1f), 0.82f, 0.74f),
                parchmentPaper = CreateMat("Mat_Parchment.mat", new Color(0.88f, 0.82f, 0.68f, 1f), 0.0f, 0.15f),
                chestWood = CreateMat("Mat_ChestWood.mat", new Color(0.30f, 0.16f, 0.09f, 1f), 0.08f, 0.34f),
                doorWood = CreateMat("Mat_DoorWood.mat", new Color(0.24f, 0.14f, 0.09f, 1f), 0.20f, 0.28f),
                doorFrame = CreateMat("Mat_DoorFrame.mat", new Color(0.34f, 0.32f, 0.30f, 1f), 0.08f, 0.22f),
                forestGrass = CreateMat("Mat_ForestGrass.mat", new Color(0.12f, 0.18f, 0.11f, 1f), 0.0f, 0.10f),
                pineLeaf = CreateMat("Mat_PineLeaf.mat", new Color(0.09f, 0.20f, 0.12f, 1f), 0.0f, 0.12f),
                tentCanvas = CreateMat("Mat_TentCanvas.mat", new Color(0.48f, 0.42f, 0.32f, 1f), 0.0f, 0.14f),
                campfireFlame = CreateMat("Mat_CampfireFlame.mat", new Color(1f, 0.56f, 0.12f, 1f), 0.0f, 0.40f)
            };
        }

        private static Material CreateMat(string fileName, Color baseColor, float metallic, float smoothness)
        {
            string path = $"{MaterialsDir}/{fileName}";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            // Strictly ZERO emission/glow on all assets
            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateTexturedMat(string fileName, Texture2D tex, Color tint, float metallic, float smoothness)
        {
            Material mat = CreateMat(fileName, tint, metallic, smoothness);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateCobwebMat(string fileName, Texture2D webTex)
        {
            string path = $"{MaterialsDir}/{fileName}";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;

            // Translucent URP alpha-blended cobweb lit strictly by player lantern (ZERO emission!)
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.86f, 0.85f, 0.82f, 0.80f));
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.86f, 0.85f, 0.82f, 0.80f));
            if (webTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", webTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", webTex);
            }
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            mat.renderQueue = (int)RenderQueue.Transparent;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateUnlitMat(string fileName, Color unlitColor)
        {
            // Used ONLY for tiny spider red eyes — pure unlit red color, ZERO scene light emitted
            string path = $"{MaterialsDir}/{fileName}";
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", unlitColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", unlitColor);

            EditorUtility.SetDirty(mat);
            return mat;
        }

#if UNITY_6000_0_OR_NEWER
        private static PhysicsMaterial CreateZeroFrictionPhysicsMaterial()
        {
            string path = $"{MaterialsDir}/PlayerZeroFriction.physicsMaterial";
            PhysicsMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (pm == null)
            {
                pm = new PhysicsMaterial("PlayerZeroFriction");
                AssetDatabase.CreateAsset(pm, path);
            }
            pm.dynamicFriction = 0f;
            pm.staticFriction = 0f;
            pm.bounciness = 0f;
            pm.frictionCombine = PhysicsMaterialCombine.Minimum;
            pm.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(pm);
            return pm;
        }
#else
        private static PhysicMaterial CreateZeroFrictionPhysicsMaterial()
        {
            string path = $"{MaterialsDir}/PlayerZeroFriction.physicMaterial";
            PhysicMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(path);
            if (pm == null)
            {
                pm = new PhysicMaterial("PlayerZeroFriction");
                AssetDatabase.CreateAsset(pm, path);
            }
            pm.dynamicFriction = 0f;
            pm.staticFriction = 0f;
            pm.bounciness = 0f;
            pm.frictionCombine = PhysicMaterialCombine.Minimum;
            pm.bounceCombine = PhysicMaterialCombine.Minimum;
            EditorUtility.SetDirty(pm);
            return pm;
        }
#endif

        // =========================================================================
        // GENERAL HELPERS
        // =========================================================================

        private static GameObject TryInstantiateImportedModel(string[] keywords, Transform parent, bool allowAnyHumanoidFallback)
        {
            string[] searchFolders = Directory.Exists("Assets/Models") ? new[] { "Assets/Models", "Assets" } : new[] { "Assets" };

            // Pass 1: Look for explicit keyword-matching .fbx/.glb/.obj Models OR Prefabs
            string[] filters = { "t:Model", "t:Prefab" };
            for (int f = 0; f < filters.Length; f++)
            {
                string[] guids = AssetDatabase.FindAssets(filters[f], searchFolders);
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(path)) continue;
                    string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                    for (int k = 0; k < keywords.Length; k++)
                    {
                        if (lower.Contains(keywords[k]))
                        {
                            GameObject inst = InstantiateCharacterAsset(path, parent);
                            if (inst != null) return inst;
                        }
                    }
                }
            }

            // Pass 2: If enabled, auto-detect ANY imported Humanoid/Generic rigged character model under Assets/Models or Assets/
            if (allowAnyHumanoidFallback)
            {
                string[] modelGuids = AssetDatabase.FindAssets("t:Model", searchFolders);
                for (int i = 0; i < modelGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(modelGuids[i]);
                    string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                    if (lower.Contains("bandit") || lower.Contains("daku") || lower.Contains("enemy") || lower.Contains("rat") || lower.Contains("spider"))
                        continue;

                    ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer != null && (importer.animationType == ModelImporterAnimationType.Human || importer.animationType == ModelImporterAnimationType.Generic))
                    {
                        GameObject inst = InstantiateCharacterAsset(path, parent);
                        if (inst != null) return inst;
                    }
                }
            }

            return null;
        }

        private static GameObject InstantiateCharacterAsset(string assetPath, Transform parent)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null) return null;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            if (inst == null) return null;

            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            // Ensure Animator component is present if the imported asset provides a rigged Avatar
            Animator anim = inst.GetComponentInChildren<Animator>();
            if (anim == null)
            {
                Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
                if (avatar != null)
                {
                    anim = inst.AddComponent<Animator>();
                    anim.avatar = avatar;
                }
            }
            return inst;
        }

        private static GameObject CreateMeshChild(string name, Mesh mesh, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return go;
        }

        private static GameObject CreatePrimitiveChild(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool removeCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            if (removeCollider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            return go;
        }

        private static GameObject CreateWall(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            return CreatePrimitiveChild(name, PrimitiveType.Cube, parent, localPos, scale, mat, false);
        }

        private static Transform CreateLimbPivot(Transform parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private static GameObject CreateFullStretchPanel(string name, Transform parent, Color color, bool raycastTarget)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycastTarget;
            StretchToParent(go.GetComponent<RectTransform>());
            return go;
        }

        private static GameObject CreateImageBox(string name, Transform parent, Sprite sprite, Color color, Image.Type type)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            return go;
        }

        private static Text CreateText(string name, Transform parent, Font font, string content, int fontSize, FontStyle style, Color color, TextAnchor align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        private static void SetAnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddShadow(GameObject go, Color color, Vector2 dist)
        {
            Shadow s = go.AddComponent<Shadow>();
            s.effectColor = color;
            s.effectDistance = dist;
        }

        private static void EnsureDirectory(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == scenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
