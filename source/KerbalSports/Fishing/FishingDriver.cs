using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSP.UI.Screens.Flight;
using Fishing.KerbalAnimation;

namespace Fishing
{
    public class FishingDriver : MonoBehaviour
    {
        public enum FishingState
        {
            NotFishing,
            StartFishing,
            Idle,
            Casting,
            Reeling,
            Hooked,
            Caught
        }

        System.Random rand = new System.Random();
        bool visible = true;

        Vessel evaVessel;
        ProtoCrewMember kerbalFisher;
        Animation animation;
        Fish currentFish;
        FishingPole fishingPole;
        bool navballToggled;

        const double ctrlDeltaTime = 0.1;
        const double castDelay = 1.2;
        const double castDuration = 0.4;
        const double castTime = 2.0;
        const float rodDelta = 0.040f;
        const float rodWindowRatio = 0.1f;
        const float distWindowRatio = 0.1f;
        const float reelingSpeed = 0.2f;
        const float defaultHookedReelingSpeed = 0.12f;
        const float defaultFishCatchSpeed = 0.100f;
        const float defaultFishEscapeSpeed = 0.100f;
        const float maxRodLeeway = 0.1f;

        // State and time
        public FishingState fishingState { get; private set; }
        protected float stateStartTime;
        double lCtrlTime;
        double rCtrlTime;
        float rodPosition = 0.5f;
        float bobDistance;
        float fishHookDistance;
        float rodLeeway;
        float loopingClipStart;
        float loopingClipTime;
        string loopingClipName;
        double bodyDifficulty;
        float hookedReelingSpeed;
        float fishCatchSpeed;
        float fishEscapeSpeed;

        // GUI stuff
        bool texturesLoaded = false;
        Texture2D windowTex;
        Texture2D rodWindowTex;
        Texture2D ctrlSmashTex;
        Texture2D fishLeftTex;
        Texture2D fishRightTex;
        Texture2D bobTex;
        GUIStyle textStyle;
        Rect sourceRect = new Rect(0.0f, 0.0f, 1f, 1f);
        Color smashColor = new Color(0x75 / 255.0f, 0x20 / 255.0f, 0x20 / 255.0f);

        // Animations
        KerbalAnimationClip castingClip;
        KerbalAnimationClip reelingClip;
        KerbalAnimationClip hookedClip;
        KerbalAnimationClip caughtClip;

        // Camera stuff
        const float cameraHeight = 0.35f;
        const float cameraDist = 1.8f;
        const float cameraAngle = 20.0f * (float)Math.PI / 180.0f;
        float startAngle;
        float startDist;
        float startHeight;
        float cameraZoomTime;
        const float defaultCameraZoomTime = 2.0f;
        const float minAngularVelocity = 20.0f * (float)Math.PI / 180.0f;
        const float maxZoomVelocity = 5.0f;

        void Awake()
        {
            castingClip = new KerbalAnimationClip("KerbalSports/anim/fishingCasting");
            reelingClip = new KerbalAnimationClip("KerbalSports/anim/fishingReeling");
            hookedClip = new KerbalAnimationClip("KerbalSports/anim/fishingHooked");
            caughtClip = new KerbalAnimationClip("KerbalSports/anim/fishingCaught");
        }

        void Start()
        {
            GameEvents.onHideUI.Add(new EventVoid.OnEvent(OnHideUI));
            GameEvents.onShowUI.Add(new EventVoid.OnEvent(OnShowUI));
            GameEvents.onGameSceneSwitchRequested.Add(new EventData<GameEvents.FromToAction<GameScenes, GameScenes>>.OnEvent(GameSceneSwitchRequested));
        }

        protected void OnDestroy()
        {
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onGameSceneSwitchRequested.Remove(new EventData<GameEvents.FromToAction<GameScenes, GameScenes>>.OnEvent(GameSceneSwitchRequested));
        }

        public void OnHideUI()
        {
            visible = false;
        }

