using UnityEngine;
using TMPro;

namespace DdaIT.Object.UI
{
    public class SingleGaugeBar : CommonGaugeBar
    {
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _maxValueText;
        protected override void ApplyFill(float ratio)
        {
            _fill.fillAmount = ratio;
        }

        public void SetText(string valueText, string maxValueText){
            _valueText.text = valueText;
            _maxValueText.text = maxValueText;
        }
    }
}
