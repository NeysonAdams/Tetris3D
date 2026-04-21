using System;
using Tetris.Core.Tetrominoes;
using Tetris.Presentation.Configs;
using Tetris.Presentation.Views;
using UnityEngine;
using UnityEngine.Pool;


namespace Tetris.Presentation.Pooling
{
    public sealed class BlockPool
    {
        private readonly BlockView _prefab;
        private readonly Transform _container;
        private readonly DOTweenAnimationSettingsSO _animationSettings;
        private readonly Material _ghostMaterial;
        private readonly ObjectPool<BlockView> _pool;

        public Transform Container => _container;

        public BlockPool (
            BlockView prefab,
             Transform container,
             DOTweenAnimationSettingsSO animationSettings,
             Material ghostMaterial = null,
             int defaultCapacity = 64,
             int maxSize = 512)
        {
            if(prefab == null) throw new ArgumentNullException(nameof(prefab));
            if(container == null) throw new ArgumentNullException(nameof (container));
            if(animationSettings == null) throw new ArgumentNullException(nameof(animationSettings));

            _ghostMaterial = ghostMaterial;

            if(defaultCapacity <1) throw new ArgumentOutOfRangeException(nameof(defaultCapacity));
            if(maxSize < defaultCapacity) throw new ArgumentOutOfRangeException(nameof(maxSize));

            _prefab = prefab;
            _container = container;
            _animationSettings = animationSettings;

            _pool = new ObjectPool<BlockView>(
                createFunc: CreatePooled,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyPooled, 
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public BlockView Rent(TetrominoType type, Color color, int layer = 0)
        {
            var view = _pool.Get();
            view.Bind(type, color, layer);
            return view;
        }

        public void Return (BlockView view)
        {
            if(view == null) throw new ArgumentNullException(nameof(view));
            _pool.Release(view);
        }

        public void Warmup (int count)
        {
            if (count <= 0) return;
            var buffer = new BlockView[count];
            for(int i = 0; i < count; i++)
            {
                buffer[i] = _pool.Get();
            }
            for(int i = 0; i< count; i++)
            {
                _pool.Release(buffer[i]);
            }
        }

        public void Clear()
        {
            _pool.Clear();
        }

        private BlockView CreatePooled()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _container);
            instance.Initialize(_animationSettings, _ghostMaterial);
            instance.gameObject.SetActive(false);
            return instance;
        }
        private void OnGet(BlockView view) 
        {
            view.gameObject.SetActive(true);
        }
        private void OnRelease(BlockView view) 
        {
            view.OnDespawn(_container);
            view.gameObject.SetActive(false);
        }
        private void OnDestroyPooled(BlockView view) 
        { 
            if(view != null)
                UnityEngine.Object.Destroy(view.gameObject);
        }
    }
}