        public void OnShowUI()
        {
            visible = true;
        }

        void GameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fta)
        {
            SetState(FishingState.NotFishing);
            Destroy(this);
        }

        /// <summary>
        /// Starts the fishing program.
        /// </summary>
        public void StartFishing(Vessel v, ProtoCrewMember pcm)
        {
            evaVessel = v;
            kerbalFisher = pcm;

            SetState(FishingState.StartFishing);

            // Get the kerbal EVA
            KerbalEVA eva = evaVessel.GetComponent<KerbalEVA>();

            // Create the fishing pole object
            GameObject poleObject = new GameObject("fishingPole");
            fishingPole = poleObject.AddComponent<FishingPole>();
            fishingPole.referenceTransform = eva.transform.FindDeepChild("bn_r_mid_a01");
            poleObject.SetActive(true);

            // Initialize animations
            animation = eva.GetComponent<Animation>();
            castingClip.Initialize(animation, eva.transform);
            reelingClip.Initialize(animation, eva.transform);
            hookedClip.Initialize(animation, eva.transform);
            caughtClip.Initialize(animation, eva.transform);

            // Close the window that caused us to open
            UIPartActionWindow paw = UnityEngine.Object.FindObjectOfType<UIPartActionWindow>();
            if (paw != null)
            {
                paw.isValid = false;
            }

            // Determine the body difficulty
            double gravityModifier = evaVessel.mainBody.gravParameter / (evaVessel.mainBody.Radius * evaVessel.mainBody.Radius) / 9.81;
            double scienceModifier = evaVessel.mainBody.scienceValues.SplashedDataValue / 10.0f;
            bodyDifficulty = gravityModifier + scienceModifier;
            hookedReelingSpeed = defaultHookedReelingSpeed / (float)bodyDifficulty;
            Debug.Log("Body difficulty for " + evaVessel.mainBody.name + " = " + bodyDifficulty);
        }

        void OnGUI()
        {
            if (fishingState == FishingState.NotFishing)
            {
                return;
            }

            // Always allow escape to stop fishing
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Escape)
            {
                SetState(FishingState.NotFishing);
                return;
            }

            if (!texturesLoaded)
            {
                LoadGuiInfo();
                texturesLoaded = true;
            }

