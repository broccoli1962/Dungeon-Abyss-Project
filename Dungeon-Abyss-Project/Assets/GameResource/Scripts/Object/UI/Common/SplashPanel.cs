using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 앱 최초 진입 스플래시. AppFlow 가 인스턴스를 직접 들고 로고 페이드인 / 전체 페이드아웃을 구동한다.
    /// System 레이어에 올라가며 뒤로가기로 닫히지 않는다.
    /// </summary>
    public class SplashPanel : UIPanel
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _logo;
        [SerializeField] private float _logoFadeInDuration = 1f;
        [SerializeField] private float _fadeOutDuration = 0.5f;

        public override UILayer Layer => UILayer.System;

        /// <summary>백 스택에 올려 스플래시 중 ESC/뒤로가기를 삼킨다.</summary>
        protected override bool DefaultHandleBackButton => true;

        public override bool OnBackPressed() => false;

        protected override void OnOpen()
        {
            base.OnOpen();

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            if (_logo != null)
            {
                var color = _logo.color;
                color.a = 0f;
                _logo.color = color;
            }
        }

        /// <summary>로고 알파를 0 → 1 로 페이드인한다. 로고가 없으면 즉시 완료한다.</summary>
        public UniTask FadeLogoInAsync()
        {
            if (_logo == null)
                return UniTask.CompletedTask;

            return LMotion.Create(0f, 1f, _logoFadeInDuration)
                .BindToColorA(_logo)
                .ToUniTask();
        }

        /// <summary>패널 전체를 알파 1 → 0 으로 페이드아웃한다. CanvasGroup 이 없으면 즉시 완료한다.</summary>
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
