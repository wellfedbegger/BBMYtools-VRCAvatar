using UnityEngine;
using UnityEditor;

// using System.IO;
// #error version

namespace bbmy
{
	public class LocalizeHelper
	{

		const string CONFIGKEY_myLangIndex = "General.myLanguageIdx";
		// const string LANGDATA_RELPATH = "./lang";
		// const char LANGDATA_SEPARATOR = '\t';

		// static LocalizeHelper _instance = null;

		Util.CsvData _langData;
		string[] _langs;
		int _myLangIndex = 0;
		int _prevMyLangIndex = -1;
		string _myLangName;


		// public static LocalizeHelper Instance
		// {
		// 	get
		// 	{
		// 		if (_instance == null)
		// 		{
		// 			_instance = new LocalizeHelper();
		// 		}
		// 		return _instance;
		// 	}
		// }

		public LocalizeHelper(string langDataPath, char DataSeparator)
		{
			// string csvPath = Path.Combine(Util.GetCurrentFileDir(), LANGDATA_RELPATH);
			_langData = new Util.CsvData(langDataPath, DataSeparator);
			_langs = _langData.GetHeader().ToArray();
			_myLangIndex = LoadConfig();
		}

		public void SelectLanguageGUI()
		{
			_myLangIndex = EditorGUILayout.Popup("Language", _myLangIndex, _langs);
			if (_myLangIndex != _prevMyLangIndex)
			{
				Config.Instance.Set<int>(CONFIGKEY_myLangIndex, _myLangIndex);
				_prevMyLangIndex = _myLangIndex;
				_myLangName = _langs[_myLangIndex];
			}
		}

		public string GetLocalizedText(string key)
		{
			var s = _langData.Get(key, _myLangName);
			if (s == null)
			{
				Debug.LogWarning($"key: {key}\tnot found in _langData.");
			}
			return s;
		}

		int LoadConfig()
		{
			var v = Config.Instance.Get(CONFIGKEY_myLangIndex);
			if (v != null)
			{
				return v.ToObject<int>();
			}
			return _myLangIndex;
		}
	}
}