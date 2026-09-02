using Backend.Object.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DdaIT
{
    public class OptionPopup : UIPopup<OptionPopupPresenter>
    {
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _bgmVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private CommonButton _closeButton;

        public TMP_Dropdown ResolutionDropdown => _resolutionDropdown;
        public Slider MasterVolumeSlider => _masterVolumeSlider;
        public Slider BgmVolumeSlider => _bgmVolumeSlider;
        public Slider SfxVolumeSlider => _sfxVolumeSlider;
        public CommonButton CloseButton => _closeButton;
    }
}
