using UnityEngine;
using DdaIT.Util;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using R3;
using System;

namespace DdaIT.Object.UI
{
    public class InventorySlotView : CachedMonobehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _itemCountText;

        public int Index { get; private set;}
        public Observable<int> Clicked => _clicked;
        private readonly Subject<int> _clicked = new();

        public void Init(int index) => Index = index;
        public void OnPointerClick(PointerEventData eventData) => _clicked.OnNext(Index);

        public void RenderInventoryItem(Sprite itemIcon, int count){
            _itemIcon.sprite = itemIcon;
            _itemIcon.enabled = itemIcon != null;
            _itemCountText.text = count > 1 ? count.ToString() : string.Empty;
        }
    }
}
