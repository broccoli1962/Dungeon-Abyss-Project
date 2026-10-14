using UnityEditor;
using UnityEngine;

namespace DdaIT.Util.Editor
{
    /// <summary>
    /// 미리보기 투영에 맞춘 Scene 뷰형 이동/회전/스케일 기즈모.
    /// </summary>
    public sealed class AnimationClipPreviewGizmo
    {
        public enum Axis
        {
            None = -1,
            X = 0,
            Y = 1,
            Z = 2,
            Screen = 3
        }

        private static readonly Color[] AxisColors =
        {
            new(0.92f, 0.26f, 0.26f),
            new(0.31f, 0.78f, 0.31f),
            new(0.28f, 0.52f, 1f)
        };

        private Axis _dragAxis = Axis.None;
        private Axis _hoverAxis = Axis.None;
        private Vector3 _startPos;
        private Quaternion _startRot;
        private Vector3 _startScale;
        private Vector3 _axisDir;
        private Vector3 _planeNormal;
        private Vector3 _planePoint;
        private float _startAngle;
        private float _startT;

        public bool IsDragging => _dragAxis != Axis.None;

        public void Cancel()
        {
            _dragAxis = Axis.None;
        }

        public void Draw(Camera camera, Rect rect, Transform target, int tool, bool local)
        {
            if (target == null || camera == null)
                return;

            var origin = target.position;
            var rotation = local ? target.rotation : Quaternion.identity;
            var size = HandleSize(camera, origin);
            var active = _dragAxis != Axis.None ? _dragAxis : _hoverAxis;

            switch (tool)
            {
                case 0:
                    DrawMove(camera, rect, origin, rotation, size, active);
                    break;
                case 1:
                    DrawRotate(camera, rect, origin, rotation, size, active);
                    break;
                default:
                    DrawScale(camera, rect, origin, rotation, size, active);
                    break;
            }
        }

        public bool HandleEvent(
            Event e,
            Camera camera,
            Rect rect,
            Transform target,
            int tool,
            bool local,
            int controlId,
            out bool changed)
        {
            changed = false;
            if (target == null || camera == null || e.alt || e.button == 2)
                return false;

            if (e.type == EventType.MouseMove)
            {
                var hover = Pick(camera, rect, e.mousePosition, target, tool, local);
                if (hover == _hoverAxis)
                    return false;
                _hoverAxis = hover;
                return true;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                var axis = Pick(camera, rect, e.mousePosition, target, tool, local);
                if (axis == Axis.None)
                    return false;

                BeginDrag(camera, rect, e.mousePosition, target, tool, local, axis);
                GUIUtility.hotControl = controlId;
                e.Use();
                return true;
            }

            if (e.type == EventType.MouseDrag && GUIUtility.hotControl == controlId && _dragAxis != Axis.None)
            {
                changed = ApplyDrag(camera, rect, e.mousePosition, target, tool);
                e.Use();
                return true;
            }

            if (e.rawType == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                var dragging = _dragAxis != Axis.None;
                _dragAxis = Axis.None;
                GUIUtility.hotControl = 0;
                if (!dragging)
                    return false;
                e.Use();
                return true;
            }

            return GUIUtility.hotControl == controlId && _dragAxis != Axis.None;
        }

        private void DrawMove(Camera camera, Rect rect, Vector3 origin, Quaternion rotation, float size, Axis active)
        {
            DrawCenter(camera, rect, origin, active == Axis.Screen, 6f);
            for (var i = 0; i < 3; i++)
            {
                var dir = rotation * AxisDirection(i);
                var color = AxisColor(i, active);
                if (!TryProject(camera, rect, origin, out var from) ||
                    !TryProject(camera, rect, origin + dir * size, out var to))
                    continue;

                DrawLine(from, to, color, active == (Axis)i ? 4f : 2.5f);
                DrawArrowHead(from, to, color);
            }
        }

