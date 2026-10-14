using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DdaIT.Util.Editor
{
    internal static class AnimationClipCurveUtil
    {
        private const float TimeEpsilon = 0.0005f;

        public static List<string> CollectTransformPaths(EditorCurveBinding[] bindings)
        {
            var paths = new List<string>();
            var seen = new HashSet<string>();
            if (bindings == null)
                return paths;

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.type != typeof(Transform))
                    continue;
                if (!seen.Add(binding.path))
                    continue;
                paths.Add(binding.path);
            }

            paths.Sort(string.CompareOrdinal);
            return paths;
        }

        public static List<string> CollectHierarchyPaths(Transform root)
        {
            var paths = new List<string>();
            if (root == null)
                return paths;

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
                paths.Add(AnimationUtility.CalculateTransformPath(transforms[i], root));

            paths.Sort(string.CompareOrdinal);
            return paths;
        }

        public static bool HasKeyAt(AnimationClip clip, string path, float time)
        {
            if (clip == null)
                return false;

            var bindings = AnimationUtility.GetCurveBindings(clip);
            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.type != typeof(Transform) || binding.path != path)
                    continue;

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve != null && FindKeyIndex(curve, time) >= 0)
                    return true;
            }

            return false;
        }

        public static void CollectKeyTimes(AnimationClip clip, string path, List<float> times)
        {
            times.Clear();
            if (clip == null)
                return;

            var seen = new HashSet<float>();
            var bindings = AnimationUtility.GetCurveBindings(clip);
            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.type != typeof(Transform) || binding.path != path)
                    continue;

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                    continue;

                var keys = curve.keys;
                for (var k = 0; k < keys.Length; k++)
                {
                    var t = keys[k].time;
                    if (seen.Add(t))
                        times.Add(t);
                }
            }

            times.Sort();
        }

        public static float? FindNeighborKeyTime(AnimationClip clip, string path, float time, bool previous)
        {
            var times = new List<float>();
            CollectKeyTimes(clip, path, times);
            if (previous)
            {
                for (var i = times.Count - 1; i >= 0; i--)
                {
                    if (times[i] < time - TimeEpsilon)
                        return times[i];
                }
            }
            else
            {
                for (var i = 0; i < times.Count; i++)
                {
                    if (times[i] > time + TimeEpsilon)
                        return times[i];
                }
            }

            return null;
        }

        public static bool PathHasChannel(EditorCurveBinding[] bindings, string path, TransformChannel channel)
        {
            if (bindings == null)
                return false;

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.type != typeof(Transform) || binding.path != path)
                    continue;
                if (MatchesChannel(binding.propertyName, channel))
                    return true;
            }

            return false;
        }

        public static void SetLocalPose(
            AnimationClip clip,
            string path,
            float time,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            bool writePosition,
            bool writeRotation,
            bool writeScale)
        {
            if (clip == null)
                return;

            var bindings = new List<EditorCurveBinding>();
            var curves = new List<AnimationCurve>();

            if (writePosition)
            {
                UpsertComponent(clip, path, "m_LocalPosition.x", time, localPosition.x, bindings, curves);
                UpsertComponent(clip, path, "m_LocalPosition.y", time, localPosition.y, bindings, curves);
                UpsertComponent(clip, path, "m_LocalPosition.z", time, localPosition.z, bindings, curves);
            }

            if (writeRotation)
            {
                if (UsesEuler(clip, path))
                {
                    var euler = localRotation.eulerAngles;
                    UpsertComponent(clip, path, "localEulerAnglesRaw.x", time, euler.x, bindings, curves);
                    UpsertComponent(clip, path, "localEulerAnglesRaw.y", time, euler.y, bindings, curves);
                    UpsertComponent(clip, path, "localEulerAnglesRaw.z", time, euler.z, bindings, curves);
                }
                else
                {
                    var aligned = AlignQuaternion(EvaluateQuaternion(clip, path, time), localRotation);
                    UpsertComponent(clip, path, "m_LocalRotation.x", time, aligned.x, bindings, curves);
                    UpsertComponent(clip, path, "m_LocalRotation.y", time, aligned.y, bindings, curves);
                    UpsertComponent(clip, path, "m_LocalRotation.z", time, aligned.z, bindings, curves);
                    UpsertComponent(clip, path, "m_LocalRotation.w", time, aligned.w, bindings, curves);
                }
            }

            if (writeScale)
            {
                UpsertComponent(clip, path, "m_LocalScale.x", time, localScale.x, bindings, curves);
                UpsertComponent(clip, path, "m_LocalScale.y", time, localScale.y, bindings, curves);
                UpsertComponent(clip, path, "m_LocalScale.z", time, localScale.z, bindings, curves);
            }

            if (bindings.Count == 0)
                return;

            Undo.RegisterCompleteObjectUndo(clip, "Animation Pose Key");
            AnimationUtility.SetEditorCurves(clip, bindings.ToArray(), curves.ToArray());
            if (writeRotation)
                clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
        }

        public static void RemoveKeysAt(AnimationClip clip, string path, float time)
        {
            if (clip == null)
                return;

            var bindings = AnimationUtility.GetCurveBindings(clip);
            var outBindings = new List<EditorCurveBinding>();
            var outCurves = new List<AnimationCurve>();
            var changed = false;

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.type != typeof(Transform) || binding.path != path)
                    continue;

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                    continue;

                var index = FindKeyIndex(curve, time);
                if (index < 0)
                    continue;

                curve.RemoveKey(index);
                outBindings.Add(binding);
                outCurves.Add(curve.keys.Length == 0 ? null : curve);
                changed = true;
            }

            if (!changed)
                return;

            Undo.RegisterCompleteObjectUndo(clip, "Remove Animation Key");
            AnimationUtility.SetEditorCurves(clip, outBindings.ToArray(), outCurves.ToArray());
            EditorUtility.SetDirty(clip);
        }

        private static bool UsesEuler(AnimationClip clip, string path)
        {
            var euler = EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x");
            return AnimationUtility.GetEditorCurve(clip, euler) != null;
        }

        private static Quaternion EvaluateQuaternion(AnimationClip clip, string path, float time)
        {
            return new Quaternion(
                Evaluate(clip, path, "m_LocalRotation.x", time),
                Evaluate(clip, path, "m_LocalRotation.y", time),
                Evaluate(clip, path, "m_LocalRotation.z", time),
                Evaluate(clip, path, "m_LocalRotation.w", time));
        }

        private static float Evaluate(AnimationClip clip, string path, string property, float time)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            return curve != null ? curve.Evaluate(time) : 0f;
        }

        private static Quaternion AlignQuaternion(Quaternion previous, Quaternion next)
        {
            if (previous.x == 0f && previous.y == 0f && previous.z == 0f && previous.w == 0f)
                return next;
            return Quaternion.Dot(previous, next) < 0f
                ? new Quaternion(-next.x, -next.y, -next.z, -next.w)
                : next;
        }

        private static void UpsertComponent(
            AnimationClip clip,
            string path,
            string property,
            float time,
            float value,
            List<EditorCurveBinding> bindings,
            List<AnimationCurve> curves)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            UpsertKey(curve, time, value);
            bindings.Add(binding);
            curves.Add(curve);
        }

        private static void UpsertKey(AnimationCurve curve, float time, float value)
        {
            var index = FindKeyIndex(curve, time);
            var key = new Keyframe(time, value);
            index = index >= 0 ? curve.MoveKey(index, key) : curve.AddKey(key);
            AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
        }

        private static int FindKeyIndex(AnimationCurve curve, float time)
        {
            var keys = curve.keys;
            for (var i = 0; i < keys.Length; i++)
            {
                if (Mathf.Abs(keys[i].time - time) <= TimeEpsilon)
                    return i;
            }

            return -1;
        }

        private static bool MatchesChannel(string propertyName, TransformChannel channel)
        {
            return channel switch
            {
                TransformChannel.Position => propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal),
                TransformChannel.Rotation => propertyName.StartsWith("m_LocalRotation", StringComparison.Ordinal)
                                             || propertyName.StartsWith("localEulerAnglesRaw", StringComparison.Ordinal),
                TransformChannel.Scale => propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal),
                _ => false
            };
        }
    }

    internal enum TransformChannel
    {
        Position,
        Rotation,
        Scale
    }
}
