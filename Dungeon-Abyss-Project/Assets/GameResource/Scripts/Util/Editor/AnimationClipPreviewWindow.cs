using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DdaIT.Util.Editor
{
    /// <summary>
    /// 테스트용 Animator 없이 AnimationClip을 에디터에서 미리보고 설정을 편집한다.
    /// </summary>
    public sealed class AnimationClipPreviewWindow : EditorWindow
    {
        private const string PrefSync = "DdaIT.AnimationClipPreview.SyncSelection";
        private const string PrefLastModel = "DdaIT.AnimationClipPreview.LastModel";
        private const string PrefModelPrefix = "DdaIT.AnimationClipPreview.Model.";

        private static readonly string[] ClipSuffixes =
        {
            "_Idle", "_Walk", "_Run", "_Attack", "_Hit", "_Death", "_Die",
            "_Aim", "_Crawl", "_Sneak", "_Strafe",
            "_WalkF", "_WalkB", "_WalkL", "_WalkR", "_RunF",
            " Idle", " Walk", " Run", " Attack", " Death", " Aim",
            " WalkF B", " WalkF", " WalkB", " WalkL", " WalkR", " RunF"
        };

        [SerializeField] private AnimationClip _clip;
        [SerializeField] private GameObject _model;
        [SerializeField] private bool _syncSelection = true;
        [SerializeField] private bool _loop = true;
        [SerializeField] private bool _lockRoot = true;
        [SerializeField] private bool _poseEdit;
        [SerializeField] private bool _autoKey;
        [SerializeField] private bool _showAllBones;
        [SerializeField] private bool _localSpace = true;
        [SerializeField] private float _speed = 1f;

        private PreviewRenderUtility _preview;
        private GameObject _instance;
        private GameObject _sampleRoot;
        private AnimationModeDriver _driver;
        private Vector3 _pivot;
        private float _distance = 3f;
        private float _yaw = 160f;
        private float _pitch = 12f;
        private float _time;
        private bool _playing;
        private double _lastUpdateTime;
        private bool _frameCamera = true;

        private EditorCurveBinding[] _bindings = Array.Empty<EditorCurveBinding>();
        private AnimationEvent[] _events = Array.Empty<AnimationEvent>();
        private readonly List<AnimationClip> _relatedClips = new();
        private readonly List<string> _missingPaths = new();
        private Vector2 _clipListScroll;
        private Vector2 _boneListScroll;
        private Vector2 _mainScroll;
        private bool _settingsFoldout = true;
        private bool _eventsFoldout = true;
        private bool _bindingsFoldout;
        private PoseTool _poseTool = PoseTool.Rotate;
        private string _selectedPath;
        private string _boneSearch = "";
        private bool _livePose;
        private bool _handleDirty;
        private bool _poseDragging;
        private AnimationClipPreviewGizmo _gizmo;
        private readonly List<string> _bonePaths = new();
        private readonly List<float> _selectedKeyTimes = new();

        private AnimationClipPreviewGizmo Gizmo => _gizmo ??= new AnimationClipPreviewGizmo();

        private enum PoseTool
        {
            Move,
            Rotate,
            Scale
        }

        [MenuItem("Tools/Animation")]
        public static void Open()
        {
            var window = GetWindow<AnimationClipPreviewWindow>("Animation");
            window.minSize = new Vector2(640f, 560f);
            window.ApplySelection(true);
            window.Show();
        }

        private void OnEnable()
        {
            _syncSelection = EditorPrefs.GetBool(PrefSync, true);
            autoRepaintOnSceneChange = true;
            wantsMouseMove = true;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            RefreshClipCache();
            ApplySelection(false);
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorPrefs.SetBool(PrefSync, _syncSelection);
            StopPlayback();
            DisposePreview();
        }

        private void OnSelectionChange()
        {
            ApplySelection(false);
            Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                StopPlayback();
                DisposePreview();
            }

            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!_playing || _clip == null || EditorApplication.isPlaying)
                return;

            var now = EditorApplication.timeSinceStartup;
            if (_lastUpdateTime <= 0d)
                _lastUpdateTime = now;

            var dt = (float)(now - _lastUpdateTime);
            _lastUpdateTime = now;
            GetPlaybackRange(out var start, out var stop);
            var length = Mathf.Max(0.0001f, stop - start);
            _time += dt * Mathf.Max(0.01f, _speed);

            if (_time > stop)
            {
                if (_loop)
                    _time = start + Mathf.Repeat(_time - start, length);
                else
                {
                    _time = stop;
                    StopPlayback();
                }
            }

            SamplePose();
            Repaint();
        }

        private void OnGUI()
        {
            HandleKeyboard();

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서는 미리보기를 사용할 수 없습니다.", MessageType.Info);
                return;
            }

            DrawToolbar();
            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            if (_poseEdit)
                DrawBoneList();
            else
                DrawClipList();
            EditorGUILayout.BeginVertical();
            DrawPreviewArea();
            EditorGUILayout.LabelField(
                _poseEdit
                    ? "본 클릭 선택 · RGB 기즈모로 이동/회전/스케일 · Alt 카메라 · W/E/R"
                    : "드래그 회전 · Shift/휠버튼 이동 · 휠 줌 · 우클릭 프레이밍 · Space 재생",
                EditorStyles.miniLabel);
            DrawTransport();
            if (_poseEdit)
                DrawPoseInspector();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            DrawClipInfo();
            DrawSettings();
            DrawEvents();
            DrawBindings();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("클립", GUILayout.Width(28f));
            EditorGUI.BeginChangeCheck();
            var clip = (AnimationClip)EditorGUILayout.ObjectField(_clip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
                SetClip(clip);

            GUILayout.Label("모델", GUILayout.Width(28f));
            EditorGUI.BeginChangeCheck();
            var model = (GameObject)EditorGUILayout.ObjectField(_model, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
                SetModel(model);

            if (GUILayout.Button("자동 모델", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                TryAssignModel(true);

            _syncSelection = GUILayout.Toggle(_syncSelection, "선택 동기화", EditorStyles.toolbarButton, GUILayout.Width(84f));
            EditorGUI.BeginChangeCheck();
            _poseEdit = GUILayout.Toggle(_poseEdit, "포즈 편집", EditorStyles.toolbarButton, GUILayout.Width(72f));
            if (EditorGUI.EndChangeCheck() && _poseEdit)
            {
                StopPlayback();
                RefreshBonePaths();
            }

            EditorGUILayout.EndHorizontal();

            if (_clip == null)
                EditorGUILayout.HelpBox("AnimationClip을 지정하거나 프로젝트에서 선택하세요. 미리볼 프리팹/모델이 필요하면 함께 지정합니다.", MessageType.Info);
            else if (_model == null)
                EditorGUILayout.HelpBox("이 클립을 재생할 모델(프리팹/FBX)을 지정하세요. '자동 모델'로 가까운 에셋을 찾습니다.", MessageType.Warning);
        }

        private void DrawClipList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(188f));
            EditorGUILayout.LabelField("관련 클립", EditorStyles.boldLabel);
            _clipListScroll = EditorGUILayout.BeginScrollView(_clipListScroll, GUILayout.ExpandHeight(true));

            for (var i = 0; i < _relatedClips.Count; i++)
            {
                var related = _relatedClips[i];
                if (related == null)
                    continue;

                var selected = related == _clip;
                var style = selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                if (selected)
                    GUI.backgroundColor = new Color(0.45f, 0.7f, 1f);
                if (GUILayout.Button(related.name, style))
                    SetClip(related);
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawBoneList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(188f));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("본", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _showAllBones = GUILayout.Toggle(_showAllBones, "전체", EditorStyles.miniButton, GUILayout.Width(40f));
            if (EditorGUI.EndChangeCheck())
                RefreshBonePaths();
            EditorGUILayout.EndHorizontal();
            _boneSearch = EditorGUILayout.TextField(_boneSearch, EditorStyles.toolbarSearchField);
            _boneListScroll = EditorGUILayout.BeginScrollView(_boneListScroll, GUILayout.ExpandHeight(true));

            for (var i = 0; i < _bonePaths.Count; i++)
            {
                var path = _bonePaths[i];
                var leaf = BoneDisplayName(path);
                if (!string.IsNullOrEmpty(_boneSearch) &&
                    leaf.IndexOf(_boneSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                    path.IndexOf(_boneSearch, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var selected = path == _selectedPath;
                if (selected)
                    GUI.backgroundColor = new Color(0.45f, 0.7f, 1f);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(BoneDepth(path) * 8f);
                if (GUILayout.Button(new GUIContent(leaf, path), EditorStyles.miniButton))
                    SelectBone(path);
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPoseInspector()
        {
            var editable = CanEditPose();
            using (new EditorGUI.DisabledScope(!editable))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                _poseTool = (PoseTool)GUILayout.Toolbar((int)_poseTool, new[] { "이동", "회전", "스케일" }, GUILayout.Height(20f));
                _localSpace = GUILayout.Toggle(_localSpace, _localSpace ? "Local" : "Global", GUILayout.Width(58f));
                _autoKey = GUILayout.Toggle(_autoKey, "Auto Key", GUILayout.Width(72f));
                EditorGUILayout.EndHorizontal();

                var bone = FindSelectedBone();
                EditorGUILayout.LabelField("선택", string.IsNullOrEmpty(_selectedPath) ? "(없음)" : BoneDisplayName(_selectedPath));
                if (_livePose)
                    EditorGUILayout.HelpBox("키를 기록하지 않은 포즈입니다. 시간을 바꾸면 사라집니다.", MessageType.Warning);

                if (bone != null)
                {
                    EditorGUI.BeginChangeCheck();
                    var position = EditorGUILayout.Vector3Field("위치", bone.localPosition);
                    var euler = EditorGUILayout.Vector3Field("회전", bone.localEulerAngles);
                    var scale = EditorGUILayout.Vector3Field("스케일", bone.localScale);
                    if (EditorGUI.EndChangeCheck())
                    {
                        bone.localPosition = position;
                        bone.localEulerAngles = euler;
                        bone.localScale = scale;
                        _livePose = true;
                        if (_autoKey)
                            RecordSelectedPose(true, true, true);
                    }
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("키 기록"))
                    RecordSelectedPose(false, false, false);
                if (GUILayout.Button("키 삭제"))
                    DeleteSelectedKey();
                if (GUILayout.Button("이전 키"))
                    JumpSelectedKey(true);
                if (GUILayout.Button("다음 키"))
                    JumpSelectedKey(false);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (_clip != null && _clip.humanMotion)
                EditorGUILayout.HelpBox("Humanoid 클립은 머슬 커브라서 관절을 직접 고칠 수 없습니다.", MessageType.Info);
            else if (_clip != null && IsModelSubAsset(_clip))
                EditorGUILayout.HelpBox("FBX 클립은 읽기 전용입니다. 복제한 뒤 포즈를 편집하세요.", MessageType.Info);
        }

        private void DrawPreviewArea()
        {
            var height = Mathf.Clamp(position.height * 0.42f, 220f, 420f);
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

            HandleDragAndDrop(rect);

            if (_clip == null || _model == null)
            {
                HandlePreviewInput(rect);
                DrawCenteredLabel(rect, "클립과 모델을 지정하면 미리보기가 표시됩니다.");
                return;
            }

            EnsurePreview();
            if (_instance == null)
                RebuildInstance();

            if (!_livePose)
                SamplePose();

            if (_preview != null)
                PreparePreviewCamera(rect);

            if (Event.current.type == EventType.Repaint && _preview != null && _instance != null)
            {
                _preview.BeginPreview(rect, GUIStyle.none);
                PreparePreviewCamera(rect);
                _preview.Render(true, false);
                _preview.EndAndDrawPreview(rect);
                PreparePreviewCamera(rect);
            }

            if (_poseEdit && CanEditPose() && _preview != null && _instance != null)
                DrawPoseOverlay(rect);

            HandlePreviewInput(rect);
        }

        private void DrawTransport()
        {
            using (new EditorGUI.DisabledScope(_clip == null))
            {
                GetPlaybackRange(out var start, out var stop);
                var length = Mathf.Max(0.0001f, stop - start);
                var frameRate = _clip != null ? Mathf.Max(1f, _clip.frameRate) : 30f;

                DrawTimeline(start, stop);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(_playing ? "일시정지" : "재생", GUILayout.Width(72f)))
                    TogglePlay();
                if (GUILayout.Button("정지", GUILayout.Width(48f)))
                {
                    StopPlayback();
                    _time = start;
                    SamplePose();
                }

                if (GUILayout.Button("<", GUILayout.Width(24f)))
                    StepFrame(-1f / frameRate);
                if (GUILayout.Button(">", GUILayout.Width(24f)))
                    StepFrame(1f / frameRate);

                _loop = GUILayout.Toggle(_loop, "루프", GUILayout.Width(44f));
                _lockRoot = GUILayout.Toggle(_lockRoot, "루트 고정", GUILayout.Width(68f));
                GUILayout.Label("배속", GUILayout.Width(32f));
                _speed = EditorGUILayout.Slider(_speed, 0.05f, 2f, GUILayout.Width(140f));
                GUILayout.FlexibleSpace();

                var frame = Mathf.RoundToInt(_time * frameRate);
                var total = Mathf.RoundToInt(stop * frameRate);
                GUILayout.Label($"{_time:0.000}s  /  {stop:0.000}s    {frame}f / {total}f", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                _time = EditorGUILayout.Slider("시간", _time, start, start + length);
                if (EditorGUI.EndChangeCheck())
                {
                    StopPlayback();
                    DiscardLivePose();
                    SamplePose();
                }
            }
        }

        private void DrawTimeline(float start, float stop)
        {
            var length = Mathf.Max(0.0001f, stop - start);
            var rect = GUILayoutUtility.GetRect(18f, 20f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));

            var filled = rect;
            filled.width = rect.width * Mathf.Clamp01((_time - start) / length);
            EditorGUI.DrawRect(filled, new Color(0.23f, 0.42f, 0.72f, 0.85f));

            for (var i = 0; i < _selectedKeyTimes.Count; i++)
            {
                var x = rect.x + rect.width * Mathf.Clamp01((_selectedKeyTimes[i] - start) / length);
                EditorGUI.DrawRect(new Rect(x - 1f, rect.y, 2f, rect.height), new Color(0.35f, 0.85f, 1f));
            }

            if (_events != null)
            {
                for (var i = 0; i < _events.Length; i++)
                {
                    var evt = _events[i];
                    var x = rect.x + rect.width * Mathf.Clamp01((evt.time - start) / length);
                    EditorGUI.DrawRect(new Rect(x - 1f, rect.y, 2f, rect.height), new Color(1f, 0.85f, 0.2f));
                }
            }

            var playhead = rect.x + rect.width * Mathf.Clamp01((_time - start) / length);
            EditorGUI.DrawRect(new Rect(playhead - 1f, rect.y, 2f, rect.height), Color.white);

            var id = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && e.button == 0)
            {
                GUIUtility.hotControl = id;
                StopPlayback();
                _time = start + Mathf.Clamp01((e.mousePosition.x - rect.x) / rect.width) * length;
                DiscardLivePose();
                SamplePose();
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                _time = start + Mathf.Clamp01((e.mousePosition.x - rect.x) / rect.width) * length;
                DiscardLivePose();
                SamplePose();
                e.Use();
            }
            else if (e.type == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        private void DrawClipInfo()
        {
            if (_clip == null)
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            var kind = _clip.legacy ? "Legacy" : _clip.humanMotion ? "Humanoid" : "Generic";
            EditorGUILayout.LabelField($"{kind}  ·  길이 {_clip.length:0.###}s  ·  {_clip.frameRate:0.##} fps  ·  커브 {_bindings.Length}  ·  이벤트 {_events.Length}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("에셋 선택", GUILayout.Width(76f)))
            {
                Selection.activeObject = _clip;
                EditorGUIUtility.PingObject(_clip);
            }

            if (GUILayout.Button("Animation 창", GUILayout.Width(96f)))
            {
                Selection.activeObject = _clip;
                EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
            }

            if (GUILayout.Button("클립 복제", GUILayout.Width(72f)))
                DuplicateClip();
            EditorGUILayout.EndHorizontal();

            if (_missingPaths.Count > 0)
            {
                var shown = Mathf.Min(3, _missingPaths.Count);
                var detail = string.Join(", ", _missingPaths.GetRange(0, shown));
                if (_missingPaths.Count > shown)
                    detail += $" 외 {_missingPaths.Count - shown}개";
                EditorGUILayout.HelpBox($"이 모델에서 {_missingPaths.Count}개 경로를 찾지 못했습니다: {detail}", MessageType.Warning);
            }

            if (IsModelSubAsset(_clip))
                EditorGUILayout.HelpBox("FBX에 포함된 클립은 읽기 전용입니다. 설정을 고치려면 복제하세요.", MessageType.Info);
        }

        private void DrawSettings()
        {
            if (_clip == null)
                return;

            _settingsFoldout = EditorGUILayout.Foldout(_settingsFoldout, "클립 설정", true);
            if (!_settingsFoldout)
                return;

            using (new EditorGUI.DisabledScope(IsModelSubAsset(_clip)))
            {
                var settings = AnimationUtility.GetAnimationClipSettings(_clip);
                EditorGUI.BeginChangeCheck();
                settings.loopTime = EditorGUILayout.Toggle("Loop Time", settings.loopTime);
                settings.loopBlend = EditorGUILayout.Toggle("Loop Pose", settings.loopBlend);
                settings.cycleOffset = EditorGUILayout.Slider("Cycle Offset", settings.cycleOffset, 0f, 1f);
                settings.startTime = EditorGUILayout.FloatField("Start", settings.startTime);
                settings.stopTime = EditorGUILayout.FloatField("Stop", settings.stopTime);

                if (_clip.humanMotion)
                {
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField("Root Transform", EditorStyles.miniBoldLabel);
                    settings.keepOriginalOrientation = EditorGUILayout.Toggle("Bake Orientation", settings.keepOriginalOrientation);
                    settings.keepOriginalPositionY = EditorGUILayout.Toggle("Bake Position Y", settings.keepOriginalPositionY);
                    settings.keepOriginalPositionXZ = EditorGUILayout.Toggle("Bake Position XZ", settings.keepOriginalPositionXZ);
                    settings.mirror = EditorGUILayout.Toggle("Mirror", settings.mirror);
                }

                var frameRate = EditorGUILayout.FloatField("Frame Rate", _clip.frameRate);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_clip, "Animation Clip Settings");
                    _clip.frameRate = Mathf.Max(1f, frameRate);
                    AnimationUtility.SetAnimationClipSettings(_clip, settings);
                    EditorUtility.SetDirty(_clip);
                    RefreshClipCache();
                }
            }
        }

        private void DrawEvents()
        {
            if (_clip == null)
                return;

            _eventsFoldout = EditorGUILayout.Foldout(_eventsFoldout, $"이벤트 ({_events.Length})", true);
            if (!_eventsFoldout)
                return;

            using (new EditorGUI.DisabledScope(IsModelSubAsset(_clip)))
            {
                if (GUILayout.Button("현재 시간에 이벤트 추가", GUILayout.Width(180f)))
                    AddEventAtTime(_time);

                for (var i = 0; i < _events.Length; i++)
                {
                    var evt = _events[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    evt.time = EditorGUILayout.FloatField("시간", evt.time);
                    evt.functionName = EditorGUILayout.TextField(evt.functionName);
                    if (GUILayout.Button("이동", GUILayout.Width(40f)))
                    {
                        StopPlayback();
                        _time = evt.time;
                        SamplePose();
                    }

                    if (GUILayout.Button("삭제", GUILayout.Width(40f)))
                    {
                        EditorGUI.EndChangeCheck();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        RemoveEvent(i);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                    evt.floatParameter = EditorGUILayout.FloatField("Float", evt.floatParameter);
                    evt.intParameter = EditorGUILayout.IntField("Int", evt.intParameter);
                    evt.stringParameter = EditorGUILayout.TextField("String", evt.stringParameter);
                    evt.objectReferenceParameter = EditorGUILayout.ObjectField("Object", evt.objectReferenceParameter, typeof(UnityEngine.Object), false);
                    if (EditorGUI.EndChangeCheck())
                        ApplyEvents();
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void DrawBindings()
        {
            if (_clip == null)
                return;

            _bindingsFoldout = EditorGUILayout.Foldout(_bindingsFoldout, $"바인딩 ({_bindings.Length})", true);
            if (!_bindingsFoldout)
                return;

            var showCount = Mathf.Min(_bindings.Length, 40);
            for (var i = 0; i < showCount; i++)
            {
                var binding = _bindings[i];
                var path = string.IsNullOrEmpty(binding.path) ? "(root)" : binding.path;
                EditorGUILayout.LabelField($"{path}  ·  {binding.propertyName}", EditorStyles.miniLabel);
            }

            if (_bindings.Length > showCount)
                EditorGUILayout.LabelField($"... {_bindings.Length - showCount}개 더 있음", EditorStyles.miniLabel);
        }

        private void HandleKeyboard()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField || _clip == null)
                return;

            var frameStep = 1f / Mathf.Max(1f, _clip.frameRate);
            switch (e.keyCode)
            {
                case KeyCode.Space:
                    TogglePlay();
                    e.Use();
                    break;
                case KeyCode.LeftArrow:
                    StepFrame(-frameStep);
                    e.Use();
                    break;
                case KeyCode.RightArrow:
                    StepFrame(frameStep);
                    e.Use();
                    break;
                case KeyCode.Home:
                    GetPlaybackRange(out var start, out _);
                    StopPlayback();
                    _time = start;
                    DiscardLivePose();
                    SamplePose();
                    e.Use();
                    break;
                case KeyCode.W when _poseEdit:
                    _poseTool = PoseTool.Move;
                    e.Use();
                    break;
                case KeyCode.E when _poseEdit:
                    _poseTool = PoseTool.Rotate;
                    e.Use();
                    break;
                case KeyCode.R when _poseEdit:
                    _poseTool = PoseTool.Scale;
                    e.Use();
                    break;
                case KeyCode.K when _poseEdit:
                    RecordSelectedPose(false, false, false);
                    e.Use();
                    break;
                case KeyCode.Delete when _poseEdit:
                case KeyCode.Backspace when _poseEdit:
                    DeleteSelectedKey();
                    e.Use();
                    break;
                case KeyCode.F when _poseEdit:
                    FrameSelectedBone();
                    e.Use();
                    break;
            }
        }

        private void HandleDragAndDrop(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition))
                return;

            if (e.type is not (EventType.DragUpdated or EventType.DragPerform))
                return;

            var accepted = false;
            for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                var obj = DragAndDrop.objectReferences[i];
                if (obj is AnimationClip or GameObject)
                    accepted = true;
            }

            if (!accepted)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type != EventType.DragPerform)
                return;

            DragAndDrop.AcceptDrag();
            for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                var obj = DragAndDrop.objectReferences[i];
                if (obj is AnimationClip clip)
                    SetClip(clip);
                else if (obj is GameObject go)
                    SetModel(go);
            }

            e.Use();
        }

        private void HandlePreviewInput(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition) && GUIUtility.hotControl == 0)
                return;

            var allowOrbit = !_poseEdit || e.alt || e.button == 2;
            var id = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                _distance = Mathf.Clamp(_distance * (1f + e.delta.y * 0.04f), 0.2f, 40f);
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && allowOrbit && (e.button == 0 || e.button == 2))
            {
                GUIUtility.hotControl = id;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                if (e.button == 2 || e.shift)
                {
                    var pan = _preview != null
                        ? _preview.camera.transform.right * (-e.delta.x * _distance * 0.0025f)
                          + _preview.camera.transform.up * (e.delta.y * _distance * 0.0025f)
                        : Vector3.zero;
                    _pivot += pan;
                }
                else
                {
                    _yaw += e.delta.x * 0.4f;
                    _pitch = Mathf.Clamp(_pitch + e.delta.y * 0.4f, -80f, 80f);
                }

                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1 && rect.Contains(e.mousePosition))
            {
                _frameCamera = true;
                FrameCamera();
                e.Use();
            }
        }

        private void ApplySelection(bool force)
        {
            if (!force && !_syncSelection)
                return;

            if (Selection.activeObject is AnimationClip clip)
                SetClip(clip);
            else if (Selection.activeGameObject != null && IsRenderable(Selection.activeGameObject))
                SetModel(Selection.activeGameObject);
            else if (force && _clip == null && _model == null)
                TryAssignModel(false);
        }

        private void SetClip(AnimationClip clip)
        {
            if (_clip == clip)
                return;

            _clip = clip;
            GetPlaybackRange(out _time, out _);
            DiscardLivePose();
            _selectedPath = null;
            RefreshClipCache();
            if (_model == null)
                TryAssignModel(false);
            RefreshMissingBindings();
            SamplePose();
        }

        private void SetModel(GameObject model)
        {
            if (_model == model)
                return;

            _model = model;
            SaveModelPref(model);
            _frameCamera = true;
            RebuildInstance();
            RefreshRelatedClips();
            RefreshMissingBindings();
        }

        private void TryAssignModel(bool ping)
        {
            var found = FindModelForClip(_clip);
            if (found == null)
            {
                if (ping)
                    UnityEngine.Debug.LogWarning("클립에 맞는 미리보기 모델을 찾지 못했습니다. 프리팹을 직접 지정하세요.");
                return;
            }

            SetModel(found);
            if (ping)
                EditorGUIUtility.PingObject(found);
        }

        private void TogglePlay()
        {
            if (_clip == null || _model == null)
                return;

            _playing = !_playing;
            if (_playing)
                DiscardLivePose();
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            GetPlaybackRange(out var start, out var stop);
            if (_playing && _time >= stop)
                _time = start;
        }

        private void StopPlayback()
        {
            _playing = false;
            _lastUpdateTime = 0d;
        }

        private void StepFrame(float delta)
        {
            if (_clip == null)
                return;

            StopPlayback();
            GetPlaybackRange(out var start, out var stop);
            _time = Mathf.Clamp(_time + delta, start, stop);
            DiscardLivePose();
            SamplePose();
        }

        private void GetPlaybackRange(out float start, out float stop)
        {
            start = 0f;
            stop = _clip != null ? _clip.length : 1f;
            if (_clip == null)
                return;

            var settings = AnimationUtility.GetAnimationClipSettings(_clip);
            start = settings.startTime;
            stop = settings.stopTime;
            if (stop <= start)
            {
                start = 0f;
                stop = Mathf.Max(0.0001f, _clip.length);
            }
        }

        private void RefreshClipCache()
        {
            _bindings = _clip != null ? AnimationUtility.GetCurveBindings(_clip) : Array.Empty<EditorCurveBinding>();
            _events = _clip != null ? AnimationUtility.GetAnimationEvents(_clip) : Array.Empty<AnimationEvent>();
            RefreshRelatedClips();
            RefreshBonePaths();
            RefreshSelectedKeyTimes();
            RefreshMissingBindings();
        }

        private void RefreshRelatedClips()
        {
            _relatedClips.Clear();
            var seen = new HashSet<int>();

            void AddClip(AnimationClip clip)
            {
                if (clip == null || !seen.Add(clip.GetInstanceID()))
                    return;
                _relatedClips.Add(clip);
            }

            AddClip(_clip);

            if (_clip != null)
            {
                var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(_clip))?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir) && dir.StartsWith("Assets", StringComparison.Ordinal))
                {
                    var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { dir });
                    for (var i = 0; i < guids.Length; i++)
                        AddClip(AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[i])));
                }
            }

            if (_model != null)
            {
                var animator = _model.GetComponentInChildren<Animator>(true);
                var controller = animator != null ? animator.runtimeAnimatorController : null;
                if (controller != null)
                {
                    var clips = controller.animationClips;
                    for (var i = 0; i < clips.Length; i++)
                        AddClip(clips[i]);
                }
            }

            _relatedClips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private void RefreshMissingBindings()
        {
            _missingPaths.Clear();
            if (_clip == null || _clip.humanMotion || _sampleRoot == null)
                return;

            var missing = new HashSet<string>();
            for (var i = 0; i < _bindings.Length; i++)
            {
                var path = _bindings[i].path;
                if (string.IsNullOrEmpty(path))
                    continue;
                if (_sampleRoot.transform.Find(path) != null)
                    continue;
                missing.Add(path);
            }

            _missingPaths.AddRange(missing);
        }

        private void RefreshBonePaths()
        {
            _bonePaths.Clear();
            if (_showAllBones && _sampleRoot != null)
                _bonePaths.AddRange(AnimationClipCurveUtil.CollectHierarchyPaths(_sampleRoot.transform));
            else
                _bonePaths.AddRange(AnimationClipCurveUtil.CollectTransformPaths(_bindings));
        }

        private void RefreshSelectedKeyTimes()
        {
            AnimationClipCurveUtil.CollectKeyTimes(_clip, _selectedPath, _selectedKeyTimes);
        }

        private void SelectBone(string path)
        {
            _selectedPath = path;
            RefreshSelectedKeyTimes();
            Repaint();
        }

        private Transform FindSelectedBone()
        {
            return FindBone(_selectedPath);
        }

        private bool CanEditPose()
        {
            return _clip != null && _model != null && !_clip.humanMotion && !IsModelSubAsset(_clip);
        }

        private void DiscardLivePose()
        {
            _livePose = false;
            _handleDirty = false;
            _poseDragging = false;
            Gizmo.Cancel();
        }

        private float SnapKeyTime(float time)
        {
            var frameRate = _clip != null ? Mathf.Max(1f, _clip.frameRate) : 30f;
            return Mathf.Round(time * frameRate) / frameRate;
        }

        private void RecordSelectedPose(bool writePosition, bool writeRotation, bool writeScale)
        {
            if (!CanEditPose())
                return;

            var bone = FindSelectedBone();
            if (bone == null)
                return;

            var path = _selectedPath ?? string.Empty;
            if (!writePosition && !writeRotation && !writeScale)
            {
                writePosition = _poseTool == PoseTool.Move
                    || AnimationClipCurveUtil.PathHasChannel(_bindings, path, TransformChannel.Position);
                writeRotation = _poseTool == PoseTool.Rotate
                    || AnimationClipCurveUtil.PathHasChannel(_bindings, path, TransformChannel.Rotation);
                writeScale = _poseTool == PoseTool.Scale
                    || AnimationClipCurveUtil.PathHasChannel(_bindings, path, TransformChannel.Scale);
                if (!writePosition && !writeRotation && !writeScale)
                    writeRotation = true;
            }

            AnimationClipCurveUtil.SetLocalPose(
                _clip,
                path,
                SnapKeyTime(_time),
                bone.localPosition,
                bone.localRotation,
                bone.localScale,
                writePosition,
                writeRotation,
                writeScale);
            DiscardLivePose();
            RefreshClipCache();
            SamplePose();
        }

        private void DeleteSelectedKey()
        {
            if (!CanEditPose() || _selectedPath == null)
                return;

            AnimationClipCurveUtil.RemoveKeysAt(_clip, _selectedPath, SnapKeyTime(_time));
            DiscardLivePose();
            RefreshClipCache();
            SamplePose();
        }

        private void JumpSelectedKey(bool previous)
        {
            var time = AnimationClipCurveUtil.FindNeighborKeyTime(_clip, _selectedPath, _time, previous);
            if (!time.HasValue)
                return;

            StopPlayback();
            _time = time.Value;
            DiscardLivePose();
            SamplePose();
        }

        private void FrameSelectedBone()
        {
            var bone = FindSelectedBone();
            if (bone == null)
                return;

            _pivot = bone.position;
            Repaint();
        }

        private void DrawPoseOverlay(Rect rect)
        {
            var camera = _preview.camera;
            PreparePreviewCamera(rect);
            var selected = FindSelectedBone();
            var gizmoLocal = _poseTool == PoseTool.Scale || _localSpace;

            if (Event.current.type == EventType.Repaint)
            {
                Handles.BeginGUI();
                for (var i = 0; i < _bonePaths.Count; i++)
                {
                    var bone = FindBone(_bonePaths[i]);
                    if (bone == null || !AnimationClipPreviewGizmo.TryProject(camera, rect, bone.position, out var gui))
                        continue;

                    if (bone.parent != null &&
                        AnimationClipPreviewGizmo.TryProject(camera, rect, bone.parent.position, out var parentGui))
                    {
                        Handles.color = new Color(0.3f, 0.8f, 1f, 0.55f);
                        Handles.DrawAAPolyLine(2f, new Vector3(parentGui.x, parentGui.y, 0f), new Vector3(gui.x, gui.y, 0f));
                    }

                    var isSelected = _bonePaths[i] == _selectedPath;
                    Handles.color = isSelected ? new Color(1f, 0.85f, 0.15f) : new Color(0.45f, 0.9f, 1f, 0.9f);
                    Handles.DrawSolidDisc(gui, Vector3.forward, isSelected ? 4f : 3.2f);
                }

                if (selected != null)
                    Gizmo.Draw(camera, rect, selected, (int)_poseTool, gizmoLocal);
                Handles.EndGUI();
            }

            HandlePoseOverlayInput(rect, camera, selected, gizmoLocal);
        }

        private void HandlePoseOverlayInput(Rect rect, Camera camera, Transform selected, bool gizmoLocal)
        {
            var e = Event.current;
            if (e.alt || e.button == 2)
                return;

            var id = GUIUtility.GetControlID(FocusType.Passive);
            if (selected != null &&
                Gizmo.HandleEvent(e, camera, rect, selected, (int)_poseTool, gizmoLocal, id, out var changed))
            {
                if (changed)
                {
                    _livePose = true;
                    _handleDirty = true;
                    StopPlayback();
                    Repaint();
                }

                if (e.rawType == EventType.MouseUp && _handleDirty && _autoKey)
                {
                    RecordSelectedPose(
                        _poseTool == PoseTool.Move,
                        _poseTool == PoseTool.Rotate,
                        _poseTool == PoseTool.Scale);
                }

                if (e.type == EventType.MouseMove)
                    Repaint();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                var hit = PickBone(camera, rect, e.mousePosition);
                if (hit == null)
                    return;

                SelectBone(hit);
                e.Use();
            }
        }

        private string PickBone(Camera camera, Rect rect, Vector2 mouse)
        {
            string best = null;
            var bestDist = 14f;
            for (var i = 0; i < _bonePaths.Count; i++)
            {
                var bone = FindBone(_bonePaths[i]);
                if (bone == null || !AnimationClipPreviewGizmo.TryProject(camera, rect, bone.position, out var gui))
                    continue;

                var dist = Vector2.Distance(gui, mouse);
                if (dist >= bestDist)
                    continue;

                bestDist = dist;
                best = _bonePaths[i];
            }

            return best;
        }

        private Transform FindBone(string path)
        {
            if (_sampleRoot == null || path == null)
                return null;
            return string.IsNullOrEmpty(path)
                ? _sampleRoot.transform
                : _sampleRoot.transform.Find(path);
        }

        private static string BoneDisplayName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "(root)";
            var slash = path.LastIndexOf('/');
            return slash < 0 ? path : path[(slash + 1)..];
        }

        private static int BoneDepth(string path)
        {
            if (string.IsNullOrEmpty(path))
                return 0;
            var depth = 1;
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                    depth++;
            }

            return depth;
        }

        private void AddEventAtTime(float time)
        {
            var list = new List<AnimationEvent>(_events)
            {
                new() { time = time, functionName = "OnAnimationEvent" }
            };
            _events = list.ToArray();
            ApplyEvents();
        }

        private void RemoveEvent(int index)
        {
            var list = new List<AnimationEvent>(_events);
            list.RemoveAt(index);
            _events = list.ToArray();
            ApplyEvents();
        }

        private void ApplyEvents()
        {
            if (_clip == null)
                return;

            Undo.RecordObject(_clip, "Animation Clip Events");
            AnimationUtility.SetAnimationEvents(_clip, _events);
            EditorUtility.SetDirty(_clip);
            _events = AnimationUtility.GetAnimationEvents(_clip);
        }

        private void DuplicateClip()
        {
            if (_clip == null)
                return;

            var srcPath = AssetDatabase.GetAssetPath(_clip);
            var dir = Path.GetDirectoryName(srcPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || !dir.StartsWith("Assets", StringComparison.Ordinal))
                dir = "Assets";

            var newPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{_clip.name}.anim");
            var clone = new AnimationClip();
            EditorUtility.CopySerialized(_clip, clone);
            AssetDatabase.CreateAsset(clone, newPath);
            AssetDatabase.SaveAssets();
            SetClip(AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath));
            EditorGUIUtility.PingObject(_clip);
        }

        private void EnsurePreview()
        {
            if (_preview != null)
                return;

            _preview = new PreviewRenderUtility();
            _preview.cameraFieldOfView = 30f;
            var cam = _preview.camera;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);
            cam.fieldOfView = 30f;
            _preview.ambientColor = new Color(0.35f, 0.35f, 0.35f, 0f);

            if (_preview.lights is { Length: > 0 })
            {
                _preview.lights[0].enabled = true;
                _preview.lights[0].intensity = 1.3f;
                _preview.lights[0].transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            }

            if (_preview.lights is { Length: > 1 })
            {
                _preview.lights[1].enabled = true;
                _preview.lights[1].intensity = 0.7f;
                _preview.lights[1].transform.rotation = Quaternion.Euler(340f, 170f, 0f);
            }
        }

        private void RebuildInstance()
        {
            DestroyInstance();
            if (_preview == null || _model == null)
                return;

            _instance = CreateInstanceFromModel(_model);
            if (_instance == null)
                return;

            PrepareInstance(_instance);
            _sampleRoot = ResolveSampleRoot(_instance, _clip);
            SamplePose();
            if (_frameCamera)
                FrameCamera();
            RefreshBonePaths();
            RefreshMissingBindings();
        }

        private GameObject CreateInstanceFromModel(GameObject source)
        {
            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(source);
            if (string.IsNullOrEmpty(assetPath) && AssetDatabase.Contains(source))
                assetPath = AssetDatabase.GetAssetPath(source);

            if (!string.IsNullOrEmpty(assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset != null)
                    return _preview.InstantiatePrefabInScene(asset);
            }

            var instance = UnityEngine.Object.Instantiate(source);
            instance.hideFlags = HideFlags.HideAndDontSave;
            _preview.AddSingleGO(instance);
            return instance;
        }

        private static void PrepareInstance(GameObject instance)
        {
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            instance.hideFlags = HideFlags.HideAndDontSave;

            var behaviours = instance.GetComponentsInChildren<Behaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
                behaviours[i].enabled = false;

            var bodies = instance.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = true;
                bodies[i].detectCollisions = false;
            }
        }

        private static GameObject ResolveSampleRoot(GameObject instance, AnimationClip clip)
        {
            if (clip != null && clip.humanMotion)
            {
                var animator = instance.GetComponentInChildren<Animator>(true);
                return animator != null ? animator.gameObject : instance;
            }

            var bindings = clip != null ? AnimationUtility.GetCurveBindings(clip) : Array.Empty<EditorCurveBinding>();
            for (var i = 0; i < bindings.Length; i++)
            {
                var path = bindings[i].path;
                if (string.IsNullOrEmpty(path))
                    continue;

                var first = path.Split('/')[0];
                if (instance.transform.Find(first) != null)
                    return instance;

                var transforms = instance.GetComponentsInChildren<Transform>(true);
                for (var t = 0; t < transforms.Length; t++)
                {
                    if (transforms[t].Find(first) != null)
                        return transforms[t].gameObject;
                }

                break;
            }

            var fallbackAnimator = instance.GetComponentInChildren<Animator>(true);
            return fallbackAnimator != null ? fallbackAnimator.gameObject : instance;
        }

        private void SamplePose()
        {
            if (_clip == null || _sampleRoot == null || EditorApplication.isPlaying)
                return;

            EnsureAnimationMode();
            AnimationMode.BeginSampling();
            try
            {
                AnimationMode.SampleAnimationClip(_sampleRoot, _clip, _time);
            }
            finally
            {
                AnimationMode.EndSampling();
            }

            if (_lockRoot && _instance != null)
                _instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void EnsureAnimationMode()
        {
            if (_driver == null)
            {
                _driver = CreateInstance<AnimationModeDriver>();
                _driver.hideFlags = HideFlags.HideAndDontSave;
            }

            if (!AnimationMode.InAnimationMode(_driver))
                AnimationMode.StartAnimationMode(_driver);
        }

        private void FrameCamera()
        {
            if (_instance == null)
                return;

            var bounds = ComputeBounds(_instance);
            _pivot = bounds.center;
            _distance = Mathf.Clamp(bounds.extents.magnitude * 2.4f, 0.6f, 20f);
            _frameCamera = false;
        }

        private void PreparePreviewCamera(Rect rect)
        {
            if (_preview == null)
                return;

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var cam = _preview.camera;
            cam.transform.position = _pivot + rotation * (Vector3.back * _distance);
            cam.transform.rotation = rotation;
            cam.nearClipPlane = Mathf.Max(0.01f, _distance * 0.01f);
            cam.farClipPlane = Mathf.Max(50f, _distance * 20f);
            cam.fieldOfView = Mathf.Max(1f, _preview.cameraFieldOfView);
            cam.aspect = Mathf.Max(1f, rect.width) / Mathf.Max(1f, rect.height);
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                if (renderers[i].bounds.size.sqrMagnitude <= 0f)
                    continue;
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private void DestroyInstance()
        {
            _sampleRoot = null;
            if (_instance == null)
                return;

            UnityEngine.Object.DestroyImmediate(_instance);
            _instance = null;
        }

        private void DisposePreview()
        {
            if (_driver != null && AnimationMode.InAnimationMode(_driver))
                AnimationMode.StopAnimationMode(_driver);

            if (_driver != null)
            {
                UnityEngine.Object.DestroyImmediate(_driver);
                _driver = null;
            }

            _instance = null;
            _sampleRoot = null;
            if (_preview == null)
                return;

            _preview.Cleanup();
            _preview = null;
        }

        private void SaveModelPref(GameObject model)
        {
            if (model == null)
                return;

            var path = AssetDatabase.GetAssetPath(model);
            if (string.IsNullOrEmpty(path))
                path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model);
            if (string.IsNullOrEmpty(path))
                return;

            var guid = AssetDatabase.AssetPathToGUID(path);
            EditorPrefs.SetString(PrefLastModel, guid);
            if (_clip != null)
                EditorPrefs.SetString(PrefModelPrefix + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_clip)), guid);
        }

        private static GameObject FindModelForClip(AnimationClip clip)
        {
            if (clip == null)
                return null;

            var clipPath = AssetDatabase.GetAssetPath(clip);
            var saved = EditorPrefs.GetString(PrefModelPrefix + AssetDatabase.AssetPathToGUID(clipPath), string.Empty);
            var savedModel = LoadRenderable(saved);
            if (savedModel != null)
                return savedModel;

            if (AssetDatabase.LoadMainAssetAtPath(clipPath) is GameObject embedded && IsRenderable(embedded))
                return embedded;

            var guess = GuessModelSearchName(clip.name);
            var candidates = new List<GameObject>();
            CollectPrefabs(guess, candidates);
            if (candidates.Count == 0 && guess.Contains('_'))
                CollectPrefabs(guess.Replace('_', ' '), candidates);

            var best = PickBestModel(candidates, clip.name);
            if (best != null)
                return best;

            return LoadRenderable(EditorPrefs.GetString(PrefLastModel, string.Empty));
        }

        private static void CollectPrefabs(string filter, List<GameObject> results)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return;

            var guids = AssetDatabase.FindAssets($"t:Prefab {filter}");
            for (var i = 0; i < guids.Length; i++)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (IsRenderable(go))
                    results.Add(go);
            }
        }

        private static GameObject PickBestModel(List<GameObject> candidates, string clipName)
        {
            if (candidates.Count == 0)
                return null;

            var guess = NormalizeName(GuessModelSearchName(clipName));
            GameObject prefabFolderMatch = null;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (NormalizeName(candidate.name) == guess)
                    return candidate;

                var path = AssetDatabase.GetAssetPath(candidate);
                if (prefabFolderMatch == null && path.Contains("/Prefab/", StringComparison.Ordinal))
                    prefabFolderMatch = candidate;
            }

            return prefabFolderMatch != null ? prefabFolderMatch : candidates[0];
        }

        private static string GuessModelSearchName(string clipName)
        {
            var name = clipName;
            for (var i = 0; i < ClipSuffixes.Length; i++)
            {
                var suffix = ClipSuffixes[i];
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return name[..^suffix.Length];
            }

            return name;
        }

        private static string NormalizeName(string value)
        {
            return value.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        }

        private static GameObject LoadRenderable(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            return IsRenderable(go) ? go : null;
        }

        private static bool IsRenderable(GameObject go)
        {
            return go != null && go.GetComponentInChildren<Renderer>(true) != null;
        }

        private static bool IsModelSubAsset(AnimationClip clip)
        {
            if (clip == null)
                return false;
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) is ModelImporter;
        }

        private static void DrawCenteredLabel(Rect rect, string text)
        {
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                wordWrap = true,
                fontSize = 12
            };
            GUI.Label(rect, text, style);
        }
    }
}