        private void DrawRotate(Camera camera, Rect rect, Vector3 origin, Quaternion rotation, float size, Axis active)
        {
            DrawScreenCircle(camera, rect, origin, size * 1.15f, new Color(0.8f, 0.8f, 0.8f, active == Axis.Screen ? 1f : 0.55f));
            for (var i = 0; i < 3; i++)
            {
                var axis = rotation * AxisDirection(i);
                DrawCircle(camera, rect, origin, axis, size, AxisColor(i, active), 48);
            }
        }

        private void DrawScale(Camera camera, Rect rect, Vector3 origin, Quaternion rotation, float size, Axis active)
        {
            DrawCenter(camera, rect, origin, active == Axis.Screen, 7f);
            for (var i = 0; i < 3; i++)
            {
                var dir = rotation * AxisDirection(i);
                var color = AxisColor(i, active);
                if (!TryProject(camera, rect, origin, out var from) ||
                    !TryProject(camera, rect, origin + dir * size, out var to))
                    continue;

                DrawLine(from, to, color, active == (Axis)i ? 4f : 2.5f);
                Handles.color = color;
                Handles.DrawSolidRectangleWithOutline(
                    new Rect(to.x - 5f, to.y - 5f, 10f, 10f),
                    color,
                    Color.black);
            }
        }

        private Axis Pick(Camera camera, Rect rect, Vector2 mouse, Transform target, int tool, bool local)
        {
            var origin = target.position;
            var rotation = local ? target.rotation : Quaternion.identity;
            var size = HandleSize(camera, origin);
            var best = Axis.None;
            var bestDist = 10f;

            if (tool == 1)
            {
                var screenDist = DistanceToCircle(camera, rect, origin, camera.transform.forward, size * 1.15f, mouse);
                if (screenDist < bestDist)
                {
                    bestDist = screenDist;
                    best = Axis.Screen;
                }

                for (var i = 0; i < 3; i++)
                {
                    var dist = DistanceToCircle(camera, rect, origin, rotation * AxisDirection(i), size, mouse);
                    if (dist >= bestDist)
                        continue;
                    bestDist = dist;
                    best = (Axis)i;
                }

                return bestDist < 10f ? best : Axis.None;
            }

            if (TryProject(camera, rect, origin, out var center) && Vector2.Distance(center, mouse) < 10f)
                return Axis.Screen;

            for (var i = 0; i < 3; i++)
            {
                if (!TryProject(camera, rect, origin + rotation * AxisDirection(i) * size, out var tip) ||
                    !TryProject(camera, rect, origin, out var from))
                    continue;

                var dist = DistanceToSegment(mouse, from, tip);
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                best = (Axis)i;
            }

            return bestDist < 10f ? best : Axis.None;
        }

        private void BeginDrag(Camera camera, Rect rect, Vector2 mouse, Transform target, int tool, bool local, Axis axis)
        {
            _dragAxis = axis;
            _startPos = target.position;
            _startRot = target.rotation;
            _startScale = target.localScale;
            _axisDir = axis == Axis.Screen
                ? camera.transform.forward
                : (local ? target.rotation : Quaternion.identity) * AxisDirection((int)axis);
            _axisDir.Normalize();

            if (axis == Axis.Screen)
            {
                _planeNormal = camera.transform.forward;
                _planePoint = _startPos;
            }
            else if (tool == 1)
            {
                _planeNormal = _axisDir;
                _planePoint = _startPos;
            }
            else
            {
                _planeNormal = Vector3.Cross(_axisDir, Vector3.Cross(camera.transform.forward, _axisDir));
                if (_planeNormal.sqrMagnitude < 0.0001f)
                    _planeNormal = camera.transform.forward;
                _planeNormal.Normalize();
                _planePoint = _startPos;
            }

            if (!RayPlane(GuiToWorldRay(camera, rect, mouse), _planePoint, _planeNormal, out var hit))
                hit = _startPos;

            _startT = Vector3.Dot(hit - _startPos, _axisDir);
            _startAngle = AngleOnPlane(hit - _startPos, _axisDir);
        }

