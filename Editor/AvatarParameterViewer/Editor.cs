using UnityEngine;
using UnityEditor;
using System.Collections.Immutable;
using System.IO;
using VRC.SDK3.Avatars.Components;

namespace bbmy
{
    namespace AvatarParameterViewer
    {
        class Editor : EditorWindow
        {
            const string LANGDATA_RELPATH = "./lang.csv";
            const char LANGDATA_SEPARATOR = '\t';

            SerializedObject serializedObject;
            Vector2 _scrollPosition = Vector2.zero;
            LocalizeHelper localizeHelper;


            readonly ImmutableArray<string> _reservedParams = ImmutableArray.Create(
                "IsLocal",
                "Viseme",
                "Voice",
                "GestureLeft",
                "GestureRight",
                "Gesture LeftWeight",
                "GestureRightWeight",
                "AngularY",
                "VelocityX",
                "VelocityY",
                "VelocityZ",
                "VelocityMagnitude",
                "Upright",
                "Grounded",
                "Seated",
                "AFK",
                "TrackingType",
                "VRMode",
                "MuteSelf",
                "InStation",
                "Earmuffs"
                );

            [SerializeField] VRCAvatarDescriptor avatar = null;
            [SerializeField] string parameter_name = null;
            [SerializeField] string new_parameter_name = null;
            [SerializeField] bool is_physbone_mode = false;

            [MenuItem("Tools/BBMYtools - Avatar/AvatarParameterViewer")]
            static void ShowWindow()
            {
                GetWindow<Editor>("AvatarParameterViewer");
            }
            void OnEnable()
            {
                serializedObject = new SerializedObject(this);
                string langDatPath = Path.Combine(Util.GetCurrentFileDir(), LANGDATA_RELPATH);
                localizeHelper = new LocalizeHelper(langDatPath, LANGDATA_SEPARATOR);
            }
            void OnGUI()
            {
                if (serializedObject == null || serializedObject.targetObject == null)
                {
                    serializedObject = new SerializedObject(this);
                }

                serializedObject.Update();


                if (GUILayout.Button("RefreshUI"))
                {
                    string langDatPath = Path.Combine(Util.GetCurrentFileDir(), LANGDATA_RELPATH);
                    localizeHelper = new LocalizeHelper(langDatPath, LANGDATA_SEPARATOR);
                }

                localizeHelper.SelectLanguageGUI();

                // スクロール開始位置
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                // PhysBonesモード
                using (new GUILayout.HorizontalScope())
                {
                    Util.TooltipButton(
                    localizeHelper.GetLocalizedText("sPBModeDescription") + "\n\n{Parameter}_IsGrabbed\n{Parameter}_Angle\n{Parameter}_Stretch\n{Parameter}_Squish"
                                        );
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("is_physbone_mode"), new GUIContent("PhysBone Mode"));
                    Searcher.is_physbone_mode = is_physbone_mode;
                }

                // EditorGUILayout.PropertyField(serializedObject.FindProperty("include_sdk_default_layers"), new GUIContent("SDK default",
                // "Whether to include the default PlayableLayer provided with VRCSDK in the search target when a custom one is not specified."));

                // アバターの指定
                EditorGUILayout.PropertyField(serializedObject.FindProperty("avatar"));
                Searcher.avatar = avatar;


                // パラメータ名
                if (!is_physbone_mode)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("parameter_name"), new GUIContent("Parameter name"));
                else
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("parameter_name"), new GUIContent("PB Parameter basename",
                    "input \"hanyahanya\" here, then it will Find\n\thanyahanya_IsGrabbed\n\thanyahanya_Angle\n\thanyahanya_Stretch"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("new_parameter_name"), new GUIContent("New name"));

                using (new GUILayout.HorizontalScope())
                {
                    // 検索
                    if (GUILayout.Button("Search") && Searcher.avatar != null)
                    {
                        Searcher.Search(parameter_name);
                    }
                    // 置換
                    if (GUILayout.Button("Rename") && !string.IsNullOrEmpty(new_parameter_name))
                    {
                        var let = false;
                        Searcher.Search(parameter_name);
                        if (EditorUtility.DisplayDialog("confirmation", "Rename all shown parameters.\nOK?", "Go", "Cancel"))
                        {
                            if (_reservedParams.Contains(parameter_name))
                            {
                                if (EditorUtility.DisplayDialog("confirmation", "You are attempting to rename a VRCSDK reserved parameter.\nThis replacement may cause unexpected behavior.\nAre you sure?", "Go", "Cancel"))
                                    let = true;
                            }
                            else if (_reservedParams.Contains(new_parameter_name))
                            {
                                if (EditorUtility.DisplayDialog("confirmation", "The new parameter name is reserved by VRCSDK.\nThis replacement may cause unexpected behavior.\nAre you sure?", "Go", "Cancel"))
                                    let = true;
                            }
                            else
                                let = true;
                        }
                        if (let)
                            Searcher.Rename(parameter_name, new_parameter_name);
                    }
                }
                Searcher.Show();

                EditorGUILayout.EndScrollView();

                serializedObject.ApplyModifiedProperties();

            }
        }
    }
}