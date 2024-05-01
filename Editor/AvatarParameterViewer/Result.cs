using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System.Linq;
using System.Collections.Generic;

using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Dynamics.Contact.Components;

namespace bbmy
{
	namespace AvatarParameterViewer
	{
		// AvatarParameterViewer Data singleton
		public class Data
		{
			public static readonly string[] pbSuffixs = { "", "_IsGrabbed", "_IsPosed", "_Angle", "_Stretch", "_Squish" };

			static Data _instance = null;
			readonly string _filePath;

			public static Data Instance
			{
				get
				{
					if (_instance == null)
					{
						_instance = new Data();
					}
					return _instance;
				}
			}

			Data()
			{
			}

		}
	}
}