using UnityEditor.Animations;
using System.Linq;
using System.Collections.Generic;

using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Contact.Components;

namespace bbmy
{
	namespace AvatarParameterViewer
	{

		public static class Const
		{
			public static readonly string[] pbSuffixs = { "", "_IsGrabbed", "_IsPosed", "_Angle", "_Stretch", "_Squish" };
			public static readonly int N_ANIMLAYER = 9;
		}

		public struct ParameterData
		{
			public List<AnimatorParameterData> animatorParameterDatas;
			public List<ExParam> exparams;
			public List<ExMenuParamRefs> exmenuParamrefs;
			public List<VRCContactReceiver> contactReceivers;
			public bool hasAnimatorData;
			public string parameter_name;
			public bool HasData()
			{
				var layerWithData = this.animatorParameterDatas
						.SelectMany(a =>
						a.layerParameterDatas.OrEmptyIfNull())
						.Any(
								ly =>
										ly.transitions.Any() ||
										ly.blendTrees.Any() ||
										ly.blendTreesY.Any() ||
										ly.parameterdrivers.Any()
						);
				var animatorHasParameter = this.animatorParameterDatas
							.Any(a => a.animatorParameterIdx != -1);
				var hasExParam = this.exparams.Any();
				var hasExMenuRef = this.exmenuParamrefs.Any();
				var hasContactReceiver = this.contactReceivers.Any();

				return (layerWithData ||
									animatorHasParameter ||
									hasExParam ||
									hasExMenuRef ||
									hasContactReceiver);
			}
		}

		public struct ExParam
		{
			public readonly VRCExpressionParameters exparam;
			public readonly VRCExpressionParameters.Parameter parameter;
			public ExParam(
				VRCExpressionParameters exparam,
				VRCExpressionParameters.Parameter parameter)
			{
				this.exparam = exparam;
				this.parameter = parameter;
			}
		}


		public struct AnimatorParameterData
		{
			public readonly AnimatorController animator;
			/// <summary>
			/// Parameterで定義されていなければ-1 stateで使用されている可能性は無いと思う、ここが-1ならlayerParameterDatasも無いはず
			/// </summary>
			public readonly int animatorParameterIdx;
			public readonly VRCAvatarDescriptor.AnimLayerType layerType;
			public readonly List<LayerParameterData> layerParameterDatas;

			// コンストラクタ
			public AnimatorParameterData(
				AnimatorController animator,
				VRCAvatarDescriptor.AnimLayerType layer_type,
				string _parameterName
				 )
			{
				this.animator = animator;
				this.layerType = layer_type;
				this.animatorParameterIdx = Util.FindAnimatorControllerParameter(animator, _parameterName);
				this.layerParameterDatas = new List<LayerParameterData> { };
			}
		}


		public struct LayerParameterData
		{
			public readonly AnimatorControllerLayer layer;

			public readonly List<Transition> transitions;
			public readonly List<BlendTree> blendTrees;
			public readonly List<BlendTree> blendTreesY;
			public readonly List<VRCAvatarParameterDriverData> parameterdrivers;

			public LayerParameterData(AnimatorControllerLayer layer)
			{
				this.layer = layer;
				transitions = new List<Transition> { };
				blendTrees = new List<BlendTree> { };
				blendTreesY = new List<BlendTree> { };
				parameterdrivers = new List<VRCAvatarParameterDriverData> { };
			}
		}
		public struct ExMenuParamRefs
		{
			public readonly VRCExpressionsMenu.Control control;
			public readonly List<VRCExpressionsMenu.Control.Parameter> parameters;
			public readonly VRCExpressionsMenu menu;

			public ExMenuParamRefs(
			VRCExpressionsMenu.Control control,
			List<VRCExpressionsMenu.Control.Parameter> parameters,
			VRCExpressionsMenu menu
			)
			{
				this.control = control;
				this.parameters = parameters;
				this.menu = menu;
			}
		}

		public struct Transition
		{
			public readonly string fromStateName;
			public readonly string destinationStateName;
			public readonly AnimatorTransition transition;
			public readonly AnimatorStateTransition stateTransition;
			public readonly AnimatorState fromState;
			public readonly AnimatorStateMachine selectMachineInstead;
			public readonly int index;

			public Transition(
				int index,
				string from_state_name,
				string destination_state_name,
				AnimatorTransition transition,
				AnimatorStateTransition statetransition,
				AnimatorState fromState,
				AnimatorStateMachine selectMachineInstead
			)
			{
				this.index = index;
				this.fromStateName = from_state_name;
				this.destinationStateName = destination_state_name;
				this.transition = transition;
				this.stateTransition = statetransition;
				this.fromState = fromState;
				this.selectMachineInstead = selectMachineInstead;
			}
		}

		public struct VRCAvatarParameterDriverData
		{
			public readonly AnimatorState state;
			public readonly AnimatorStateMachine statemachine;
			public readonly VRC.SDKBase.VRC_AvatarParameterDriver.Parameter parameter;
			public readonly VRCAvatarParameterDriver parameterDriver;

			public VRCAvatarParameterDriverData(
				AnimatorState state,
				AnimatorStateMachine statemachine,
				VRC.SDKBase.VRC_AvatarParameterDriver.Parameter parameter,
				VRCAvatarParameterDriver parameterdriver
			)
			{
				this.state = state;
				this.statemachine = statemachine;
				this.parameter = parameter;
				this.parameterDriver = parameterdriver;
			}
		}

	}
}
