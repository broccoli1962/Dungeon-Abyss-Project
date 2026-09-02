using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 씬 전환용 풀스크린 로딩 커튼. System 레이어에 올라가며 뒤로가기로 닫히지 않는다.
    /// AppFlow 가 인스턴스를 직접 들고 FadeInAsync / SetProgress / FadeOutAsync 를 호출한다.
    /// </summary>
    public class LoadingPanel : UIPanel
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private float _progressLerpSpeed = 4f;

        private float _targetProgress;

        public override UILayer Layer => UILayer.System;

        /// <summary>백 스택에 올려 로딩 중 ESC/뒤로가기를 삼킨다.</summary>
        protected override bool DefaultHandleBackButton => true;

        public override bool OnBackPressed() => false;

        protected override void OnOpen()
        {
            base.OnOpen();

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            _targetProgress = 0f;
            if (_progressFill != null)
                _progressFill.fillAmount = 0f;
        }

        private void Update()
        {
            if (_progressFill == null) return;

            _progressFill.fillAmount = Mathf.MoveTowards(
                _progressFill.fillAmount,
                _targetProgress,
                _progressLerpSpeed * Time.deltaTime);
        }

        public void SetMessage(string message)
        {
            if (_messageText != null)
                _messageText.text = message ?? string.Empty;
        }

        /// <summary>진행률 목표값을 설정한다. 실제 표시값은 Update 에서 부드럽게 lerp 된다.</summary>
        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);
        }

        /// <summary>커튼을 알파 0 → 1 로 페이드인한다. CanvasGroup 이 없으면 즉시 완료한다.</summary>
        public UniTask FadeInAsync()
        {
            if (_canvasGroup == null)
                return UniTask.CompletedTask;

            return LMotion.Create(0f, 1f, _fadeInDuration)
                .BindToAlpha(_canvasGroup)
                .ToUniTask();
        }

        /// <summary>커튼을 알파 1 → 0 으로 페이드아웃한다. CanvasGroup 이 없으면 즉시 완료한다.</summary>
        public UniTask FadeOutAsync()
        {
            if (_canvasGroup == null)
                return UniTask.CompletedTask;

            return LMotion.Create(1f, 0f, _fadeOutDuration)
                .BindToAlpha(_canvasGroup)
                .ToUniTask();
        }
    }
}
