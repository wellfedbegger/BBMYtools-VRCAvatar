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
        public class Searcher
        {

            public static VRCAvatarDescriptor avatar;
            public static bool is_physbone_mode = false;

            // 折り畳み表示の制御用bool[]
            static List<List<bool>> show_AnimatorData = new List<List<bool>>(
                Enumerable.Range(0, Const.pbSuffixs.Count())
                .Select(_ =>
                    new List<bool>(Enumerable.Repeat(false, Const.N_ANIMLAYER)))
                );
            static List<bool> show_ExData = new List<bool>(Enumerable.Repeat(false, Const.pbSuffixs.Count()));
            static List<bool> show_ctRData = new List<bool>(Enumerable.Repeat(false, Const.pbSuffixs.Count()));
            static bool show_PBData = false;

            // 背景色一覧
            static readonly Color _defaultGUIColor = GUI.backgroundColor;
            static readonly List<Color> _GUIColors = new List<Color>(
                    Enumerable.Range(0, Const.N_ANIMLAYER)
                    .Select(i => Color.HSVToRGB((float)i / (float)Const.N_ANIMLAYER, 1f, 1f)));

            static List<bool> show_paramdats = new List<bool>(Enumerable.Repeat(true, Const.pbSuffixs.Count()));

            static List<VRCPhysBone> _physbones = new List<VRCPhysBone> { };

            static List<ParameterData> _paramdats = new List<ParameterData>(
                Enumerable.Range(0, Const.pbSuffixs.Count())
                .Select(_ =>
                        new ParameterData
                        {
                            animatorParameterDatas = new List<AnimatorParameterData> { },
                            exparams = new List<ExParam> { },
                            exmenuParamrefs = new List<ExMenuParamRefs> { },
                            contactReceivers = new List<VRCContactReceiver> { },
                            parameter_name = "",
                            hasAnimatorData = false,
                        })
                        );

            static ParameterData Flush(ParameterData _paramdat)
            {
                _paramdat.animatorParameterDatas.Clear();
                _paramdat.exparams.Clear();
                _paramdat.exmenuParamrefs.Clear();
                _paramdat.contactReceivers.Clear();
                _paramdat.parameter_name = "";
                _paramdat.hasAnimatorData = false;
                return _paramdat;
            }

            public static void Search(string parameterName)
            {
                if (!is_physbone_mode)
                {
                    Flush(_paramdats[0]);
                    Get($"{parameterName}{Const.pbSuffixs[0]}", 0);
                }
                else if (is_physbone_mode)
                {
                    foreach (int i in Enumerable.Range(0, Const.pbSuffixs.Count()))
                    {
                        Flush(_paramdats[i]);
                        Get($"{parameterName}{Const.pbSuffixs[i]}", i);
                    }
                    _physbones.Clear();
                    GetPB(parameterName);
                }
            }
            static void Get(string parameterName, int idx)
            {
                if (!string.IsNullOrEmpty(parameterName))
                {
                    //for each Animator
                    foreach (var animlayers in new VRCAvatarDescriptor.CustomAnimLayer[][] { avatar.baseAnimationLayers, avatar.specialAnimationLayers })
                    {
                        foreach (var animlayer in animlayers
                        .Where(animlayer => (animlayer.animatorController != null && !animlayer.isDefault)))
                        {
                            _paramdats[idx].animatorParameterDatas.Add(
                                    CheckAnimaor(animlayer, parameterName));
                            // Debug.Log($"{CheckAnimaor(animlayer, parameterName).animatorParameterIdx}");
                        }
                    }
                    //Expression Parameters
                    if (avatar.expressionParameters != null)
                    {
                        if (avatar.expressionParameters.FindParameter(parameterName) != null)
                        {
                            _paramdats[idx].exparams.Add(
                                new ExParam(
                                avatar.expressionParameters,
                                avatar.expressionParameters.FindParameter(parameterName)
                            ));
                        }
                    }
                    //Expression Menu Parameter References
                    if (avatar.expressionsMenu != null)
                    {
                        CheckExMenu(avatar.expressionsMenu, parameterName, _paramdats[idx].exmenuParamrefs);
                    }
                    _paramdats[idx].contactReceivers.AddRange(avatar.GetComponentsInChildren<VRCContactReceiver>(includeInactive: true).OrEmptyIfNull()
                                                    .Where(ctR => ctR.parameter == parameterName).ToList());
                    // Debug.Log($"{_paramdats[idx].contactReceivers.Count()}");

                    var tmp = _paramdats[idx];
                    tmp.parameter_name = parameterName;
                    tmp.hasAnimatorData = _paramdats[idx].HasData();
                    _paramdats[idx] = tmp;
                }
            }
            /// <summary>
            /// VRC PhysBone のParameter欄を検索
            /// </summary>
            static void GetPB(string parameterName)
            {
                if (!string.IsNullOrEmpty(parameterName))
                {
                    _physbones.AddRange(avatar.GetComponentsInChildren<VRCPhysBone>(includeInactive: true).OrEmptyIfNull()
                                                    .Where(pb => pb.parameter == parameterName).ToList());
                }
            }

            static AnimatorParameterData CheckAnimaor(VRCAvatarDescriptor.CustomAnimLayer customAnimLayer, string parameterName)
            {
                var dat = new AnimatorParameterData(
                    (AnimatorController)customAnimLayer.animatorController,
                    customAnimLayer.type, parameterName);
                foreach (var layer in ((AnimatorController)customAnimLayer.animatorController).layers)
                {
                    var layerdat = new LayerParameterData(layer);
                    CheckStates(layer.stateMachine, layerdat, parameterName);
                    dat.layerParameterDatas.Add(layerdat);
                }
                return dat;
            }

            static void CheckStates(AnimatorStateMachine stateMachine, LayerParameterData _lydat, string parameterName)
            {
                // Debug.LogWarning($"{stateMachine.name}");
                _lydat.transitions.AddRange(CheckTransition(
                    from_state_name: "Entry",
                    parameterName: parameterName,
                    transitions: stateMachine.entryTransitions,
                    selectMachineInstead: stateMachine)
                    );
                _lydat.transitions.AddRange(CheckTransition(
                    from_state_name: "AnyState",
                    parameterName: parameterName,
                    statetransitions: stateMachine.anyStateTransitions,
                    selectMachineInstead: stateMachine)
                    );
                foreach (ChildAnimatorState state in stateMachine.states)
                {
                    _lydat.transitions.AddRange(CheckTransition(
                        from_state_name: state.state.name,
                        parameterName: parameterName,
                        statetransitions: state.state.transitions,
                        fromState: state.state)
                        );
                    if (state.state.motion is BlendTree)
                    {
                        BlendTree blendTree = state.state.motion as BlendTree;

                        if (blendTree.blendParameter == parameterName)
                            _lydat.blendTrees.Add(blendTree);
                        if (blendTree.blendParameterY == parameterName)
                            _lydat.blendTreesY.Add(blendTree);
                    }
                    CheckVRCAvatarParameterDriver(parameterName, _lydat, state: state.state);
                }
                CheckVRCAvatarParameterDriver(parameterName, _lydat, statemachine: stateMachine);

                foreach (ChildAnimatorStateMachine chstateMachine in stateMachine.stateMachines)
                {
                    _lydat.transitions.AddRange(CheckTransition(
                        from_state_name: chstateMachine.stateMachine.name,
                        parameterName: parameterName,
                        transitions: stateMachine.GetStateMachineTransitions(chstateMachine.stateMachine),
                        selectMachineInstead: stateMachine
                        ));
                    // 再帰的にサブステートマシンも見る
                    CheckStates(chstateMachine.stateMachine, _lydat, parameterName);
                }
            }

            static void CheckVRCAvatarParameterDriver(
                string parameterName,
            LayerParameterData lydat,
            AnimatorState state = null,
            AnimatorStateMachine statemachine = null)
            {
                if (state != null)
                {
                    foreach (var b in state.behaviours
                    .Where(b => b.GetType() == typeof(VRCAvatarParameterDriver)))
                    {
                        foreach (var p in ((VRCAvatarParameterDriver)b).parameters
                        .Where(p => p.name == parameterName))
                        {
                            lydat.parameterdrivers.Add(
                            new VRCAvatarParameterDriverData(
                            state: state,
                            statemachine: statemachine,
                            parameter: p,
                            parameterdriver: (VRCAvatarParameterDriver)b
                            )
                        );
                        }
                    }
                }
                else if (statemachine != null)
                {
                    foreach (var b in statemachine.behaviours
                    .Where(b => b.GetType() == typeof(VRCAvatarParameterDriver)))
                    {
                        foreach (var p in ((VRCAvatarParameterDriver)b).parameters
                        .Where(p => p.name == parameterName))
                        {
                            lydat.parameterdrivers.Add(
                            new VRCAvatarParameterDriverData(
                            state: state,
                            statemachine: statemachine,
                            parameter: p,
                            parameterdriver: (VRCAvatarParameterDriver)b
                            )
                        );
                        }
                    }
                }
            }

            static List<Transition> CheckTransition(
            string from_state_name,
            string parameterName,

            AnimatorTransition[] transitions = null,
            AnimatorStateTransition[] statetransitions = null,

            AnimatorState fromState = null,
            AnimatorStateMachine selectMachineInstead = null
              )
            {
                var res = new List<Transition> { };
                foreach (AnimatorTransition transition in transitions.OrEmptyIfNull())
                {
                    string destination_state_name = "?";
                    if (transition.destinationState != null)
                        destination_state_name = $"[{transition.destinationState.name}]";
                    else if (transition.destinationStateMachine != null)
                        destination_state_name = $"<{transition.destinationStateMachine.name}>";
                    else if (transition.isExit)
                        destination_state_name = "Exit";

                    for (int i = 0; i < transition.conditions.Length; i++)
                    {
                        if (transition.conditions[i].parameter == parameterName)
                        {
                            res.Add(
                                new Transition(
                                    i,
                                    from_state_name: from_state_name,
                                    destination_state_name: destination_state_name,
                                    transition: transition,
                                    statetransition: null,
                                    fromState: fromState,
                                    selectMachineInstead: selectMachineInstead
                                    )
                                    );
                        }
                    }
                }
                foreach (AnimatorStateTransition transition in statetransitions.OrEmptyIfNull())
                {
                    string destination_state_name = "?";
                    if (transition.destinationState != null)
                        destination_state_name = $"[{transition.destinationState.name}]";
                    else if (transition.destinationStateMachine != null)
                        destination_state_name = $"<{transition.destinationStateMachine.name}>";
                    else if (transition.isExit)
                        destination_state_name = "Exit";

                    for (int i = 0; i < transition.conditions.Length; i++)
                    {
                        if (transition.conditions[i].parameter == parameterName)
                        {
                            res.Add(
                                new Transition(
                                    i,
                                    from_state_name: from_state_name,
                                    destination_state_name: destination_state_name,
                                    transition: null,
                                    statetransition: transition,
                                    fromState: fromState,
                                    selectMachineInstead: selectMachineInstead
                                    )
                                    );
                        }
                    }
                }
                return res;
            }

            static void CheckExMenu(VRCExpressionsMenu menu, string name, List<ExMenuParamRefs> exmenu_paramrefs)
            {
                foreach (var control in menu.controls.OrEmptyIfNull())
                {
                    var prms = new List<VRCExpressionsMenu.Control.Parameter> { };
                    if (!VRCExpressionsMenu.Control.Parameter.IsNull(control.parameter))
                    {
                        if (control.parameter.name == name)
                        {
                            prms.Add(control.parameter);
                        }
                    }
                    foreach (var p in control.subParameters.OrEmptyIfNull())
                    {
                        if ((!VRCExpressionsMenu.Control.Parameter.IsNull(p)) && p.name == name)
                            prms.Add(p);
                    }
                    if (prms.Count() > 0)
                    {
                        exmenu_paramrefs.Add(
                            new ExMenuParamRefs(
                                menu: menu,
                                control: control,
                                parameters: prms
                            )
                        );
                    }
                    if (control.subMenu != null)
                    {
                        // 再帰的にサブメニューも見る
                        CheckExMenu(control.subMenu, name, exmenu_paramrefs);
                    }
                }
            }
            public static void Show()
            {
                if (is_physbone_mode)
                {
                    PrintPB();
                    foreach (int i in Enumerable.Range(0, Const.pbSuffixs.Count()))
                    {
                        Print(i);
                        // Debug.Log($"{_paramdats[i].animatorParameterDatas.Count()}");
                    }
                }

                else if (!is_physbone_mode)
                {
                    Print(0);
                }
            }
            public static void Print(int idx)
            {
                if (!_paramdats[idx].hasAnimatorData)
                    // if (!_paramdats[idx].HasData())
                    return;

                using (new EditorGUILayout.VerticalScope("Box", GUILayout.ExpandWidth(true)))
                {
                    // Debug.Log($"{_paramdats[idx].hasAnimatorData}\tsufix:{Const.pbSuffixs[idx]}");
                    // show_paramdats[idx] = EditorGUILayout.Foldout(show_paramdats[idx], $"{parameter_name}{Const.pbSuffixs[idx]}");
                    show_paramdats[idx] = EditorGUILayout.Foldout(show_paramdats[idx], $"{_paramdats[idx].parameter_name}{Const.pbSuffixs[idx]}");

                    if (show_paramdats[idx])
                    {
                        var apd = _paramdats[idx].animatorParameterDatas;
                        show_AnimatorData[idx].Extend(apd.Count());

                        foreach (int i in Enumerable.Range(0, apd.Count()))
                        {
                            // Debug.Log($"{(int)(apd[i].type) % Const.N_ANIMLAYER}");
                            var layerWithData = apd[i].layerParameterDatas.OrEmptyIfNull()
                            .Where(
                                ly =>
                                    ly.transitions.Count() > 0 ||
                                    ly.blendTrees.Count() > 0 ||
                                    ly.blendTreesY.Count() > 0 ||
                                    ly.parameterdrivers.Count() > 0
                                );
                            if ((layerWithData.Count() > 0 || apd[i].animatorParameterIdx != -1) == false)
                                continue;

                            GUI.backgroundColor = _GUIColors[(int)(apd[i].layerType) % Const.N_ANIMLAYER];
                            // Debug.Log($"{layerWithData.Count()}  {apd[i].animator.parameters[apd[i].animatorParameterIdx].name}{apd[i].animatorParameterIdx}");

                            using (new EditorGUILayout.VerticalScope("Box", GUILayout.ExpandWidth(true)))
                            {
                                // Debug.Log($"{Const.pbSuffixs[idx]} {show_AnimatorData[idx][i]}");
                                show_AnimatorData[idx][i] = EditorGUILayout.BeginFoldoutHeaderGroup(
                                    show_AnimatorData[idx][i],
                                        $"{apd[i].layerType}\t\t{apd[i].animator.name}");
                                if (show_AnimatorData[idx][i])
                                {
                                    if (apd[i].animatorParameterIdx != -1)
                                    {
                                        //FIXME: parameterを削除するとここでIndexOutOfRangeException
                                        var parameter = apd[i].animator.parameters[apd[i].animatorParameterIdx];
                                        string defaultValue = "";
                                        switch (parameter.type)
                                        {
                                            case AnimatorControllerParameterType.Bool:
                                                defaultValue = parameter.defaultBool.ToString();
                                                break;
                                            case AnimatorControllerParameterType.Trigger:
                                                defaultValue = parameter.defaultBool.ToString();
                                                break;
                                            case AnimatorControllerParameterType.Float:
                                                defaultValue = parameter.defaultFloat.ToString();
                                                break;
                                            case AnimatorControllerParameterType.Int:
                                                defaultValue = parameter.defaultInt.ToString();
                                                break;
                                            default:
                                                break;
                                        }

                                        GUI.backgroundColor = Color.black;
                                        using (new EditorGUILayout.VerticalScope("Box", GUILayout.ExpandWidth(true)))
                                        {
                                            GUI.backgroundColor = _defaultGUIColor;
                                            Util.LabeltoSelectObject($"{parameter.type}\t[default: {defaultValue}]", apd[i].animator,
                                            EditorStyles.boldLabel);
                                        }
                                    }
                                    foreach (var ly in apd[i].layerParameterDatas.OrEmptyIfNull())
                                    {
                                        if (
                                            ly.transitions.Count() > 0 ||
                                            // ly.statetransitions.Count() > 0 ||
                                            ly.blendTrees.Count() > 0 ||
                                            ly.blendTreesY.Count() > 0 ||
                                            ly.parameterdrivers.Count() > 0
                                                )
                                        {
                                            EditorGUILayout.LabelField($"{apd[i].animator.name} / {ly.layer.name}");
                                        }
                                        foreach (var tr in ly.transitions)//TODO: クリックするとAnimatorウィンドウ上でフォーカスする様にしたい
                                        {
                                            if (tr.transition != null || tr.stateTransition != null)
                                            {
                                                if (tr.fromState != null)
                                                {
                                                    Util.LabeltoSelectObject($"\t[{tr.fromStateName}] → {tr.destinationStateName}", tr.fromState);
                                                    // Util.LabeltoSelectObject("FROM", tr.fromState);
                                                    // Util.LabeltoSelectObject("TRANS", tr.transition);
                                                    // EditorGUILayout.ObjectField(tr.destinationStateName, typeof(AnimatorTransition), true);
                                                    // EditorGUILayout.LabelField($"\t[{tr.fromStateName}] → {tr.destinationStateName}");
                                                }
                                                else
                                                {
                                                    Util.LabeltoSelectObject($"\t<{tr.fromStateName}> → {tr.destinationStateName}", tr.selectMachineInstead);
                                                    // EditorGUILayout.ObjectField(tr.selectMachineInstead, typeof(AnimatorStateMachine), true);
                                                    // EditorGUILayout.LabelField($"\t<{tr.fromStateName}> → {tr.destinationStateName}");
                                                }
                                            }
                                            // if (tr.transition != null || tr.statetransition != null)
                                            // {
                                            // EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                                            // if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                            // {
                                            //     // Selection.activeObject = apd[i].animator;
                                            //     if (tr.fromState != null)
                                            //         Selection.activeObject = tr.fromState;
                                            //     else if (tr.selectMachineInstead != null)
                                            //         Selection.activeObject = tr.selectMachineInstead;
                                            // }
                                            // }
                                        }
                                        foreach (var bt in ly.blendTrees)
                                            Util.LabeltoSelectObject($"\t(BlendTree)\t{bt.name}", bt);
                                        foreach (var bt in ly.blendTreesY)
                                            Util.LabeltoSelectObject($"\t(BlendTree Y)\t{bt.name}", bt);
                                        foreach (var dr in ly.parameterdrivers)
                                        {
                                            if (dr.state != null)
                                                EditorGUILayout.LabelField($"\t(VRC AvatarParameterDriver) [{dr.state.name}] ");
                                            else if (dr.statemachine != null)
                                                EditorGUILayout.LabelField($"\t(VRC AvatarParameterDriver) <{dr.statemachine.name}> ");
                                        }
                                    }
                                }
                            }
                            EditorGUILayout.EndFoldoutHeaderGroup();
                            GUI.backgroundColor = _defaultGUIColor;

                        }

                        if (_paramdats[idx].exparams.Count() > 0 || _paramdats[idx].exmenuParamrefs.Count() > 0)
                        {
                            GUI.backgroundColor = Color.gray;
                            using (new EditorGUILayout.VerticalScope("Box", GUILayout.ExpandWidth(true)))
                            {
                                show_ExData[idx] = EditorGUILayout.BeginFoldoutHeaderGroup(show_ExData[idx], "Expressions");
                                if (show_ExData[idx])
                                {
                                    foreach (var exparam in _paramdats[idx].exparams)
                                    {
                                        Util.LabeltoSelectObject($"EXParameter: {exparam.parameter.valueType.ToString()}\tdefault: {exparam.parameter.defaultValue}\tSaved: {exparam.parameter.saved}\tSynced: {exparam.parameter.networkSynced}",
                                        exparam.exparam);
                                    }
                                    foreach (var exmenuCtrl in _paramdats[idx].exmenuParamrefs)
                                    {
                                        Util.LabeltoSelectObject($"{exmenuCtrl.menu.name} /\t{exmenuCtrl.control.name}", exmenuCtrl.menu);
                                    }
                                }
                            }
                            EditorGUILayout.EndFoldoutHeaderGroup();
                            GUI.backgroundColor = _defaultGUIColor;
                        }

                        if (_paramdats[idx].contactReceivers.Count() > 0)
                        {
                            GUI.backgroundColor = Color.gray;
                            using (new EditorGUILayout.VerticalScope("Box", GUILayout.ExpandWidth(true)))
                            {
                                show_ctRData[idx] = EditorGUILayout.BeginFoldoutHeaderGroup(show_ctRData[idx], "Contact Receivers");
                                if (show_ctRData[idx])
                                {
                                    foreach (var ctR in _paramdats[idx].contactReceivers)
                                    {
                                        Util.LabeltoSelectObject($"{ctR.name}", ctR);
                                    }
                                }

                            }
                            EditorGUILayout.EndFoldoutHeaderGroup();
                            GUI.backgroundColor = _defaultGUIColor;
                        }
                    }
                }

            }
            public static void PrintPB()
            {

                if (_physbones.Count() > 0)
                {
                    GUI.backgroundColor = Color.black;
                    using (new EditorGUILayout.VerticalScope("Box", GUILayout.ExpandWidth(true)))
                    {
                        show_PBData = EditorGUILayout.BeginFoldoutHeaderGroup(show_PBData, "PhysBones");
                        if (show_PBData)
                        {
                            foreach (var pb in _physbones)
                            {
                                Util.LabeltoSelectObject($"{pb.name}", pb);
                            }
                        }
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    GUI.backgroundColor = _defaultGUIColor;
                    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(3));
                }

            }

            public static void Rename(string parameterName, string newParameterName)
            {
                Search(parameterName);

                if (is_physbone_mode)
                {
                    foreach (int i in Enumerable.Range(0, Const.pbSuffixs.Count()))
                    {
                        DoRename($"{newParameterName}{Const.pbSuffixs[i]}", i);
                    }
                    DoRenamePB(newParameterName);
                }

                else if (!is_physbone_mode)
                {
                    DoRename($"{newParameterName}{Const.pbSuffixs[0]}", 0);
                }
                Search(parameterName);
            }

            static void DoRename(string newParameterName, int idx)
            {
                // foreach (var apd in _paramdats[idx].animatorParameterDatas)
                var apds = _paramdats[idx].animatorParameterDatas.GroupBy(apd => apd.animator).Select(g => g.First());
                foreach (var apd in apds)
                {
                    // Debug.Log($"{apd.animator.name}");
                    // Animator
                    Undo.RecordObject(apd.animator, "Rename Parameters");
                    if (apd.animatorParameterIdx != -1)
                    {
                        var parameters = apd.animator.parameters;
                        parameters[apd.animatorParameterIdx].name = newParameterName;
                        apd.animator.parameters = parameters;
                    }

                    // Transition condition
                    foreach (var ly in apd.layerParameterDatas.OrEmptyIfNull())
                    {
                        foreach (var tr in ly.transitions)
                        {
                            if (tr.transition != null)
                            {
                                Undo.RecordObject(tr.transition, "Rename Parameters");
                                var c = tr.transition.conditions;
                                c[tr.index].parameter = newParameterName;
                                tr.transition.conditions = c;
                            }
                            if (tr.stateTransition != null)
                            {
                                Undo.RecordObject(tr.stateTransition, "Rename Parameters");
                                var c = tr.stateTransition.conditions;
                                c[tr.index].parameter = newParameterName;
                                tr.stateTransition.conditions = c;
                            }
                        }

                        // BlendTree
                        foreach (var bt in ly.blendTrees)
                        {
                            Undo.RecordObject(bt, "Rename Parameters");
                            bt.blendParameter = newParameterName;
                        }
                        foreach (var bt in ly.blendTreesY)
                        {
                            Undo.RecordObject(bt, "Rename Parameters");
                            bt.blendParameterY = newParameterName;
                        }

                        // VRCAvatarParameterDriver
                        foreach (var dr in ly.parameterdrivers)
                        {
                            Undo.RecordObject(dr.parameterDriver, "Rename Parameters");
                            dr.parameter.name = newParameterName;
                        }
                    }
                }
                // VRCExpressionParameters
                foreach (var exparam in _paramdats[idx].exparams)
                {
                    Undo.RecordObject(exparam.exparam, "Rename Parameters");
                    exparam.parameter.name = newParameterName;
                }

                // VRCExpressionsMenu
                foreach (var exmenuCtrl in _paramdats[idx].exmenuParamrefs)
                {
                    Undo.RecordObject(exmenuCtrl.menu, "Rename Parameters");
                    foreach (var parameter in exmenuCtrl.parameters)
                        parameter.name = newParameterName;
                }

                foreach (var ctR in _paramdats[idx].contactReceivers)
                {
                    Undo.RecordObject(ctR, "Rename Parameters");
                    ctR.parameter = newParameterName;
                }
            }

            static void DoRenamePB(string newParameterName)
            {
                foreach (var pb in _physbones)
                {
                    Undo.RecordObject(pb, "Rename Parameters");
                    pb.parameter = newParameterName;
                }
            }
        }
    }
}