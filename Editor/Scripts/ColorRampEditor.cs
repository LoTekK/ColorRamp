using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeckArtist.Tools
{
    [CustomEditor(typeof(ColorRamp))]
    public class ColorRampEditor : UnityEditor.Editor
    {
        private ColorRamp ramp;
        private bool editorStateChange;
        private ReorderableList list;

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += CheckPlaymodeState;
            Undo.undoRedoPerformed += UndoHandler;
            ramp = target as ColorRamp;
            // if (ramp.Ramps == null || ramp.Ramps.Length == 0)
            // {
            //     ramp.Ramps = new ColorRamp.Ramp[1];
            // }
            // if (ramp.Texture == null)
            // {
            //     ramp.Texture = new Texture2D(ramp.Size.x, ramp.Size.y)
            //     {
            //         name = ramp.name,
            //         wrapMode = TextureWrapMode.Clamp
            //     };
            // }
            list = new ReorderableList(serializedObject, serializedObject.FindProperty("Ramps"))
            {
                drawHeaderCallback = DrawListHeader,
                drawElementCallback = DrawElement,
                onChangedCallback = OnListChanged
            };
        }

        void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= CheckPlaymodeState;
            Undo.undoRedoPerformed -= UndoHandler;
            if (editorStateChange || UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                // Debug.Log("Editor is switching states; not saving gradient");
                return;
            }
            if (ramp.Texture == null)
            {
                return;
            }
            Save();
        }

        [MenuItem("Assets/Create/Color Ramp", false, 10)]
        private static void CreateAsset()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            if (!AssetDatabase.IsValidFolder(path))
            {
                path = Path.GetDirectoryName(path);
            }
            path = AssetDatabase.GenerateUniqueAssetPath($"{path}/Color Ramp.asset");
            var newRamp = CreateInstance<ColorRamp>();
            var gradient = new Gradient();
            var colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(Color.black, 0);
            colorKeys[1] = new GradientColorKey(Color.white, 1);
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1, 0);
            alphaKeys[1] = new GradientAlphaKey(1, 1);
            gradient.SetKeys(colorKeys, alphaKeys);
            newRamp.TransitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            newRamp.Texture = new Texture2D(newRamp.Size.x, newRamp.Size.y)
            {
                name = newRamp.name,
                wrapMode = TextureWrapMode.Clamp
            };
            newRamp.Ramps = new ColorRamp.Ramp[1];
            newRamp.Ramps[0].Gradient = gradient;
            newRamp.VerticalTransitionMode = GradientMode.Blend;
            ProjectWindowUtil.CreateAsset(newRamp, path);
        }

        private void UndoHandler()
        {
            UpdateTexture(ramp);
            Refresh();
        }

        private void DrawListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Ramp[s]");
        }

        private void OnListChanged(ReorderableList list)
        {
            UpdateTexture(ramp);
            Refresh();
        }

        private void Refresh()
        {
            // HACK: Inspector handling when reorderin/removingg complex serialized props (eg. gradients, quaternions) is a bit broken
            Selection.objects = null;
            EditorApplication.delayCall += () => Selection.objects = new UnityEngine.Object[] { ramp };
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element = list.serializedProperty.GetArrayElementAtIndex(index);
            Rect buttonRect = new Rect(rect)
            {
                xMin = rect.width - 8,
                width = 28
            };
            // EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 32, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 32, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Gradient"), GUIContent.none);
            if (GUI.Button(new Rect(buttonRect.x, buttonRect.y, buttonRect.width, EditorGUIUtility.singleLineHeight), new GUIContent(".."), EditorStyles.miniButton))
            {
                GenericMenu m = new GenericMenu();
                m.AddItem(new GUIContent("Reverse", "Reverse this Gradient"), false, () =>
                {
                    int undoIndex = Undo.GetCurrentGroup();
                    Undo.RegisterCompleteObjectUndo(ramp, "Reverse Gradient");
                    var g = ramp.Ramps[index].Gradient;
                    var newC = new GradientColorKey[g.colorKeys.Length];
                    var newA = new GradientAlphaKey[g.alphaKeys.Length];
                    for (int i = 0; i < g.colorKeys.Length; i++)
                    {
                        newC[i].color = g.colorKeys[i].color;
                        newC[i].time = 1 - g.colorKeys[i].time;
                    }
                    for (int i = 0; i < g.alphaKeys.Length; i++)
                    {
                        newA[i].alpha = g.alphaKeys[i].alpha;
                        newA[i].time = 1 - g.alphaKeys[i].time;
                    }
                    ramp.Ramps[index].Gradient.SetKeys(newC, newA);
                    Undo.CollapseUndoOperations(undoIndex);
                    EditorUtility.SetDirty(ramp);
                    UpdateTexture(ramp);
                    Refresh();
                });
                m.AddSeparator("");
                m.AddItem(new GUIContent("Smoothstep", "Toggle between linear and smoothstep interpolation between keys"), ramp.Ramps[index].Smoothstep, () =>
                {
                    int undoIndex = Undo.GetCurrentGroup();
                    Undo.RegisterCompleteObjectUndo(ramp, "Toggle Gradient Smoothstep");
                    ramp.Ramps[index].Smoothstep = !ramp.Ramps[index].Smoothstep;
                    Undo.CollapseUndoOperations(undoIndex);
                    EditorUtility.SetDirty(ramp);
                    UpdateTexture(ramp);
                    Refresh();
                });
                m.ShowAsContext();
            }
        }

        void CheckPlaymodeState(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    editorStateChange = true;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    editorStateChange = false;
                    break;
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Bind(serializedObject);
            var imgui = new IMGUIContainer(() =>
            {
                serializedObject.Update();
                using (var c = new EditorGUI.ChangeCheckScope())
                {
                    list.DoLayoutList();
                    serializedObject.ApplyModifiedProperties();
                    if (c.changed)
                    {
                        UpdateTexture(ramp);
                    }
                }
            });
            var transitionLabel = new Label("Vertical Transition");
            var modeChoices = new List<GradientMode>() { GradientMode.Blend, GradientMode.Fixed };
            var transitionMode = new EnumField();
            transitionMode.BindProperty(serializedObject.FindProperty("VerticalTransitionMode"));
            transitionMode.RegisterValueChangedCallback<Enum>(e =>
            {
                if (e.newValue != null)
                {
                    ramp.VerticalTransitionMode = (GradientMode)e.newValue;
                    UpdateTexture(ramp);
                    UpdatePreview(root);
                }
            });
            var transitionCurve = new CurveField();
            transitionCurve.style.flexGrow = 1;
            transitionCurve.BindProperty(serializedObject.FindProperty("TransitionCurve"));
            transitionCurve.RegisterValueChangedCallback(_ =>
            {
                UpdateTexture(ramp);
                UpdatePreview(root);
            });
            var normalizeCurve = new Toggle()
            {
                tooltip = "Normalize Curve to fit 0-1",
            };
            normalizeCurve.BindProperty(serializedObject.FindProperty("NormalizeCurve"));
            normalizeCurve.RegisterValueChangedCallback<bool>(e =>
            // normalizeCurve.RegisterCallback<ClickEvent>(_ =>
            {
                ramp.NormalizeCurve = e.newValue;
                UpdateTexture(ramp);
                UpdatePreview(root);
            });
            var transitionContainer = new VisualElement() { name = "TransitionContainer" };
            transitionContainer.style.flexDirection = FlexDirection.Row;
            transitionContainer.Add(transitionLabel);
            transitionContainer.Add(transitionMode);
            transitionContainer.Add(transitionCurve);
            transitionContainer.Add(normalizeCurve);
            var label = new Label("Preview");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 14;
            // var size = new Vector2IntField("Texture Size");
            // size.BindProperty(serializedObject.FindProperty("Size"));
            // size.RegisterValueChangedCallback(e =>
            // {
            //     size.value = Vector2Int.Max(e.newValue, Vector2Int.one);
            //     ramp.Texture.Resize(size.value.x, size.value.y);
            //     UpdateTexture(ramp);
            //     UpdatePreview(root);
            // });
            // size.style.paddingBottom = 8;
            var gradientType = new EnumField("Gradient Type", ramp.GradientType);
            gradientType.RegisterValueChangedCallback(e =>
            {
                ramp.GradientType = (ColorRamp.GradientTypes)e.newValue;
                UpdateTexture(ramp);
            });
            var choices = new List<int>() { 2, 3, 4, 5, 6, 7, 8 };
            var sizeW = new PopupField<int>("Texture Size", choices, choices.IndexOf((int)Mathf.Log(ramp.Texture.width, 2)))
            {
                formatListItemCallback = i => ((int)Mathf.Pow(2, i)).ToString(),
                formatSelectedValueCallback = i => ((int)Mathf.Pow(2, i)).ToString()
            };
            sizeW.RegisterValueChangedCallback(e =>
            {
#if UNITY_2021_2_OR_NEWER
                ramp.Texture.Reinitialize((int)Mathf.Pow(2, e.newValue), ramp.Texture.height);
#else
                ramp.Texture.Resize((int)Mathf.Pow(2, e.newValue), ramp.Texture.height);
#endif
                UpdateTexture(ramp);
                UpdatePreview(root);
            });
            var sizeH = new PopupField<int>(choices, choices.IndexOf((int)Mathf.Log(ramp.Texture.height, 2)))
            {
                formatListItemCallback = i => ((int)Mathf.Pow(2, i)).ToString(),
                formatSelectedValueCallback = i => ((int)Mathf.Pow(2, i)).ToString()
            };
            sizeH.RegisterValueChangedCallback(e =>
            {
#if UNITY_2021_2_OR_NEWER
                ramp.Texture.Reinitialize(ramp.Texture.width, (int)Mathf.Pow(2, e.newValue));
#else
                ramp.Texture.Resize(ramp.Texture.width, (int)Mathf.Pow(2, e.newValue));
#endif
                UpdateTexture(ramp);
                UpdatePreview(root);
            });
            var linear = new Toggle("Linear")
            {
                value = ramp.isLinear
            };
            linear.RegisterValueChangedCallback(e =>
            {
                UpdateTexture(ramp);
                EditorUtility.SetDirty(ramp);
                AssetDatabase.SaveAssetIfDirty(ramp);
                ramp.isLinear = e.newValue;
                var path = AssetDatabase.GetAssetPath(ramp);
                var text = File.ReadAllText(path);
                // m_ColorSpace => sRGB: 0, linear: 1
                text = Regex.Replace(text, $"m_ColorSpace: {(e.newValue ? 1 : 0)}", $"m_ColorSpace: {(e.newValue ? 0 : 1)}");
                text = Regex.Replace(text, $"isLinear: {(e.newValue ? 0 : 1)}", $"isLinear: {(e.newValue ? 1 : 0)}");
                File.WriteAllText(path, text);
                AssetDatabase.Refresh();
            });
            var tex = new Image
            {
                image = ramp.Texture,
                scaleMode = ScaleMode.ScaleToFit
            };
            var sizeContainer = new VisualElement();
            sizeContainer.style.flexDirection = FlexDirection.Row;
            sizeContainer.style.paddingBottom = 8;
            sizeContainer.Add(sizeW);
            sizeContainer.Add(sizeH);
            var refreshButton = new Button(() => UpdateTexture(ramp)) { text = " Refresh Preview " };
            refreshButton.style.alignSelf = Align.FlexEnd;
            refreshButton.style.paddingBottom = refreshButton.style.paddingLeft = refreshButton.style.paddingRight = refreshButton.style.paddingTop = 4;
            root.Add(imgui);
            root.Add(transitionContainer);
            // root.Add(transitionCurve);
            // root.Add(size);
            root.Add(gradientType);
            root.Add(sizeContainer);
            root.Add(linear);
            root.Add(label);
            root.Add(tex);
            root.Add(refreshButton);
            root.RegisterCallback<GeometryChangedEvent>(_ => UpdatePreview(root));
            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Save();
                UpdateTexture(ramp);
            });
            return root;
        }

        private void UpdatePreview(VisualElement root)
        {
            if (ramp.Texture.width >= ramp.Texture.height)
            {
                root.Q<Image>().style.width = root.layout.width;
                root.Q<Image>().style.height = root.layout.width * (float)ramp.Texture.height / ramp.Texture.width;
            }
            else
            {
                root.Q<Image>().style.width = root.layout.width * (float)ramp.Texture.width / ramp.Texture.height;
                root.Q<Image>().style.height = root.layout.width;
            }
            var transitionContainer = root.Q<VisualElement>("TransitionContainer");
            transitionContainer.style.display = ramp.Ramps.Length > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            // transitionContainer.Q<CurveField>().visible = ramp.VerticalTransitionMode == GradientMode.Blend;
            // transitionContainer.Q<Toggle>().visible = ramp.VerticalTransitionMode == GradientMode.Blend;
        }

        private void UpdateTexture(ColorRamp ramp)
        {
            // normalize the curve so you just need to deal with the shape, and not the exact values
            var keys = ramp.TransitionCurve.keys;
            float minT = 0, maxT = 1, minV = 0, maxV = 1;
            if (ramp.NormalizeCurve)
            {
                minT = keys[0].time;
                maxT = keys[keys.Length - 1].time;
                minV = Mathf.Infinity;
                maxV = Mathf.NegativeInfinity;
                for (int y = 0; y < ramp.Texture.height; y++)
                {
                    float y0 = (float)y / ramp.Texture.height;
                    float v = ramp.TransitionCurve.Evaluate(Remap(y0, 0, 1, minT, maxT));
                    minV = Mathf.Min(minV, v);
                    maxV = Mathf.Max(maxV, v);
                }
            }
            var colors = new Color[ramp.Texture.width * ramp.Texture.height];
            for (int y = 0; y < ramp.Texture.height; y++)
            {
                for (int x = 0; x < ramp.Texture.width; x++)
                {
                    var t0 = (float)x / (ramp.Texture.width - 1);
                    var y0 = ((float)y / (ramp.Texture.height - 1));
                    switch (ramp.GradientType)
                    {
                        case ColorRamp.GradientTypes.Horizontal:
                            break;
                        case ColorRamp.GradientTypes.Vertical:
                            (t0, y0) = (y0, t0);
                            break;
                        case ColorRamp.GradientTypes.Radial:
                            t0 = t0 * 2 - 1;
                            y0 = y0 * 2 - 1;
                            var r = new Vector2(t0, y0).magnitude;
                            var theta = Mathf.Atan2(y0, t0) * Mathf.Rad2Deg / 360 + 0.5f;
                            t0 = r;
                            y0 = theta;
                            break;
                    }
                    switch (ramp.VerticalTransitionMode)
                    {
                        case GradientMode.Blend:
                            y0 = Remap(y0, 0, 1, minT, maxT);
                            // y0 = 1 - Mathf.Clamp01(ramp.TransitionCurve.Evaluate(y0));
                            y0 = 1 - Remap(ramp.TransitionCurve.Evaluate(y0), minV, maxV, 0, 1, true);
                            break;
                        case GradientMode.Fixed:
                            y0 = Remap(y0, 0, 1, minT, maxT);
                            // y0 = 1 - Mathf.Clamp01(ramp.TransitionCurve.Evaluate(y0));
                            y0 = Remap(ramp.TransitionCurve.Evaluate(y0), minV, maxV, 0, 1, true);
                            y0 = 1 - Mathf.Floor(y0 * (ramp.Ramps.Length)) / ramp.Ramps.Length;
                            break;
                    }
                    var i0 = Mathf.FloorToInt((ramp.Ramps.Length - 1) * y0);
                    var i1 = Mathf.CeilToInt((ramp.Ramps.Length - 1) * y0);
                    var c0 = ramp.Ramps[i0].Smoothstep ? EvaluateSmooth(ramp.Ramps[i0].Gradient, t0) : ramp.Ramps[i0].Gradient.Evaluate(t0);
                    var c1 = ramp.Ramps[i1].Smoothstep ? EvaluateSmooth(ramp.Ramps[i1].Gradient, t0) : ramp.Ramps[i1].Gradient.Evaluate(t0);
                    switch (ramp.VerticalTransitionMode)
                    {
                        case GradientMode.Blend:
                            var l0 = ramp.Ramps.Length > 1 ? (float)i0 / (ramp.Ramps.Length - 1) : 0;
                            var l1 = ramp.Ramps.Length > 1 ? (float)i1 / (ramp.Ramps.Length - 1) : 1;
                            colors[y * ramp.Texture.width + x] = Color.Lerp(c0, c1, Mathf.InverseLerp(l0, l1, y0));
                            break;
                        case GradientMode.Fixed:
                            colors[y * ramp.Texture.width + x] = c0;
                            break;
                    }
                }
            }
            ramp.Texture.SetPixels(colors);
            ramp.Texture.Apply();
            EditorUtility.SetDirty(ramp);
        }

        private float Remap(float value, float oldMin, float oldMax, float newMin, float newMax, bool clamp = true)
        {
            value = (value - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
            return clamp ? Mathf.Clamp(value, newMin, newMax) : value;
        }

        private Color EvaluateSmooth(Gradient gradient, float t)
        {
            t = Mathf.Clamp01(t);
            var c = Color.clear;
            var minT = gradient.colorKeys[0].time;
            for (int i = gradient.colorKeys.Length - 1; i >= 0; i--)
            {
                if (t <= minT)
                {
                    c = gradient.colorKeys[0].color;
                    break;
                }
                else if (t > gradient.colorKeys[i].time)
                {
                    var i1 = Mathf.Min(i + 1, gradient.colorKeys.Length - 1);
                    float t0 = Mathf.InverseLerp(gradient.colorKeys[i].time, gradient.colorKeys[i1].time, t);
                    c = Color.Lerp(gradient.colorKeys[i].color, gradient.colorKeys[i1].color, Mathf.SmoothStep(0, 1, t0));
                    break;
                }
            }
            minT = gradient.alphaKeys[0].time;
            for (int i = gradient.alphaKeys.Length - 1; i >= 0; i--)
            {
                if (t <= minT)
                {
                    c.a = gradient.alphaKeys[0].alpha;
                    break;
                }
                else if (t > gradient.alphaKeys[i].time)
                {
                    var i1 = Mathf.Min(i + 1, gradient.alphaKeys.Length - 1);
                    float t0 = Mathf.InverseLerp(gradient.alphaKeys[i].time, gradient.alphaKeys[i1].time, t);
                    c.a = Mathf.Lerp(gradient.alphaKeys[i].alpha, gradient.alphaKeys[i1].alpha, Mathf.SmoothStep(0, 1, t0));
                    break;
                }
            }
            return c;
        }

        private void Save()
        {
            if (ramp.Texture.name != ramp.name)
            {
                ramp.Texture.name = ramp.name;
                EditorUtility.SetDirty(ramp.Texture);
            }
            if (AssetDatabase.Contains(ramp) && !AssetDatabase.Contains(ramp.Texture))
            {
                AssetDatabase.AddObjectToAsset(ramp.Texture, ramp);
                AssetDatabase.SaveAssets();
                EditorApplication.delayCall += AssetDatabase.Refresh;
            }
        }
    }
}