using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TeckArtist.Tools
{
    public class MaterialColorRampDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            // base.OnGUI(position, prop, label, editor);
            EditorGUI.BeginChangeCheck();
            label.tooltip = "Field accepts either Textures or ColorRamp assets";
            prop.textureValue = EditorGUI.ObjectField(position, label, prop.textureValue, typeof(Texture2D), false) as Texture;
            Event e = Event.current;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0;
            Rect previewRect = new Rect(position);
            previewRect.xMin += EditorGUIUtility.labelWidth;
            previewRect.xMax -= EditorGUIUtility.fieldWidth;
            previewRect.yMin += 3;
            previewRect.yMax -= 1;
            if (prop.textureValue)
            {
                EditorGUI.DrawPreviewTexture(previewRect, prop.textureValue);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, Color.black);
            }
            EditorGUI.LabelField(previewRect, "Preview");
            Rect dragRect = new Rect(position);
            dragRect.xMin += EditorGUIUtility.labelWidth;
            if (dragRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
                {
                    var o = DragAndDrop.objectReferences[0];
                    if (DragAndDrop.objectReferences.Length == 1 && (o is ColorRamp || o is Texture2D))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (e.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            if (o is ColorRamp)
                            {
                                prop.textureValue = (o as ColorRamp).Texture;
                            }
                            else if (o is Texture2D)
                            {
                                prop.textureValue = o as Texture2D;
                            }
                        }
                    }
                }
            }
            EditorGUIUtility.labelWidth = oldLabelWidth;
            if (EditorGUI.EndChangeCheck())
            {

            }
        }
    }
}