        private bool ApplyDrag(Camera camera, Rect rect, Vector2 mouse, Transform target, int tool)
        {
            if (!RayPlane(GuiToWorldRay(camera, rect, mouse), _planePoint, _planeNormal, out var hit))
                return false;

            switch (tool)
            {
                case 0:
                    if (_dragAxis == Axis.Screen)
                        target.position = _startPos + Vector3.ProjectOnPlane(hit - _startPos, camera.transform.forward);
                    else
                        target.position = _startPos + _axisDir * (Vector3.Dot(hit - _startPos, _axisDir) - _startT);
                    return true;
                case 1:
                    var angle = Mathf.DeltaAngle(_startAngle * Mathf.Rad2Deg, AngleOnPlane(hit - _startPos, _axisDir) * Mathf.Rad2Deg);
                    target.rotation = Quaternion.AngleAxis(angle, _axisDir) * _startRot;
                    return true;
                default:
                    if (_dragAxis == Axis.Screen)
                    {
                        if (!TryProject(camera, rect, _startPos, out var startGui))
                            return false;
                        var delta = mouse - startGui;
                        var uniform = 1f + (delta.x - delta.y) * 0.01f;
                        target.localScale = _startScale * Mathf.Max(0.01f, uniform);
                        return true;
                    }

                    var t = Vector3.Dot(hit - _startPos, _axisDir);
                    var ratio = Mathf.Abs(_startT) < 0.0001f ? 1f : t / _startT;
                    ratio = Mathf.Max(0.01f, ratio);
                    var axisIndex = (int)_dragAxis;
                    var scale = _startScale;
                    scale[axisIndex] = _startScale[axisIndex] * ratio;
                    target.localScale = scale;
                    return true;
            }
        }

        private static void DrawCircle(Camera camera, Rect rect, Vector3 origin, Vector3 axis, float radius, Color color, int segments)
        {
            axis.Normalize();
            var tangent = Vector3.Cross(axis, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f)
                tangent = Vector3.Cross(axis, Vector3.right);
            tangent.Normalize();
            var bitangent = Vector3.Cross(axis, tangent);
            Vector2? prev = null;
            for (var i = 0; i <= segments; i++)
            {
                var a = i * Mathf.PI * 2f / segments;
                var point = origin + (tangent * Mathf.Cos(a) + bitangent * Mathf.Sin(a)) * radius;
                if (!TryProject(camera, rect, point, out var gui))
                {
                    prev = null;
                    continue;
                }

                if (prev.HasValue)
                {
                    var facing = Vector3.Dot((point - origin).normalized, -camera.transform.forward);
                    var fade = color;
                    fade.a *= 0.25f + 0.75f * Mathf.Max(0f, facing);
                    DrawLine(prev.Value, gui, fade, 2.4f);
                }

                prev = gui;
            }
        }

        private static void DrawScreenCircle(Camera camera, Rect rect, Vector3 origin, float radius, Color color)
        {
            DrawCircle(camera, rect, origin, camera.transform.forward, radius, color, 48);
        }

        private static void DrawCenter(Camera camera, Rect rect, Vector3 origin, bool active, float radius)
        {
            if (!TryProject(camera, rect, origin, out var gui))
                return;
            Handles.color = active ? Color.white : new Color(1f, 1f, 1f, 0.7f);
            Handles.DrawSolidDisc(gui, Vector3.forward, radius);
            Handles.color = Color.black;
            Handles.DrawWireDisc(gui, Vector3.forward, radius);
        }

        private static void DrawArrowHead(Vector2 from, Vector2 to, Color color)
        {
            var dir = to - from;
            if (dir.sqrMagnitude < 1f)
                return;
            dir.Normalize();
            var perp = new Vector2(-dir.y, dir.x);
            DrawLine(to, to - dir * 11f + perp * 5f, color, 2.2f);
            DrawLine(to, to - dir * 11f - perp * 5f, color, 2.2f);
        }

        private static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(width, new Vector3(a.x, a.y, 0f), new Vector3(b.x, b.y, 0f));
        }

        private Color AxisColor(int axis, Axis active)
        {
            var color = AxisColors[axis];
            if (active == (Axis)axis)
                return Color.Lerp(color, Color.white, 0.35f);
            return color;
        }

