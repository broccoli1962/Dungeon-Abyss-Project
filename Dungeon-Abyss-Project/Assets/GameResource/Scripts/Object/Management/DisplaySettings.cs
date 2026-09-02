using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    /// <summary>
    /// 해상도 설정을 PlayerPrefs 에 저장하고 <see cref="Screen.SetResolution"/> 으로 적용한다.
    /// </summary>
    public static class DisplaySettings
    {
        private const string PREF_WIDTH = "DisplaySettings_Width";
        private const string PREF_HEIGHT = "DisplaySettings_Height";
        private const string PREF_REFRESH_NUM = "DisplaySettings_RefreshNumerator";
        private const string PREF_REFRESH_DEN = "DisplaySettings_RefreshDenominator";

        /// <summary>
        /// 모니터가 지원하는 해상도를 가로×세로 기준으로 중복 제거해 반환한다.
        /// 같은 해상도는 가장 높은 주사율을 남긴다.
        /// </summary>
        public static List<Resolution> GetUniqueResolutions()
        {
            var unique = new Dictionary<(int, int), Resolution>();
            var all = Screen.resolutions;
            for (int i = 0; i < all.Length; i++)
            {
                var res = all[i];
                var key = (res.width, res.height);
                if (!unique.TryGetValue(key, out var existing)
                    || res.refreshRateRatio.value > existing.refreshRateRatio.value)
                {
                    unique[key] = res;
                }
            }

            if (unique.Count == 0)
            {
                unique[(Screen.width, Screen.height)] = new Resolution
                {
                    width = Screen.width,
                    height = Screen.height,
                    refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 },
                };
            }

            var list = new List<Resolution>(unique.Values);
            list.Sort(CompareResolution);
            return list;
        }

        /// <summary>
        /// 저장된(또는 현재) 해상도가 목록에서 몇 번째인지 반환한다.
        /// </summary>
        public static int FindCurrentIndex(IReadOnlyList<Resolution> resolutions)
        {
            if (resolutions == null || resolutions.Count == 0)
                return 0;

            int width = PlayerPrefs.GetInt(PREF_WIDTH, Screen.width);
            int height = PlayerPrefs.GetInt(PREF_HEIGHT, Screen.height);
            for (int i = 0; i < resolutions.Count; i++)
            {
                if (resolutions[i].width == width && resolutions[i].height == height)
                    return i;
            }

            for (int i = 0; i < resolutions.Count; i++)
            {
                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                    return i;
            }

            return resolutions.Count - 1;
        }

        /// <summary>
        /// 목록의 인덱스로 해상도를 적용하고 저장한다.
        /// </summary>
        public static void ApplyIndex(IReadOnlyList<Resolution> resolutions, int index)
        {
            if (resolutions == null || index < 0 || index >= resolutions.Count)
                return;

            ApplyResolution(resolutions[index]);
        }

        /// <summary>
        /// 부트 시 저장된 해상도를 복원한다. 저장된 값이 없으면 아무 것도 하지 않는다.
        /// </summary>
        public static void ApplySaved()
        {
            if (!PlayerPrefs.HasKey(PREF_WIDTH) || !PlayerPrefs.HasKey(PREF_HEIGHT))
                return;

            int width = PlayerPrefs.GetInt(PREF_WIDTH);
            int height = PlayerPrefs.GetInt(PREF_HEIGHT);
            uint numerator = (uint)Mathf.Max(1, PlayerPrefs.GetInt(PREF_REFRESH_NUM, 60));
            uint denominator = (uint)Mathf.Max(1, PlayerPrefs.GetInt(PREF_REFRESH_DEN, 1));

            Screen.SetResolution(
                width,
                height,
                Screen.fullScreenMode,
                new RefreshRate { numerator = numerator, denominator = denominator });
        }

        private static void ApplyResolution(Resolution resolution)
        {
            PlayerPrefs.SetInt(PREF_WIDTH, resolution.width);
            PlayerPrefs.SetInt(PREF_HEIGHT, resolution.height);
            PlayerPrefs.SetInt(PREF_REFRESH_NUM, (int)resolution.refreshRateRatio.numerator);
            PlayerPrefs.SetInt(PREF_REFRESH_DEN, (int)Mathf.Max(1, resolution.refreshRateRatio.denominator));

            Screen.SetResolution(
                resolution.width,
                resolution.height,
                Screen.fullScreenMode,
                resolution.refreshRateRatio);
        }

        private static int CompareResolution(Resolution a, Resolution b)
        {
            int widthCompare = a.width.CompareTo(b.width);
            return widthCompare != 0 ? widthCompare : a.height.CompareTo(b.height);
        }
    }
}
