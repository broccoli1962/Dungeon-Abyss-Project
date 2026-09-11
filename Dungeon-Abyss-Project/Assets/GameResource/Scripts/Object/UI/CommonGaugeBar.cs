using UnityEngine;
using UnityEngine.UI;
using DdaIT.Util;

namespace DdaIT.Object.UI
{
    /// <summary>
    /// 모든 GaugeBar 의 공통 베이스. 회색 Background 색상과 Background Image 적용을 책임진다.
    /// 실제 Fill 갱신 형식은 자식 클래스 SingleGaugeBar 가 담당한다.
    /// </summary>
    public abstract class CommonGaugeBar : CachedMonobehaviour
    {
        public static readonly Color BackgroundColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("Gauge")]
        [SerializeField] protected Image _background;
        [SerializeField] protected Image _fill;

        protected virtual void Awake()
        {
            if (_background != null)
                _background.color = BackgroundColor;
        }

        public void SetRatio(float ratio){
            ApplyFill(Mathf.Clamp01(ratio));
        }

        protected abstract void ApplyFill(float ratio);
    }
}
