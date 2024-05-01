
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace bbmy
{
	namespace PlaceObjects
	{

		public class PlaceObjects : ScriptableObject
		{
			SerializedObject serializedObject;

			[SerializeField] List<GameObject> anchors = new List<GameObject>();
			[SerializeField] List<GameObject> objs = new List<GameObject>();
			[SerializeField] bool position = true;
			[SerializeField] bool rotation = true;
			[SerializeField] bool scale = false;


			bool isEnoughAnchors = false;

			public void Enable()
			{
				serializedObject = new SerializedObject(this);
			}
			public void GUI()
			{

				if (serializedObject == null || serializedObject.targetObject == null)
				{
					serializedObject = new SerializedObject(this);
				}

				serializedObject.Update();

				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.PropertyField(serializedObject.FindProperty("objs"), new GUIContent("Objects"), true);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("anchors"), new GUIContent("Anchors"), true);
				}

				// Anchorが足りなかったらエラー
				isEnoughAnchors = objs.Count - anchors.Count <= 0;
				// Debug.Log($"{let}");
				if (isEnoughAnchors == false)
				{
					EditorGUILayout.HelpBox($"Objects {objs.Count} 個 > Anchor {anchors.Count} 個\n" +
					$"Anchorの数が足りません。あと {objs.Count - anchors.Count}個 Anchorを追加して下さい",
					MessageType.Error);
				}
				// EditorGUILayout.BeginHorizontal();
				// {
				// 	EditorGUILayout.LabelField("Global");

				// 	EditorGUILayout.PropertyField(serializedObject.FindProperty("position"));
				// 	GUILayout.FlexibleSpace();
				// }
				// EditorGUILayout.EndHorizontal(); 
				// using (new GUILayout.HorizontalScope())
				// {
				// 	EditorGUILayout.LabelField("Global");
				// 	EditorGUILayout.PropertyField(serializedObject.FindProperty("position"));
				// }
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("(Global)", GUILayout.Width(50)); // 幅を指定して左寄せにする
					EditorGUILayout.PropertyField(serializedObject.FindProperty("position")); // ExpandWidthをfalseにすると余分なスペースを取らない
				}
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("(Global)", GUILayout.Width(50)); // 幅を指定して左寄せにする
					EditorGUILayout.PropertyField(serializedObject.FindProperty("rotation"));
				}
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("(Local)", GUILayout.Width(50)); // 幅を指定して左寄せにする
					EditorGUILayout.PropertyField(serializedObject.FindProperty("scale"));
				}
				// }


				if (GUILayout.Button("Apply"))
					Place();

				void Place()
				{
					if (isEnoughAnchors)
					{
						// foreach (var ach in anchors.OrEmptyIfNull()){
						//   Debug.Log($"{ach.name}\t{ach.transform.position}\t{ach.transform.rotation}");
						// }
						foreach (int i in Enumerable.Range(0, objs.Count()))
						{
							// Debug.Log($"{anchors[i].name} ");
							Undo.RecordObject(objs[i].transform, "Update object transform");
							if (position == true)
								objs[i].transform.position = anchors[i].transform.position;
							if (rotation == true)
								objs[i].transform.rotation = anchors[i].transform.rotation;
							if (scale == true)
								objs[i].transform.localScale = anchors[i].transform.localScale;


						}
					}
				}
				serializedObject.ApplyModifiedProperties();
			}
		}
	}

}