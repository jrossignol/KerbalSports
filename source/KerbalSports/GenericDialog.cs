using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalSports
{
    public class GenericDialog : MonoBehaviour
    {
        private static Material _portraitRenderMaterial = null;
        public static Material PortraitRenderMaterial
        {
            get
            {
                if (_portraitRenderMaterial == null)
                {
                    _portraitRenderMaterial = AssetBase.GetPrefab("Instructor_Gene").GetComponent<KerbalInstructor>().PortraitRenderMaterial;
                }
                return _portraitRenderMaterial;
            }
        }
        
        public enum Animation
        {
            idle,
            idle_lookAround,
            idle_sigh,
            idle_wonder,
            true_thumbUp,
            true_thumbsUp,
            true_nodA,
            true_nodB,
            true_smileA,
            true_smileB,
            false_disappointed,
            false_disagreeA,
            false_disagreeB,
            false_disagreeC,
            false_sadA,
        }

        private Rect windowPos = new Rect(0, 0, 0, 0);
        private static GUIStyle windowStyle;
        public bool visible = true;

        // Kerbal Instructor public stuff
        public string instructorName;
        public Animation? animation;

        // Kerbal Instructor private stuff
        KerbalInstructor instructor;
        RenderTexture instructorTexture;
        static float offset = 0.0f;
        GameObject lightGameObject = null;
        string characterName;
        CharacterAnimationState animState = null;
        float nextAnimTime = float.MaxValue;
        private GUIStyle instructorLabelStyle;

        // Text stuff
        public string text;
        GUIStyle labelStyle;

        void OnGUI()
        {
            float w = 512f;

            if (windowPos.width == 0 && windowPos.height == 0)
            {
                float x = Screen.width / 2.0f - w;
                float h = 600f;
                float y = Math.Max(144f, Screen.height/2.0f - h);

                windowPos = new Rect(x, y, w, h);
            }

            if (visible)
            {
                UnityEngine.GUI.skin = HighLogic.Skin;
                windowPos = GUILayout.Window(GetType().FullName.GetHashCode(),
                    windowPos, WindowFunction, "Kerbal Sports", windowStyle ?? HighLogic.Skin.window, GUILayout.Width(w));
            }
        }

        void WindowFunction(int windowID)
        {
            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(HighLogic.Skin.window);
                windowStyle.alignment = TextAnchor.UpperLeft;
                windowStyle.active.textColor = Color.white;
                windowStyle.focused.textColor = Color.white;
                windowStyle.hover.textColor = Color.white;
                windowStyle.normal.textColor = Color.white;
                windowStyle.onActive.textColor = Color.white;
                windowStyle.onFocused.textColor = Color.white;
                windowStyle.onHover.textColor = Color.white;
                windowStyle.onNormal.textColor = Color.white;
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Space(8);

            if (instructor == null && !string.IsNullOrEmpty(instructorName))
            {
                instructor = ((GameObject)UnityEngine.Object.Instantiate(AssetBase.GetPrefab(instructorName))).GetComponent<KerbalInstructor>();

                instructorTexture = new RenderTexture(128, 128, 8);
                instructor.instructorCamera.targetTexture = instructorTexture;
                instructor.instructorCamera.ResetAspect();

                // TODO - this not needed in 1.1 as of current build
                // Remove the lights for Gene/Wernher
                /*
                Light mainlight = instructor.GetComponentsInChildren<Light>(true).Where(l => l.name == "mainlight").FirstOrDefault();
                if (mainlight != null)
                {
                    UnityEngine.Object.Destroy(mainlight);
                }
                Light backlight = instructor.GetComponentsInChildren<Light>(true).Where(l => l.name == "backlight").FirstOrDefault();
                if (backlight != null)
                {
                    UnityEngine.Object.Destroy(backlight);
                }*/

                offset += 25f;
                instructor.gameObject.transform.Translate(offset, 0.0f, 0.0f);

                // Add a light
                lightGameObject = new GameObject("Dialog Box Light");
                Light lightComp = lightGameObject.AddComponent<Light>();
                lightComp.color = new Color(0.4f, 0.4f, 0.4f);
                lightGameObject.transform.position = instructor.instructorCamera.transform.position;

                if (string.IsNullOrEmpty(characterName))
                {
                    characterName = instructor.CharacterName;
                }

                instructor.SetupAnimations();

                if (animation != null)
                {
                    switch (animation.Value)
                    {
                        case Animation.idle:
                            animState = instructor.anim_idle;
                            break;
                        case Animation.idle_lookAround:
                            animState = instructor.anim_idle_lookAround;
                            break;
                        case Animation.idle_sigh:
                            animState = instructor.anim_idle_sigh;
                            break;
                        case Animation.idle_wonder:
                            animState = instructor.anim_idle_wonder;
                            break;
                        case Animation.true_thumbUp:
                            animState = instructor.anim_true_thumbUp;
                            break;
                        case Animation.true_thumbsUp:
                            animState = instructor.anim_true_thumbsUp;
                            break;
                        case Animation.true_nodA:
                            animState = instructor.anim_true_nodA;
                            break;
                        case Animation.true_nodB:
                            animState = instructor.anim_true_nodB;
                            break;
                        case Animation.true_smileA:
                            animState = instructor.anim_true_smileA;
                            break;
                        case Animation.true_smileB:
                            animState = instructor.anim_true_smileB;
                            break;
                        case Animation.false_disappointed:
                            animState = instructor.anim_false_disappointed;
                            break;
                        case Animation.false_disagreeA:
                            animState = instructor.anim_false_disagreeA;
                            break;
                        case Animation.false_disagreeB:
                            animState = instructor.anim_false_disagreeB;
                            break;
                        case Animation.false_disagreeC:
                            animState = instructor.anim_false_disagreeC;
                            break;
                        case Animation.false_sadA:
                            animState = instructor.anim_false_sadA;
                            break;
                    }

                    // Give a short delay before playing the animation
                    nextAnimTime = Time.fixedTime + 0.3f;
                }
            }

            if (instructor != null)
            {
                // Play the animation
                if (nextAnimTime <= Time.fixedTime)
                {
                    instructor.PlayEmote(animState);
                    animState.audioClip = null;
                    nextAnimTime = Time.fixedTime + animState.clip.length;
                }

                GUILayout.BeginVertical(GUILayout.Width(128));
                GUILayout.Box("", GUILayout.Width(128), GUILayout.Height(128));
                if (Event.current.type == EventType.Repaint)
                {
                    Rect rect = GUILayoutUtility.GetLastRect();
                    rect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
                    Graphics.DrawTexture(rect, instructorTexture, new Rect(0.0f, 0.0f, 1f, 1f), 124, 124, 124, 124, Color.white, PortraitRenderMaterial);
                }

                DisplayName(128);

                GUILayout.EndVertical();
            }

            // Display text
            if (text != null)
            {
                if (labelStyle == null)
                {
                    labelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
                    labelStyle.alignment = TextAnchor.UpperLeft;
                    labelStyle.richText = true;
                    labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                    labelStyle.fontSize = 16;
                }

                GUILayout.Label(text, labelStyle, GUILayout.ExpandWidth(true));
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK", GUILayout.MinWidth(80)))
            {
                visible = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            UnityEngine.GUI.DragWindow();
        }

        protected void DisplayName(float width)
        {
            if (instructorLabelStyle == null)
            {
                instructorLabelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
                instructorLabelStyle.alignment = TextAnchor.UpperCenter;
                instructorLabelStyle.normal.textColor = new Color(0.729f, 0.855f, 0.333f);
                instructorLabelStyle.fontStyle = FontStyle.Bold;
            }

            GUILayout.Label(characterName, instructorLabelStyle, GUILayout.Width(width));
        }
    }
}
