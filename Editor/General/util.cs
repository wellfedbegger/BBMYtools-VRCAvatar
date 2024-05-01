
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor.Animations;
using System.Runtime.CompilerServices;
using System.IO;
using System.Data;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace bbmy
{
    public static class Util
    {

        public static void TooltipButton(string text)
        {
            if (GUILayout.Button("?", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
            {
                var mouseRect = new Rect(Event.current.mousePosition, Vector2.one);
                var content = new Util.PopupContent(text);
                PopupWindow.Show(mouseRect, content);
            }
        }

        class PopupContent : PopupWindowContent
        {
            private string displayText;
            private Vector2 windowSize;

            public PopupContent(string text)
            {
                displayText = text;

                // テキストのサイズを計算するためのGUIStyle
                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.wordWrap = true;

                // テキストのサイズを計算
                Vector2 textSize = textStyle.CalcSize(new GUIContent(displayText));
                // ウィンドウのサイズをテキストのサイズに基づいて設定
                windowSize = new Vector2(textSize.x + 20, textSize.y + 20);
            }

            public override Vector2 GetWindowSize()
            {
                return windowSize; // 計算したウィンドウサイズを返す
            }

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.LabelField(displayText, GUILayout.ExpandHeight(true));
            }
            public override void OnOpen() { }
            public override void OnClose() { }
        }


        public class JsonData
        {
            Dictionary<string, int> config = new Dictionary<string, int>();
            public JsonData(string filePath)
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    config = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                }
            }
        }

        public class CsvData
        {
            private readonly Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>();
            private readonly List<string> headers = new List<string>();
            private readonly List<string> indices = new List<string>();

            public CsvData(string filePath, char separator)
            {
                using (var reader = new StreamReader(filePath))
                {
                    string[] headerRow;
                    if (separator == ',')
                    {
                        headerRow = ParseLine(reader.ReadLine(), separator);
                    }
                    else
                    {
                        headerRow = reader.ReadLine().Split(separator);
                    }
                    headers.AddRange(headerRow.Skip(1));

                    while (!reader.EndOfStream)
                    {
                        string[] row;
                        if (separator == ',')
                        {
                            row = ParseLine(reader.ReadLine(), separator);
                        }
                        else
                        {
                            row = reader.ReadLine().Split(separator);
                        }
                        indices.Add(row[0]);
                        data[row[0]] = new Dictionary<string, string>();
                        for (int i = 1; i < row.Length; i++)
                        {
                            data[row[0]][headerRow[i]] = row[i].Replace("\\n", "\n").Replace("\\t", "\t");
                        }
                    }
                }
            }

            string[] ParseLine(string line, char separator)
            {
                var pattern = $"(?:^|{separator})(\"(?:[^\"]+|\"\")*\"|[^{separator}]*)";
                var matches = Regex.Matches(line, pattern);
                return matches.Cast<Match>().Select(m => m.Groups[1].Value.Trim('\"')).ToArray();
            }

            public string Get(string rowIndex, string columnIndex)
            {
                if (data.ContainsKey(rowIndex) && data[rowIndex].ContainsKey(columnIndex))
                {
                    return data[rowIndex][columnIndex];
                }
                return null;
            }

            public string Get(int rowIndex, int columnIndex)
            {
                if (rowIndex < indices.Count && columnIndex < headers.Count)
                {
                    return Get(indices[rowIndex], headers[columnIndex]);
                }
                return null;
            }

            public List<string> GetColumn(string columnIndex)
            {
                if (headers.Contains(columnIndex))
                {
                    return indices.Select(index => data[index][columnIndex]).ToList();
                }
                return null;
            }

            public List<string> GetColumn(int columnIndex)
            {
                if (columnIndex < headers.Count)
                {
                    return GetColumn(headers[columnIndex]);
                }
                return null;
            }

            public List<string> GetRow(string rowIndex)
            {
                if (indices.Contains(rowIndex))
                {
                    return headers.Select(column => data[rowIndex][column]).ToList();
                }
                return null;
            }

            public List<string> GetRow(int rowIndex)
            {
                if (rowIndex < indices.Count)
                {
                    return GetRow(indices[rowIndex]);
                }
                return null;
            }
            public List<string> GetIndex() => new List<string>(indices);
            public List<string> GetHeader() => new List<string>(headers);

        }


        public static string GetCurrentFilePath([CallerFilePath] string fileName = null)
        {
            return fileName;
        }

        public static string GetCurrentFileDir([CallerFilePath] string fileName = null)
        {
            return System.IO.Path.GetDirectoryName(fileName);
        }


        public static string GetPath(Transform tr, bool includeTop = true)
        {
            List<string> path_list = new List<string>() { tr.gameObject.name };
            Transform parent = tr.parent;

            while (parent != null)
            {
                path_list.Insert(0, parent.name);
                parent = parent.parent;
            }
            if (includeTop == false)
            {
                path_list.RemoveAt(0);
            }
            // Debug.Log($"{path_list.Count}");
            // Debug.Log(String.Join("/", path_list) == "");
            return String.Join("/", path_list);
        }
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> collection)
        {
            return collection ?? Enumerable.Empty<T>();
        }
        public static T[] OrEmptyIfNull<T>(this T[] collection)
        {
            return collection ?? new T[] { };
        }

        public static void Extend<T>(this List<T> collection, int count)
        {
            while (count > collection.Count())
            {
                collection.Add(default(T)); // 追加する要素はデフォルト値と仮定
            }
        }
        public static Texture2D Texture2DColor(float r, float g, float b, float a)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, new Color(r, g, b, a));
            tex.Apply();
            return tex;
        }

        public static void LabeltoSelectObject(string text, UnityEngine.Object obj)
        {
            EditorGUILayout.LabelField(text, EditorStyles.linkLabel);
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                Selection.activeObject = obj;
        }

        public static void LabeltoSelectObject(string text, UnityEngine.Object obj, GUIStyle style)
        {
            EditorGUILayout.LabelField(text, style);
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                Selection.activeObject = obj;
        }

        /// <summary>
        /// Animator の Parameter 一覧から指定名称のものを探してインデックスを返す
        /// </summary>
        /// <returns>インデックス  見つからない場合-1</returns>
        public static int FindAnimatorControllerParameter(AnimatorController animator, string parameterName)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName)
                {
                    return i;
                }
            }
            return -1; // 見つからなかった場合
        }
    }
}