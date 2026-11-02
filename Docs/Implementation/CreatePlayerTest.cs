
var source = "Assets/GameResource/Animation/Animator/PlayerAnimator.controller";
var path = "Assets/GameResource/Animation/Animator/PlayerTest.controller";
if (AssetDatabase.LoadMainAssetAtPath(path) != null)
    throw new Exception("PlayerTest already exists; refusing to overwrite.");
var clipRoot = "Assets/GameResource/External/Animation/Player/";
var hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipRoot + "Dwarf Hit.anim");
var downClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipRoot + "Sword Death 1.anim");
var deadClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipRoot + "Sword Death 2.anim");
if (hitClip == null || downClip == null || deadClip == null)
    throw new Exception("Required animation clip missing.");
if (!AssetDatabase.CopyAsset(source, path)) throw new Exception("Controller copy failed.");
var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
controller.name = "PlayerTest";
controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
controller.AddParameter("LifeState", AnimatorControllerParameterType.Int);
var sm = controller.layers[0].stateMachine;
var locomotion = sm.states.Single(s => s.state.name == "Locomotion").state;
var strafe = sm.states.Single(s => s.state.name == "Strafe").state;
var hit = sm.AddState("Hit", new Vector3(300, 240, 0));
var down = sm.AddState("Down", new Vector3(560, 140, 0));
var dead = sm.AddState("Dead", new Vector3(560, 280, 0));
hit.motion = hitClip;
down.motion = downClip;
dead.motion = deadClip;
hit.writeDefaultValues = locomotion.writeDefaultValues;
down.writeDefaultValues = locomotion.writeDefaultValues;
dead.writeDefaultValues = locomotion.writeDefaultValues;
var enterHit = sm.AddAnyStateTransition(hit);
enterHit.hasExitTime = false;
enterHit.hasFixedDuration = true;
enterHit.duration = 0.08f;
enterHit.canTransitionToSelf = false;
enterHit.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Hit");
enterHit.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1, "LifeState");
var leaveHit = hit.AddTransition(locomotion);
leaveHit.hasExitTime = true;
leaveHit.exitTime = 0.9f;
leaveHit.hasFixedDuration = true;
leaveHit.duration = 0.08f;
leaveHit.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1, "LifeState");
foreach (var state in new[] { locomotion, strafe })
    foreach (var transition in state.transitions)
        if (transition.destinationState == locomotion || transition.destinationState == strafe)
            transition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1, "LifeState");
EditorUtility.SetDirty(controller);
AssetDatabase.SaveAssetIfDirty(controller);
AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
var saved = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
if (saved.layers[0].stateMachine.states.Length != 5 || saved.parameters.Length != 7)
    throw new Exception("Unexpected saved controller structure.");
Selection.activeObject = saved;
EditorGUIUtility.PingObject(saved);
return new { path, parameters = saved.parameters.Select(p => p.name + ":" + p.type).ToArray(), layers = saved.layers.Select(l => new { l.name, states = l.stateMachine.states.Select(s => new { s.state.name, motion = s.state.motion == null ? null : s.state.motion.name, transitions = s.state.transitions.Length }).ToArray() }).ToArray(), downClipTemporary = true };

