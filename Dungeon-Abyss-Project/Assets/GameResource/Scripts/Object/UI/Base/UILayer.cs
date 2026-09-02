namespace Backend.Object.UI
{
    /// <summary>
    /// UI 가 배치될 Canvas 레이어 구분 (위로 갈수록 상위 렌더).
    /// - HUD: 인게임 상시 UI
    /// - Panel: 메인 화면/메뉴 패널
    /// - Navigation: NavBar 등 Panel 위 / Popup 아래에 상주하는 UI
    /// - Popup: 모달 팝업
    /// - System: 스플래시/로딩 커튼 등 AppFlow 가 소유하는 최상위 UI. CloseAllUI 대상에서 제외된다.
    /// </summary>
    public enum UILayer
    {
        HUD,
        Panel,
        Navigation,
        Popup,
        System,
    }
}
