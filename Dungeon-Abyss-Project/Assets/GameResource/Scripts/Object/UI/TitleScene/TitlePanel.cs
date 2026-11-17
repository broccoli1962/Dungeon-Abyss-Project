using DdaIT.Object.UI;
using UnityEngine;

namespace DdaIT.Object.UI
{
    public class TitlePanel : UIPanel<TitlePanelPresenter>
    {
        [SerializeField] private CommonButton _gameHostButton;
        [SerializeField] private CommonButton _gameClientButton;
        [SerializeField] private CommonButton _gameOptionButton;
        [SerializeField] private CommonButton _gameExitButton;

        public CommonButton GameHostButton { get => _gameHostButton; }
        public CommonButton GameClientButton { get => _gameClientButton; }
        public CommonButton GameOptionButton { get => _gameOptionButton; }
        public CommonButton GameExitButton { get => _gameExitButton; }
    }
}