        private static Vector3 AxisDirection(int axis)
        {
            return axis switch
            {
                0 => Vector3.right,
                1 => Vector3.up,
                _ => Vector3.forward
            };
        }

        private static float DistanceToCircle(Camera camera, Rect rect, Vector3 origin, Vector3 axis, float radius, Vector2 mouse)
        {
            axis.Normalize();
            var tangent = Vector3.Cross(axis, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f)
                tangent = Vector3.Cross(axis, Vector3.right);
            tangent.Normalize();
            var bitangent = Vector3.Cross(axis, tangent);
            var best = 64f;
            Vector2? prev = null;
            const int segments = 32;
            for (var i = 0; i <= segments; i++)
            {
                var a = i * Mathf.PI * 2f / segments;
                var point = origin + (tangent * Mathf.Cos(a) + bitangent * Mathf.Sin(a)) * radius;
                if (!TryProject(camera, rect, point, out var gui))
                {
                    prev = null;
                    continue;
                }

                if (prev.HasValue)
                    best = Mathf.Min(best, DistanceToSegment(mouse, prev.Value, gui));
                prev = gui;
            }

            return best;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        private static float AngleOnPlane(Vector3 vector, Vector3 axis)
        {
            axis.Normalize();
            var tangent = Vector3.Cross(axis, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f)
                tangent = Vector3.Cross(axis, Vector3.right);
            tangent.Normalize();
            var bitangent = Vector3.Cross(axis, tangent);
            var planar = vector - axis * Vector3.Dot(vector, axis);
            return Mathf.Atan2(Vector3.Dot(planar, bitangent), Vector3.Dot(planar, tangent));
        }

        private static bool RayPlane(Ray ray, Vector3 planePoint, Vector3 planeNormal, out Vector3 hit)
        {
            var denom = Vector3.Dot(ray.direction, planeNormal);
            if (Mathf.Abs(denom) < 1e-5f)
            {
                hit = default;
                return false;
            }

            var t = Vector3.Dot(planePoint - ray.origin, planeNormal) / denom;
            if (t < 0f)
            {
                hit = default;
                return false;
            }

            hit = ray.origin + ray.direction * t;
            return true;
        }

        public static bool TryProject(Camera camera, Rect rect, Vector3 world, out Vector2 gui)
        {
            var clip = camera.projectionMatrix * camera.worldToCameraMatrix * new Vector4(world.x, world.y, world.z, 1f);
            if (clip.w <= 0.0001f)
            {
                gui = default;
                return false;
            }

            var ndc = clip / clip.w;
            gui = new Vector2(
                rect.x + (ndc.x * 0.5f + 0.5f) * rect.width,
                rect.y + (-ndc.y * 0.5f + 0.5f) * rect.height);
            return true;
        }

        public static Ray GuiToWorldRay(Camera camera, Rect rect, Vector2 mouse)
        {
            var viewportX = (mouse.x - rect.x) / Mathf.Max(1f, rect.width);
            var viewportY = 1f - (mouse.y - rect.y) / Mathf.Max(1f, rect.height);
            var u = viewportX * 2f - 1f;
            var v = viewportY * 2f - 1f;
            var viewProj = camera.projectionMatrix * camera.worldToCameraMatrix;
            var inv = viewProj.inverse;
            var near = inv * new Vector4(u, v, -1f, 1f);
            var far = inv * new Vector4(u, v, 1f, 1f);
            near /= near.w;
            far /= far.w;
            var origin = new Vector3(near.x, near.y, near.z);
            var dir = new Vector3(far.x - near.x, far.y - near.y, far.z - near.z);
            if (dir.sqrMagnitude < 1e-8f)
                dir = camera.transform.forward;
            return new Ray(origin, dir.normalized);
        }

        public static float HandleSize(Camera camera, Vector3 world)
        {
            return Mathf.Max(0.05f, Vector3.Distance(camera.transform.position, world) * 0.14f);
        }
    }
}
