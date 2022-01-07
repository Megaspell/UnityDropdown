﻿namespace UnityDropdown.Editor
{
    using System;
    using SolidUtilities;
    using SolidUtilities.Editor.Helpers;
    using UnityEditor;
    using UnityEngine;

    public enum DropdownWindowType { Dropdown, Popup }

    /// <summary>Creates a dropdown window that shows the <see cref="DropdownTree"/> elements.</summary>
    public partial class DropdownWindow : EditorWindow
    {
        public const string NoneElementName = "(None)";

        private DropdownTree _dropdownTree;

        public static DropdownWindow Create(DropdownTree dropdownTree, DropdownWindowType windowType, Vector2? customWindowPosition = null, int windowHeight = 0)
        {
            var window = CreateInstance<DropdownWindow>();
            window.OnCreate(dropdownTree, windowHeight, GetWindowPosition(customWindowPosition, dropdownTree, windowType), windowType);
            return window;
        }

        private static Vector2 GetWindowPosition(Vector2? customWindowPosition, DropdownTree dropdownTree, DropdownWindowType windowType)
        {
            if (customWindowPosition != null)
                return customWindowPosition.Value;

            return windowType switch
            {
                DropdownWindowType.Dropdown => GUIUtility.GUIToScreenPoint(Event.current.mousePosition),
                DropdownWindowType.Popup => GetCenteredPosition(dropdownTree),
                _ => throw new NotImplementedException()
            };
        }

        private static Vector2 GetCenteredPosition(DropdownTree dropdownTree)
        {
            Vector2 dropdownPosition = EditorGUIUtilityHelper.GetMainWindowPosition().center;
            dropdownPosition.x -= CalculateOptimalWidth(dropdownTree.SelectionPaths) / 2f;
            return dropdownPosition.RoundUp();
        }

        /// <summary>
        /// This is basically a constructor. Since ScriptableObjects cannot have constructors,
        /// this one is called from a factory method.
        /// </summary>
        /// <param name="dropdownTree">Tree that contains the dropdown items to show.</param>
        /// <param name="windowHeight">Height of the window. If set to 0, it will be auto-adjusted.</param>
        /// <param name="windowPosition">Position of the window to set.</param>
        private void OnCreate(DropdownTree dropdownTree, float windowHeight, Vector2 windowPosition, DropdownWindowType windowType)
        {
            ResetControl();
            wantsMouseMove = true;
            _dropdownTree = dropdownTree;
            _dropdownTree.SelectionChanged += Close;
            _optimalWidth = CalculateOptimalWidth(_dropdownTree.SelectionPaths);
            _preventExpandingHeight = new PreventExpandingHeight(windowHeight == 0f);

            _positionOnCreation = GetWindowRect(windowPosition, windowHeight);

            if (windowType == DropdownWindowType.Dropdown)
            {
                // ShowAsDropDown usually shows the window under a button, but since we don't need to align the window to
                // any button, we set buttonRect.height to 0f.
                Rect buttonRect = new Rect(_positionOnCreation) { height = 0f };
                ShowAsDropDown(buttonRect, _positionOnCreation.size);
            }
            else if (windowType == DropdownWindowType.Popup)
            {
                position = _positionOnCreation;
                ShowPopup();
            }
            else
            {
                throw new Exception("Unknown window type");
            }
        }

        private void OnGUI()
        {
            CloseOnEscPress();
            DrawContent();
            RepaintIfMouseWasUsed();
        }

        private void Update()
        {
            // Sometimes, Unity resets the window position to 0,0 after showing it as a drop-down, so it is necessary
            // to set it again once the window was created.
            if (!_positionWasSetAfterCreation)
            {
                _positionWasSetAfterCreation = true;
                position = _positionOnCreation;
            }

            // If called in OnGUI, the dropdown blinks before appearing for some reason. Thus, it works well only in Update.
            AdjustSizeIfNeeded();
        }

        private void OnLostFocus() => Close();

        private static void ResetControl()
        {
            GUIUtility.hotControl = 0;
            GUIUtility.keyboardControl = 0;
        }

        private void CloseOnEscPress()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }
        }

        private void DrawContent()
        {
            using (new FixedRect(_preventExpandingHeight, position.width))
            {
                using (EditorGUILayoutHelper.VerticalBlock(_preventExpandingHeight,
                    DropdownStyle.BackgroundColor, out float contentHeight))
                {
                    _dropdownTree.Draw();

                    if (Event.current.type == EventType.Repaint)
                        _contentHeight = contentHeight;
                }

                EditorGUIHelper.DrawBorders(position.width, position.height, DropdownStyle.BorderColor);
            }
        }

        private void RepaintIfMouseWasUsed()
        {
            if (Event.current.isMouse || Event.current.type == EventType.Used || _dropdownTree.RepaintRequested)
            {
                Repaint();
                _dropdownTree.RepaintRequested = false;
            }
        }

        private readonly struct FixedRect : IDisposable
        {
            private readonly bool _enable;

            public FixedRect(bool enable, float windowWidth)
            {
                _enable = enable;

                if (_enable)
                    GUILayout.BeginArea(new Rect(0f, 0f, windowWidth, DropdownStyle.MaxWindowHeight));
            }

            public void Dispose()
            {
                if (_enable)
                    GUILayout.EndArea();
            }
        }
    }
}