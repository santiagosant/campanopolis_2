/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"ActionTransform.cs"
 * 
 *	This action modifies a GameObject position, rotation or scale over a set time.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionTransform : Action
	{

		public bool isPlayer;

		public int markerParameterID = -1;
		public int markerID = 0;
		public Marker marker;
		protected Marker runtimeMarker;

		public bool doEulerRotation = false;
		public bool clearExisting = true;
		public bool inWorldSpace = false;
		
		public AnimationCurve timeCurve = new AnimationCurve (new Keyframe(0, 0), new Keyframe(1, 1));
		
		public int parameterID = -1;
		public int constantID = 0;
		public Moveable linkedProp;
		protected Moveable runtimeLinkedProp;

		public enum SetVectorMethod { EnteredHere, FromVector3Variable };
		public SetVectorMethod setVectorMethod = SetVectorMethod.EnteredHere;

		public int newVectorParameterID = -1;
		public Vector3 newVector;

		public int vectorVarParameterID = -1;
		public int vectorVarID;
		public VariableLocation variableLocation = VariableLocation.Global;

		public float transitionTime;
		public int transitionTimeParameterID = -1;
		
		public TransformType transformType;
		public MoveMethod moveMethod;
		
		public enum ToBy { To, By };
		public ToBy toBy;

		private Vector3 nonSkipTargetVector = Vector3.zero;

		public Variables variables;
		public int variablesConstantID = 0;

		private GVar runtimeVariable;
		private LocalVariables localVariables;

		
		public ActionTransform ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Object;
			title = "Transform";
			description = "Transforms a GameObject over time, by or to a given amount, or towards a Marker in the scene. The GameObject must have a Moveable script attached.";
		}
		
		
		override public void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				if (KickStarter.player != null)
				{
					runtimeLinkedProp = KickStarter.player.GetComponent <Moveable>();
				}
				else
				{
					runtimeLinkedProp = null;
				}
			}
			else
			{
				runtimeLinkedProp = AssignFile <Moveable> (parameters, parameterID, constantID, linkedProp);
			}

			runtimeMarker = AssignFile <Marker> (parameters, markerParameterID, markerID, marker);
			transitionTime = AssignFloat (parameters, transitionTimeParameterID, transitionTime);
			newVector = AssignVector3 (parameters, newVectorParameterID, newVector);

			if (!(transformType == TransformType.CopyMarker ||
				(transformType == TransformType.Translate && toBy == ToBy.To) ||
				(transformType == TransformType.Rotate && toBy == ToBy.To)))
			{
				inWorldSpace = false;
			}

			runtimeVariable = null;
			if (transformType != TransformType.CopyMarker && setVectorMethod == SetVectorMethod.FromVector3Variable)
			{
				switch (variableLocation)
				{
					case VariableLocation.Global:
						vectorVarID = AssignVariableID (parameters, vectorVarParameterID, vectorVarID);
						runtimeVariable = GlobalVariables.GetVariable (vectorVarID, true);
						break;

					case VariableLocation.Local:
						if (!isAssetFile)
						{
							vectorVarID = AssignVariableID (parameters, vectorVarParameterID, vectorVarID);
							runtimeVariable = LocalVariables.GetVariable (vectorVarID, localVariables);
						}
						break;

					case VariableLocation.Component:
						Variables runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
						if (runtimeVariables != null)
						{
							runtimeVariable = runtimeVariables.GetVariable (vectorVarID);
						}
						runtimeVariable = AssignVariable (parameters, vectorVarParameterID, runtimeVariable);
						break;
				}
			}
		}


		override public void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}

			base.AssignParentList (actionList);
		}
		
		
		override public float Run ()	
		{
			if (!isRunning)
			{
				isRunning = true;
				
				if (runtimeLinkedProp != null)
				{
					float _transitionTime = Mathf.Max (transitionTime, 0f);
					RunToTime (_transitionTime, false);
					
					if (willWait && _transitionTime > 0f)
					{
						return (defaultPauseTime);
					}
				}
				else
				{
					if (isPlayer && KickStarter.player != null)
					{
						LogWarning ("The player " + KickStarter.player + " requires a Moveable component to be moved.", KickStarter.player);
					}
				}
			}
			else
			{
				if (runtimeLinkedProp != null)
				{
					if (!runtimeLinkedProp.IsMoving (transformType))
					{
						isRunning = false;
					}
					else
					{
						return defaultPauseTime;
					}
				}
			}
			
			return 0f;
		}
		
		
		override public void Skip ()	
		{
			if (runtimeLinkedProp != null)
			{
				RunToTime (0f, true);
			}
		}
		

		private void RunToTime (float _time, bool isSkipping)
		{
			if (transformType == TransformType.CopyMarker)
			{
				if (runtimeMarker != null)
				{
					runtimeLinkedProp.Move (runtimeMarker, moveMethod, inWorldSpace, _time, timeCurve);
				}
			}
			else
			{
				Vector3 targetVector = Vector3.zero;

				if (setVectorMethod == SetVectorMethod.FromVector3Variable)
				{
					if (runtimeVariable != null)
					{
						targetVector = runtimeVariable.Vector3Value;
					}
				}
				else if (setVectorMethod == SetVectorMethod.EnteredHere)
				{
					targetVector = newVector;
				}

				if (transformType == TransformType.Translate)
				{
					if (toBy == ToBy.By)
					{
						targetVector = SetRelativeTarget (targetVector, isSkipping, runtimeLinkedProp.transform.localPosition);
					}
				}
				else if (transformType == TransformType.Rotate)
				{
					if (toBy == ToBy.By)
					{
						int numZeros = 0;
						if (Mathf.Approximately (targetVector.x, 0f)) numZeros ++;
						if (Mathf.Approximately (targetVector.y, 0f)) numZeros ++;
						if (Mathf.Approximately (targetVector.z, 0f)) numZeros ++;

						if (numZeros == 2)
						{
							targetVector = SetRelativeTarget (targetVector, isSkipping, runtimeLinkedProp.transform.eulerAngles);
						}
						else
						{
							Quaternion currentRotation = runtimeLinkedProp.transform.localRotation;
							runtimeLinkedProp.transform.Rotate (targetVector, Space.World);
							targetVector = runtimeLinkedProp.transform.localEulerAngles;
							runtimeLinkedProp.transform.localRotation = currentRotation;
						}
					}
				}
				else if (transformType == TransformType.Scale)
				{
					if (toBy == ToBy.By)
					{
						targetVector = SetRelativeTarget (targetVector, isSkipping, runtimeLinkedProp.transform.localScale);
					}
				}
				
				if (transformType == TransformType.Rotate)
				{
					runtimeLinkedProp.Move (targetVector, moveMethod, inWorldSpace, _time, transformType, doEulerRotation, timeCurve, clearExisting);
				}
				else
				{
					runtimeLinkedProp.Move (targetVector, moveMethod, inWorldSpace, _time, transformType, false, timeCurve, clearExisting);
				}
			}
		}


		private Vector3 SetRelativeTarget (Vector3 _targetVector, bool isSkipping, Vector3 normalAddition)
		{
			if (isSkipping && nonSkipTargetVector != Vector3.zero)
			{
				_targetVector = nonSkipTargetVector;
			}
			else
			{
				_targetVector += normalAddition;
				nonSkipTargetVector = _targetVector;
			}
			return _targetVector;
		}
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Move Player?", isPlayer);
			if (!isPlayer)
			{
				parameterID = Action.ChooseParameterGUI ("Moveable object:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					linkedProp = null;
				}
				else
				{
					linkedProp = (Moveable) EditorGUILayout.ObjectField ("Moveable object:", linkedProp, typeof (Moveable), true);

					constantID = FieldToID <Moveable> (linkedProp, constantID);
					linkedProp = IDToField <Moveable> (linkedProp, constantID, false);
				}
			}

			EditorGUILayout.BeginHorizontal ();
			transformType = (TransformType) EditorGUILayout.EnumPopup (transformType);
			if (transformType != TransformType.CopyMarker)
			{
				toBy = (ToBy) EditorGUILayout.EnumPopup (toBy);
			}
			EditorGUILayout.EndHorizontal ();
			
			if (transformType == TransformType.CopyMarker)
			{
				markerParameterID = Action.ChooseParameterGUI ("Marker:", parameters, markerParameterID, ParameterType.GameObject);
				if (markerParameterID >= 0)
				{
					markerID = 0;
					marker = null;
				}
				else
				{
					marker = (Marker) EditorGUILayout.ObjectField ("Marker:", marker, typeof (Marker), true);

					markerID = FieldToID <Marker> (marker, markerID);
					marker = IDToField <Marker> (marker, markerID, false);
				}
			}
			else
			{
				setVectorMethod = (SetVectorMethod) EditorGUILayout.EnumPopup ("Vector is: ", setVectorMethod);
				if (setVectorMethod == SetVectorMethod.EnteredHere)
				{
					newVectorParameterID = Action.ChooseParameterGUI ("Value:", parameters, newVectorParameterID, ParameterType.Vector3);
					if (newVectorParameterID < 0)
					{
						newVector = EditorGUILayout.Vector3Field ("Value:", newVector);
					}
				}
				else if (setVectorMethod == SetVectorMethod.FromVector3Variable)
				{
					variableLocation = (VariableLocation) EditorGUILayout.EnumPopup ("Source:", variableLocation);

					switch (variableLocation)
					{
						case VariableLocation.Global:
							vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.GlobalVariable);
							if (vectorVarParameterID < 0)
							{
								vectorVarID = AdvGame.GlobalVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3);
							}
							break;

						case VariableLocation.Local:
							if (!isAssetFile)
							{
								vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.LocalVariable);
								if (vectorVarParameterID < 0)
								{
									vectorVarID = AdvGame.LocalVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3);
								}
							}
							else
							{
								EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
							}
							break;

						case VariableLocation.Component:
							vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.ComponentVariable);
							if (vectorVarParameterID >= 0)
							{
								variables = null;
								variablesConstantID = 0;	
							}
							else
							{
								variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
								variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
								variables = IDToField <Variables> (variables, variablesConstantID, false);
								
								if (variables != null)
								{
									vectorVarID = AdvGame.ComponentVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3, variables);
								}
							}
							break;
					}
				}

				clearExisting = EditorGUILayout.Toggle ("Stop existing transforms?", clearExisting);
			}

			if (transformType == TransformType.CopyMarker ||
				(transformType == TransformType.Translate && toBy == ToBy.To) ||
				(transformType == TransformType.Rotate && toBy == ToBy.To))
			{
				inWorldSpace = EditorGUILayout.Toggle ("Act in world-space?", inWorldSpace);

				if (inWorldSpace && transformType == TransformType.CopyMarker)
				{
					EditorGUILayout.HelpBox ("The moveable object's scale will be changed in local space.", MessageType.Info);
				}
			}

			transitionTimeParameterID = Action.ChooseParameterGUI ("Transition time (s):", parameters, transitionTimeParameterID, ParameterType.Float);
			if (transitionTimeParameterID < 0)
			{
				transitionTime = EditorGUILayout.FloatField ("Transition time (s):", transitionTime);
			}
			
			if (transitionTime > 0f)
			{
				if (transformType == TransformType.Rotate)
				{
					doEulerRotation = EditorGUILayout.Toggle ("Euler rotation?", doEulerRotation);
				}
				moveMethod = (MoveMethod) EditorGUILayout.EnumPopup ("Move method:", moveMethod);
				if (moveMethod == MoveMethod.CustomCurve)
				{
					timeCurve = EditorGUILayout.CurveField ("Time curve:", timeCurve);
				}
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}
			
			AfterRunningOption ();
		}


		override public void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberMoveable> (linkedProp);
			}
			AssignConstantID <Moveable> (linkedProp, constantID, parameterID);
			AssignConstantID <Marker> (marker, markerID, markerParameterID);

			if (transformType != TransformType.CopyMarker &&
				setVectorMethod == SetVectorMethod.FromVector3Variable &&
				variableLocation == VariableLocation.Component)
			{
				AssignConstantID <Variables> (variables, variablesConstantID, vectorVarParameterID);
			}
		}


		override public string SetLabel ()
		{
			if (linkedProp != null)
			{
				return linkedProp.name;
			}
			return string.Empty;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Transform' Action</summary>
		 * <param name = "objectToMove">The Moveable object to move</param>
		 * <param name = "markerToMoveTo">The Marker to move towards</param>
		 * <param name = "inWorldSpace">If True, the Marker's transform values will be read in world space</param>
		 * <param name = "transitionTime">The time, in seconds, to take when moving to the Marker</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTransform CreateNew (Moveable objectToMove, Marker markerToMoveTo, bool inWorldSpace = true, float transitionTime = 1f, MoveMethod moveMethod = MoveMethod.Smooth, AnimationCurve timeCurve = null, bool waitUntilFinish = false)
		{
			ActionTransform newAction = (ActionTransform) CreateInstance <ActionTransform>();
			newAction.linkedProp = objectToMove;
			newAction.transformType = TransformType.CopyMarker;
			newAction.inWorldSpace = inWorldSpace;
			newAction.transitionTime = transitionTime;
			newAction.moveMethod = moveMethod;
			newAction.timeCurve = timeCurve;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}
		
	}
	
}