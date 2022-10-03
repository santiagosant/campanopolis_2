/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"PopUpLabelData.cs"
 * 
 *	A data container for PopUp labels that are shared amongst multiple variables.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A data container for PopUp labels that are shared amongst multiple variables.
	 */
	[System.Serializable]
	public class PopUpLabelData : ITranslatable
	{

		[SerializeField] private string editorLabel = "";
		[SerializeField] private int id = 1;
		[SerializeField] private string[] labels = new string[0];
		[SerializeField] private int lineID = -1;
		[SerializeField] private bool canTranslate = false;


		/** The default Constructor */
		public PopUpLabelData (int[] idArray, string[] existingLabels, int _lineID)
		{
			editorLabel = "New label set";
			labels = new string[0];
			lineID = _lineID;

			id = 1;
			foreach (int _id in idArray)
			{
				if (id == _id)
				{
					id ++;
				}
			}

			if (existingLabels != null && existingLabels.Length > 0)
			{
				labels = new string[existingLabels.Length];
				for (int i=0; i<existingLabels.Length; i++)
				{
					labels[i] = existingLabels[i];
				}
			}
		}


		#if UNITY_EDITOR

		public void ShowGUI (bool canEdit)
		{
			List<string> popUpList = new List<string>();
			if (labels != null && labels.Length > 0)
			{
				foreach (string p in labels)
				{
					popUpList.Add (p);
				}
			}

			if (canEdit)
			{
				editorLabel = EditorGUILayout.TextField ("Editor label:", editorLabel);

				for (int i=0; i<popUpList.Count; i++)
				{
					EditorGUILayout.BeginHorizontal ();
					popUpList[i] = EditorGUILayout.TextField ("Value " + i.ToString () +":", popUpList[i]);

					if (GUILayout.Button ("-", GUILayout.MaxWidth (20f)))
					{
						popUpList.RemoveAt (i);
						EditorGUIUtility.editingTextField = false;
						i=-1;
					}

					EditorGUILayout.EndHorizontal ();
				}

				if (GUILayout.Button ("Add new value"))
				{
					popUpList.Add (string.Empty);
				}
				labels = popUpList.ToArray ();
			}
			else
			{
				for (int i=0; i<popUpList.Count; i++)
				{
					EditorGUILayout.LabelField ("Value: " + i.ToString () + ": " + popUpList[i]);
				}
			}

			if (canEdit)
			{
				canTranslate = CustomGUILayout.Toggle ("Values can be translated?", canTranslate, "", "If True, the labels can be translated");
			}
			else if (canTranslate)
			{
				EditorGUILayout.LabelField ("Labels can be translated");
			}

			EditorGUILayout.HelpBox ("Changes made to this preset will affect all PopUp variables that refer to it.", MessageType.Info);
		}


		public string[] GenerateEditorPopUpLabels ()
		{
			string[] popUpLabels = new string[labels.Length];
			for (int i=0; i<popUpLabels.Length; i++)
			{
				popUpLabels[i] = GetValue (i);
				if (string.IsNullOrEmpty (popUpLabels[i]))
				{
					popUpLabels[i] = "(Unnamed)";
				}
				popUpLabels[i] = i + ": " + popUpLabels[i];
			}

			return popUpLabels;
		}


		public string EditorLabel
		{
			get
			{
				if (string.IsNullOrEmpty (editorLabel))
				{
					editorLabel = "(Unnamed)";
				}
				return ID.ToString () + ": " + editorLabel;
			}
		}

		#endif


		/**
		 * <summary>Gets a label value</summary>
		 * <param name = "index">The label's index number</param>
		 * <returns>The label</returns>
		 */
		public string GetValue (int index)
		{
			if (index >= 0 && index < Length)
			{
				return labels[index];
			}
			return string.Empty;
		}


		/** A unique identifier */
		public int ID
		{
			get
			{
				return id;
			}
		}


		/** How many labels are defined */
		public int Length
		{
			get
			{
				return labels.Length;
			}
		}


		/** The translation ID, as recorded by the Speech Manager */
		public int LineID
		{
			get
			{
				return lineID;
			}
		}


		/**
		 * <summary>Gets if the data can be translated</summary>
		 * <returns>True if the data can be translated</returns>
		 */
		public bool CanTranslate ()
		{
			if (canTranslate)
			{
				return !string.IsNullOrEmpty (GetPopUpsString ());
			}
			return false;
		}


		/** ITranslatable implementation */

		public virtual string GetTranslatableString (int index)
		{
			return GetPopUpsString ();
		}


		public virtual int GetTranslationID (int index)
		{
			return lineID;
		}


		#if UNITY_EDITOR

		public int GetNumTranslatables ()
		{
			return 1;
		}


		public virtual bool HasExistingTranslation (int index)
		{
			return lineID > -1;
		}


		public virtual void SetTranslationID (int index, int _lineID)
		{
			lineID = _lineID;
		}


		public string GetOwner (int index)
		{
			return string.Empty;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public virtual AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.Variable;
		}


		public virtual bool CanTranslate (int index)
		{
			if (canTranslate)
			{
				return !string.IsNullOrEmpty (GetPopUpsString ());
			}
			return false;
		}

		#endif


		public string GetPopUpsString ()
		{
			string result = string.Empty;
			foreach (string label in labels)
			{
				result += label + "]";
			}
			if (result.Length > 0)
			{
				return result.Substring (0, result.Length-1);
			}
			return string.Empty;
		}

	}

}