using System;
using System.Collections.Generic;
using DdaIT.Util.Interface;
using R3;
using UnityEngine;

namespace DdaIT.Util.Input
{
    public sealed class UiPointerBlockerService : IDisposable
    {
        private readonly HashSet<IUiPointerBlocker> _blockers = new();
        private readonly Subject<Unit> _changed = new();

        private bool _disposed;

        public Observable<Unit> Changed => _changed;

        public void Register(IUiPointerBlocker blocker)
        {
            if (_disposed || blocker == null)
                return;

            if (_blockers.Add(blocker))
            {
                _changed.OnNext(Unit.Default);
            }
        }

        public void Unregister(IUiPointerBlocker blocker)
        {
            if (_disposed || blocker == null)
                return;

            if (_blockers.Remove(blocker))
            {
                _changed.OnNext(Unit.Default);
            }
        }

        //이미 등록된게 있는지 확인
        public bool IsPointerBlocked(Vector2 screenPos){
            if(_disposed)
                return false;

            foreach(var blocker in _blockers){
                if(blocker.BlockPointer(screenPos))
                    return true;
            }

            return false;
        }

        public void NotifyChanged(){
            if(!_disposed)
                _changed.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            if(_disposed)
                return;

            _disposed = true;
            _blockers.Clear();
            _changed.Dispose();
        }
    }
}