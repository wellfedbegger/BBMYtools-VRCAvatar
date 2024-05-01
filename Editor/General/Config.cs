using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace bbmy
{
	// Config Loader singleton
	public class Config
	{
		static Config _instance = null;
		JObject _dict;
		readonly string _filePath;
		// relative path to config from this .cs
		const string CONFIG_RELPATH = "./config";

		public static Config Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Config();
				}
				return _instance;
			}
		}

		Config()
		{
			_filePath = Path.Combine(Util.GetCurrentFileDir(), CONFIG_RELPATH);
			// Debug.LogWarning(_filePath);
			var json = File.ReadAllText(_filePath);
			_dict = JObject.Parse(json);
		}

		public JToken Get(string key)
		{
			// return null if key not found
			return _dict[key];
		}

		public void Set<T>(string key, T value)
		{
			_dict[key] = JToken.FromObject(value);
			var json = JsonConvert.SerializeObject(_dict, Formatting.Indented);
			File.WriteAllText(_filePath, json);
		}
	}
}