using System.Collections.Generic;
using DdaIT.Object.Management;
using DdaIT.Object.UI;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DdaIT
{
    public class OptionPopupPresenter : UIPresenter<OptionPopup>
    {
        private readonly CompositeDisposable _disposables = new();
        private List<Resolution> _resolutions;

        public override void OnOpen()
        {
            base.OnOpen();

            BindResolution();
            BindFullScreen();
            BindVolumes();

            View.CloseButton.OnClickAsObservable()
                .Subscribe(_ => UIManager.Close(View))
                .AddTo(_disposables);
        }

        public override void OnClose()
        {
            PlayerPrefs.Save();
            _disposables.Clear();
            base.OnClose();
        }

        private void BindResolution()
        {
            _resolutions = DisplaySettings.GetUniqueResolutions();
            var dropdown = View.ResolutionDropdown;
            dropdown.ClearOptions();

            var options = new List<TMP_Dropdown.OptionData>(_resolutions.Count);
            for (int i = 0; i < _resolutions.Count; i++)
            {
                var res = _resolutions[i];
                options.Add(new TMP_Dropdown.OptionData($"{res.width} x {res.height}"));
            }

            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(DisplaySettings.FindCurrentIndex(_resolutions));

            Observable.FromEvent<UnityAction<int>, int>(
                    h => new UnityAction<int>(h),
                    h => dropdown.onValueChanged.AddListener(h),
                    h => dropdown.onValueChanged.RemoveListener(h))
                .Subscribe(OnResolutionChanged)
                .AddTo(_disposables);
        }

        private void BindFullScreen()
        {
            var toggle = View.FullScreenToggle;
            toggle.SetIsOnWithoutNotify(DisplaySettings.IsFullScreen);

            Observable.FromEvent<UnityAction<bool>, bool>(
                    h => new UnityAction<bool>(h),
                    h => toggle.onValueChanged.AddListener(h),
                    h => toggle.onValueChanged.RemoveListener(h))
                .Subscribe(DisplaySettings.SetFullScreen)
                .AddTo(_disposables);
        }

        private void BindVolumes()
        {
            BindSlider(View.MasterVolumeSlider, AudioManager.GetMasterVolume(), AudioManager.SetMasterVolume);
            BindSlider(View.BgmVolumeSlider, AudioManager.GetBgmVolume(), AudioManager.SetBgmVolume);
            BindSlider(View.SfxVolumeSlider, AudioManager.GetSfxVolume(), AudioManager.SetSfxVolume);
            BindSlider(View.VoiceVolumeSlider, AudioManager.GetVoiceVolume(), AudioManager.SetVoiceVolume);
        }

        private void BindSlider(Slider slider, float value, System.Action<float> setter)
        {
            slider.SetValueWithoutNotify(value);
            Observable.FromEvent<UnityAction<float>, float>(
                    h => new UnityAction<float>(h),
                    h => slider.onValueChanged.AddListener(h),
                    h => slider.onValueChanged.RemoveListener(h))
                .Subscribe(setter)
                .AddTo(_disposables);
        }

        private void OnResolutionChanged(int index)
        {
            DisplaySettings.ApplyIndex(_resolutions, index);
        }
    }
}
