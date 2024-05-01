using UnityEngine;
using UnityEditor;

namespace bbmy
{
    namespace PlaceObjects
    {
        class Editor : EditorWindow
        {
            // const string LANGDATA_RELPATH = "./lang.csv";
            // const char LANGDATA_SEPARATOR = '\t';

            SerializedObject serializedObject;
            Vector2 _scrollPosition = Vector2.zero;
            // LocalizeHelper localizeHelper;

            bool showInfo = false;

            // =============================
            // Items 
            // =============================
            PlaceObjects placeObjects = null;
            // LangTest langtest = null;
            // =======================================================

            [MenuItem("Tools/BBMYtools - Avatar/PlaceObjects")]
            static void ShowWindow()
            {
                GetWindow<Editor>("PlaceObjects");
            }

            void OnEnable()
            {
                serializedObject = new SerializedObject(this);

                placeObjects = ScriptableObject.CreateInstance<PlaceObjects>();
                placeObjects.Enable();
                // string langDatPath = Path.Combine(Util.GetCurrentFileDir(), LANGDATA_RELPATH);
                // localizeHelper = new LocalizeHelper(langDatPath, LANGDATA_SEPARATOR);
            }
            void OnGUI()
            {
                if (serializedObject == null || serializedObject.targetObject == null)
                {
                    serializedObject = new SerializedObject(this);
                }

                serializedObject.Update();

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    string message = "「Objects」の各オブジェクトの位置・回転・スケールを、「Anchors」の各オブジェクトに一致させます。"
                                    // + "\n※スケールは変更されません。"
                                    + "\n\n「Objectsの要素n」→「Anchorsの要素n」に合わせられます。"
                                    + "\n必要に応じてリストの並び替えを行ってください。"
                                    + "\n\n「Apply」後、Ctrl+Zで元に戻すことが出来ます。";

                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.fontSize = 12; // Change to your desired font size.
                    style.wordWrap = true;

                    showInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showInfo, "説明を表示");
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    if (showInfo)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            // Display the icon
                            GUILayout.Box(EditorGUIUtility.IconContent("console.infoicon"), GUIStyle.none, GUILayout.Width(32), GUILayout.Height(32));
                            // Display the text
                            GUILayout.Label(message, style);
                        }
                        GUILayout.Space(style.fontSize * 0.4f);
                    }

                }
                // =======================================================

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);


                placeObjects.GUI();


                EditorGUILayout.EndScrollView();
                serializedObject.ApplyModifiedProperties();
            }
        }


    }
}