            if (visible)
            {
                // Keypress handling
                if (Event.current.type == EventType.KeyUp)
                {
                    switch (fishingState)
                    {
                        case FishingState.Idle:
                            if (Event.current.keyCode == KeyCode.Space)
                            {
                                SetState(FishingState.Casting);
                            }
                            break;
                        case FishingState.Hooked:
                            if (Event.current.keyCode == KeyCode.LeftControl)
                            {
                                lCtrlTime = 0.0;
                            }
                            else if (Event.current.keyCode == KeyCode.RightControl)
                            {
                                rCtrlTime = 0.0;
                            }
                            break;
                    }
                }
                if (Event.current.type == EventType.KeyDown)
                {
                    switch (fishingState)
                    {
                        case FishingState.Hooked:
                            if (Event.current.keyCode == KeyCode.LeftControl)
                            {
                                if (lCtrlTime == 0.0)
                                {
                                    lCtrlTime = Time.time;
                                    rodPosition = Math.Max(0.0f, rodPosition - rodDelta);
                                }
                            }
                            else if (Event.current.keyCode == KeyCode.RightControl)
                            {
                                if (rCtrlTime == 0.0)
                                {
                                    rCtrlTime = Time.time;
                                    rodPosition = Math.Min(1.0f, rodPosition + rodDelta);
                                }
                            }
                            break;
                    }
                }

                // Calculate UI size
                float displayWidth = Math.Min(Screen.width, Screen.height * 16.0f / 9.0f) - 128f;
                float displayLeft = (Screen.width - displayWidth) / 2.0f;
                float displayCenter = Screen.width / 2.0f;
                float displayRight = displayLeft + displayWidth;

                // UI elements
                if (Event.current.type == EventType.Repaint)
                {
                    // Calculate reference positions
                    float ctrlWidth = 64.0f;
                    float distanceTop = 128f;
                    float distanceHeight = Screen.height - 196f - 32f - 8.0f - 16f - distanceTop;
                    float distanceCenter = distanceTop + distanceHeight / 2.0f;
                    float bobCenter = (distanceHeight - 24f) * (0.5f - bobDistance) + distanceCenter;

                    // Draw the CTRL buttons
                    Rect ctrl1Rect = new Rect(displayLeft, Screen.height - 32f - 196f - 8.0f, ctrlWidth, 48f);
                    Rect ctrl2Rect = new Rect(displayRight - ctrlWidth, Screen.height - 32f - 196f - 8.0f, ctrlWidth, 48f);
                    Graphics.DrawTexture(ctrl1Rect, windowTex, 6, 6, 6, 6);
                    Graphics.DrawTexture(ctrl2Rect, windowTex, 6, 6, 6, 6);
                    GUI.Label(ctrl1Rect, "CTRL", textStyle);
                    GUI.Label(ctrl2Rect, "CTRL", textStyle);

                    // Draw the distance meter
                    Rect distanceRect = new Rect(displayLeft + 16f, distanceTop, 32f, distanceHeight);
                    Graphics.DrawTexture(distanceRect, windowTex, 6, 6, 6, 6);

                    if (fishingState == FishingState.Hooked)
                    {
                        // Calculate some widths
                        float fishWindowWidth = displayWidth - (ctrlWidth + 32.0f) * 2.0f;
                        float rodWindowWidth = fishWindowWidth * rodWindowRatio;
                        float rodWindowHeight = distanceHeight * distWindowRatio;

                        // Draw the rod distance window
                        float rodDistanceTop = (distanceHeight - 24f) * (0.5f + rodLeeway - maxRodLeeway - bobDistance) + distanceCenter;
                        if (rodDistanceTop < distanceTop + 2)
                        {
                            rodWindowHeight -= (distanceTop + 2) - rodDistanceTop;
                            rodDistanceTop = distanceTop + 2;
                        }
                        else if ((rodDistanceTop + rodWindowHeight) > (distanceTop + distanceHeight - 2))
                        {
                            rodWindowHeight = distanceTop + distanceHeight - 2 - rodDistanceTop;
                        }
                        Rect rodDistanceRect = new Rect(displayLeft + 18f, rodDistanceTop, 28f, rodWindowHeight);
                        Graphics.DrawTexture(rodDistanceRect, rodWindowTex);

                        // Draw the rod window
                        float rodCenter = (fishWindowWidth - rodWindowWidth) * (rodPosition - 0.5f) + displayCenter;
                        Rect rodWindowRect = new Rect(rodCenter - rodWindowWidth / 2.0f, Screen.height - 30f - 196f, rodWindowWidth, 28f);
                        Graphics.DrawTexture(rodWindowRect, rodWindowTex);

                        // Draw the fish window
                        Rect fishWindowRect = new Rect(displayCenter - fishWindowWidth / 2.0f, Screen.height - 32f - 196f, fishWindowWidth, 32f);
                        Graphics.DrawTexture(fishWindowRect, windowTex, 6, 6, 6, 6);

                        // Draw the fish
                        float fishCenter = (fishWindowWidth - 48f) * (currentFish.position - 0.5f) + displayCenter;
                        Rect fishRect = new Rect(fishCenter - 24f, Screen.height - 32f - 196f + 4f, 48f, 24f);
                        Graphics.DrawTexture(fishRect, currentFish.speed > 0 ? fishRightTex : fishLeftTex);

                        // Draw the first control smash
                        if (lCtrlTime + ctrlDeltaTime > Time.time)
                        {
                            Rect smashRect = new Rect(displayLeft - 32.0f, Screen.height - 16f - 196f - 64.0f, 128f, 128f);
                            Graphics.DrawTexture(smashRect, ctrlSmashTex, sourceRect, 0, 0, 0, 0, smashColor);
                        }

                        // Draw the second control smash
                        if (rCtrlTime + ctrlDeltaTime > Time.time)
                        {
                            Rect smashRect = new Rect(displayRight - ctrlWidth - 32.0f, Screen.height - 16f - 196f - 64.0f, 128f, 128f);
                            Graphics.DrawTexture(smashRect, ctrlSmashTex, sourceRect, 0, 0, 0, 0, smashColor);
                        }
                    }

                    // Draw the bob
                    Rect bobRect = new Rect(displayLeft + 16f + 4f, bobCenter - 12f, 24f, 24f);
                    Graphics.DrawTexture(bobRect, bobTex);
                }
            }
        }

        void LoadGuiInfo()
        {
            windowTex = GameDatabase.Instance.GetTexture("KerbalSports/images/window", false);
            rodWindowTex = GameDatabase.Instance.GetTexture("KerbalSports/images/rodWindow", false);
            ctrlSmashTex = GameDatabase.Instance.GetTexture("KerbalSports/images/ctrlSmash", false);
            fishLeftTex = GameDatabase.Instance.GetTexture("KerbalSports/images/fishL", false);
            fishRightTex = GameDatabase.Instance.GetTexture("KerbalSports/images/fishR", false);
            bobTex = GameDatabase.Instance.GetTexture("KerbalSports/images/bob", false);
            textStyle = new GUIStyle(HighLogic.Skin.label)
            {
                normal =
                {
                    textColor = Color.white
                },
                margin = new RectOffset(),
                padding = new RectOffset(8, 8, 8, 8),
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                fixedHeight = 48.0f
            };
        }

        void Update()
        {
            // Move our camera towards the desired positioning
            if (fishingState == FishingState.StartFishing)
            {
                float t = Mathf.InverseLerp(stateStartTime, stateStartTime + cameraZoomTime, Time.time);

                if (t > 1.0f)
                {
                    t = 1.0f;
                }

                float angle = Mathf.Lerp(startAngle, cameraAngle, t);
                float dist = Mathf.Lerp(startDist, cameraDist, t);
                float height = Mathf.Lerp(startHeight, cameraHeight, t);

                Vector3 desiredPosition = evaVessel.transform.position;
                desiredPosition += evaVessel.transform.up * height;
                desiredPosition += evaVessel.transform.right * (float)-Math.Sin(angle) * dist;
                desiredPosition += evaVessel.transform.forward * (float)Math.Cos(angle) * dist;

                FlightCamera.fetch.SetCamCoordsFromPosition(desiredPosition);

                if (t >= 1.0f)
                {
                    SetState(FishingState.Idle);
                }
            }

            if (fishingState == FishingState.Caught)
            {
                if (!animation.isPlaying)
                {
                    SetState(FishingState.Idle);
                }
            }

            if (fishingState == FishingState.Hooked)
            {
                if (!animation.isPlaying)
                {
                    animation.Play("fishingHooked");
                }

                AnimationState animState = animation["fishingHooked"];
                animState.speed = 0.00000001f;
                animState.time = (rodLeeway / maxRodLeeway) / animState.length;
            }

            // Can turn looping on for a clip unless the clip is created through Unity, and we hacked it using Kerbal Animation Studio instead. :(
            if (!string.IsNullOrEmpty(loopingClipName))
            {
                while (loopingClipStart + loopingClipTime < Time.time)
                {
                    loopingClipStart += loopingClipTime;
                }

                if (!animation.isPlaying)
                {
                    animation.Play(loopingClipName);
                }

                AnimationState animState = animation[loopingClipName];
                animState.time = Time.time - loopingClipStart;
            }
        }

        void FixedUpdate()
        {
            Vector3 relpos = (FlightCamera.fetch.transform.position - evaVessel.transform.position);
            Vector3 scale = (FlightCamera.fetch.transform.localScale);
            switch (fishingState)
            {
                case FishingState.Casting:
                    bobDistance = Mathf.Clamp((float)((Time.fixedTime - stateStartTime - castDelay) / castDuration), 0.0f, 1.0f);
                    if (Time.fixedTime - stateStartTime >= castTime)
                    {
                        SetState(FishingState.Reeling);
                    }
                    break;
                case FishingState.Reeling:
                    // Reel in the bob
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        bobDistance -= reelingSpeed * Time.fixedDeltaTime;
                    }

                    // Has a fish been hooked?
                    if (bobDistance < fishHookDistance)
                    {
                        currentFish = new Fish(evaVessel, bodyDifficulty);
                        fishCatchSpeed = (float)(defaultFishCatchSpeed / currentFish.difficulty);
                        fishEscapeSpeed = (float)(defaultFishEscapeSpeed * currentFish.difficulty);

                        SetState(FishingState.Hooked);
                    }
                    else if (bobDistance <= 0)
                    {
                        SetState(FishingState.Idle);
                        FishingScenario.Instance.failedAttempts++;
                        Debug.Log("failed attempts = " + FishingScenario.Instance.failedAttempts);
                    }
                    break;
                case FishingState.Hooked:
                    // Check if the fish is in or out of the window
                    if (Math.Abs(currentFish.position - rodPosition) < rodWindowRatio)
                    {
                        // Leeway only builds if we're not holding shift
                        if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                        {
                            rodLeeway = Math.Min(rodLeeway + fishCatchSpeed * Time.fixedDeltaTime, maxRodLeeway);
                        }
                    }
                    else
                    {
                        rodLeeway -= fishEscapeSpeed * Time.fixedDeltaTime;
                        if (rodLeeway < 0.0)
                        {
                            bobDistance -= rodLeeway;
                            rodLeeway = 0.0f;
                        }
                    }

                    // Reel in the bob
                    if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && rodLeeway > 0.0)
                    {
                        // Reel in as much as we can
                        float dist = hookedReelingSpeed * Time.fixedDeltaTime;
                        rodLeeway -= dist;
                        if (rodLeeway < 0.0)
                        {
                            dist += rodLeeway;
                        }
                        bobDistance -= dist;
                    }

                    // Check if the fish got away
                    if (bobDistance > 1.0f)
                    {
                        ScreenMessages.PostScreenMessage("The fish got away.", 5, ScreenMessageStyle.UPPER_CENTER);
                        SetState(FishingState.Idle);
                    }
                    else if (bobDistance < 0.0f)
                    {
                        ScreenMessages.PostScreenMessage("You caught a " + currentFish.weight.ToString("N1") + " kg fish!", 5, ScreenMessageStyle.UPPER_CENTER);
                        SetState(FishingState.Caught);
                    }

                    // Finally, let the fish update
                    currentFish.FixedUpdate();
                    break;
            }
        }

        void HandleIdleStateGUI()
        {
        }

        void SetState(FishingState newState)
        {
            Debug.Log("set fishing state to " + newState);

            // Remove any looping clips
            loopingClipName = null;

            // Enable/disable stuff
            if (newState == FishingState.StartFishing)
            {
                // Set control locks
                ControlTypes locks = ControlTypes.All ^ ControlTypes.QUICKLOAD;
                InputLockManager.SetControlLock(locks, "Fishing");

                // Hide navball
                navballToggled = NavBallToggle.Instance.panel.expanded;
                if (navballToggled)
                {
                    NavBallToggle.Instance.panel.Collapse();
                }
            }
            // Undo what we did
            else if (newState == FishingState.NotFishing)
            {
                // Clear the locks
                InputLockManager.RemoveControlLock("Fishing");

                // Restore navball
                if (navballToggled)
                {
                    NavBallToggle.Instance.panel.Expand();
                }

                // Remove the fishing pole
                Destroy(fishingPole);

                // Set the animation back to the idle one
                KerbalEVA eva = evaVessel.GetComponent<KerbalEVA>();
                animation.Stop();
                animation.Play(eva.Animations.idle.animationName);
            }

            // Calculate the camera start position
            if (newState == FishingState.StartFishing)
            {
                Vector3 cameraRelPos = FlightCamera.fetch.transform.position - evaVessel.transform.position;
                Vector3 projection = Vector3.ProjectOnPlane(cameraRelPos, evaVessel.transform.up);
                startAngle = (float)Math.Acos(Vector3.Dot(projection.normalized, evaVessel.transform.forward));
                startHeight = Vector3.Dot(cameraRelPos, evaVessel.transform.up);
                double x = Vector3.Dot(cameraRelPos, evaVessel.transform.right);
                double y = Vector3.Dot(cameraRelPos, evaVessel.transform.forward);
                startDist = (float)Math.Sqrt(x * x + y * y);

                cameraZoomTime = defaultCameraZoomTime;
                if (Math.Abs(startAngle - cameraAngle) / minAngularVelocity < cameraZoomTime)
                {
                    cameraZoomTime = Math.Abs(startAngle - cameraAngle) / minAngularVelocity;
                }
                if (cameraZoomTime != defaultCameraZoomTime && Math.Abs(startHeight - cameraHeight) / maxZoomVelocity > cameraZoomTime)
                {
                    cameraZoomTime = Math.Min(defaultCameraZoomTime, Math.Abs(startHeight - cameraHeight) / maxZoomVelocity);
                }
                if (cameraZoomTime != defaultCameraZoomTime && Math.Abs(startDist - cameraDist) / maxZoomVelocity > cameraZoomTime)
                {
                    cameraZoomTime = Math.Min(defaultCameraZoomTime, Math.Abs(startDist - cameraDist) / maxZoomVelocity);
                }
            }

            // Play the idle animation
            if (newState == FishingState.Idle)
            {
                KerbalEVA eva = evaVessel.GetComponent<KerbalEVA>();
                animation.Stop();
                animation.Play(eva.Animations.idle.animationName);
            }
            // Play the casting animation
            else if (newState == FishingState.Casting)
            {
                // Set the casting animation
                animation.Stop();
                animation.Play("fishingCasting");

                // Start at the zero distance when casting
                bobDistance = 0.0f;
            }
            // Reeling animation
            else if (newState == FishingState.Reeling)
            {
                // Set the casting animation
                loopingClipStart = Time.time;
                loopingClipTime = 4.0f;
                loopingClipName = "fishingReeling";
                animation.Stop();
            }
            // Hooked animation
            else if (newState == FishingState.Hooked)
            {
                animation.Stop();
            }
            else if (newState == FishingState.Caught)
            {
                animation.Stop();
                animation.Play("fishingCaught");
            }

            // Decide if a fish will be caught on this cast, and when
            if (newState == FishingState.Reeling)
            {
                double catchChance = (FishingScenario.Instance.failedAttempts + 1) / 6.0;
                if (catchChance >= 1.0 || rand.NextDouble() < catchChance)
                {
                    fishHookDistance = (float)rand.NextDouble() * 0.55f + 0.25f;
                }
                else
                {
                    fishHookDistance = -1.0f;
                }
            }

            // Center the rod when we hook a fish
            if (newState == FishingState.Hooked)
            {
                rodPosition = 0.5f;
                rodLeeway = 0.0f;
                fishHookDistance = -1.0f;
            }

            // Caught a fish, record it
            if (newState == FishingState.Caught)
            {
                FishingScenario.Instance.CaughtFish(kerbalFisher, currentFish);
            }

            stateStartTime = Time.fixedTime;
            lCtrlTime = rCtrlTime = 0.0;
            fishingState = newState;
        }
    }
}
