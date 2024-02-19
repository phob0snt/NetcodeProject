﻿using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.Multiplayer.Tools.NetworkProfiler.Editor
{
    class DetailsViewPersistentState : ScriptableObject
    {
        static DetailsViewPersistentState s_StateObject;

        [SerializeField]
        DetailsViewFoldoutState m_FoldoutState = new DetailsViewFoldoutState();

        [SerializeField]
        DetailsViewSelectedState m_SelectedState = new DetailsViewSelectedState();

        [SerializeField]
        string m_SearchBarString = null;

        public static string MostRecentlySelected
        {
            get => GetOrCreateStateObject().m_SelectedState.MostRecentlySelected;
            private set => GetOrCreateStateObject().m_SelectedState.MostRecentlySelected = value;
        }

        public static string SearchBarString
        {
            get => GetOrCreateStateObject().m_SearchBarString;
            set => GetOrCreateStateObject().m_SearchBarString = value;
        }

        static int? StateObjectInstanceId
        {
            get
            {
                var stateObjectInstanceId = SessionState.GetInt(nameof(DetailsViewPersistentState), -1);
                if (stateObjectInstanceId == -1)
                {
                    return null;
                }

                return stateObjectInstanceId;
            }
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                SessionState.SetInt(nameof(DetailsViewPersistentState), value.Value);
            }
        }

        static DetailsViewPersistentState GetOrCreateStateObject()
        {
            if (s_StateObject)
            {
                return s_StateObject;
            }

            var maybeInstanceId = StateObjectInstanceId;
            if (maybeInstanceId.HasValue)
            {
                s_StateObject = EditorUtility.InstanceIDToObject(maybeInstanceId.Value) as DetailsViewPersistentState;
            }

            if (!s_StateObject)
            {
                s_StateObject = CreateInstance<DetailsViewPersistentState>();
                s_StateObject.hideFlags = HideFlags.HideAndDontSave;
                StateObjectInstanceId = s_StateObject.GetInstanceID();
            }

            return s_StateObject;
        }

        public static bool IsFoldedOut(string locator)
            => GetOrCreateStateObject().m_FoldoutState.IsFoldedOut(locator);

        public static void SetFoldout(string locator, bool isFoldedOut)
            => GetOrCreateStateObject().m_FoldoutState.SetFoldout(locator, isFoldedOut);

        public static void SetFoldoutExpandAll()
            => GetOrCreateStateObject().m_FoldoutState.SetFoldoutExpandAll();

        public static void SetFoldoutContractAll()
            => GetOrCreateStateObject().m_FoldoutState.SetFoldoutContractAll();

        public static bool IsSelected(string locator)
            => GetOrCreateStateObject().m_SelectedState.IsSelected(locator);

        public static void SetSelected(IReadOnlyList<string> locators)
        {
            GetOrCreateStateObject().m_SelectedState.SetSelected(locators);
            MostRecentlySelected = locators.Count > 0 ? locators[locators.Count - 1] : null;
        }
    }
}
