using System.Collections.Generic;
using Tetris.Core.Configs;
using Tetris.Core.Events;
using Tetris.Core.Presentation;
using Tetris.Core.Tetrominoes;
using Tetris.Presentation.Configs;
using Tetris.Presentation.Pooling;
using Tetris.Presentation.Views;
using UnityEngine;

namespace Tetris.Presentation.Preview
{
    public sealed class PiecePreviewRenderer : MonoBehaviour, IPiecePreviewRenderer
    {
        private const int PreviewLayer = 8;

        private GameEvents _events = default!;
        private BlockPool _pool = default!;
        private PreviewRendererSettingsSO _settings = default!;
        private FieldLayoutSettingsSO _layout = default!;

        private RenderTexture _renderTexture = default!;
        private Camera _previewCamera = default!;
        private Transform _stage = default!;
        private Transform _pieceRoot = default!;
        private readonly List<BlockView> _children = new();

        private bool _isInitialized;

        public Texture PreviewTexture => _renderTexture;

        public void Initialize(
            GameEvents events,
            BlockPool pool,
            PreviewRendererSettingsSO settings,
            FieldLayoutSettingsSO layout)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("PiecePreviewRenderer already initialized.");
            }

            if (events == null) throw new System.ArgumentNullException(nameof(events));
            if (pool == null) throw new System.ArgumentNullException(nameof(pool));
            if (settings == null) throw new System.ArgumentNullException(nameof(settings));
            if (layout == null) throw new System.ArgumentNullException(nameof(layout));

            _events = events;
            _pool = pool;
            _settings = settings;
            _layout = layout;

            CreateRenderTexture();
            CreateStage();
            CreateCamera();
            CreateLight();

            _events.NextPieceChanged += HandleNextPieceChanged;

            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                _events.NextPieceChanged -= HandleNextPieceChanged;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }

        private void Update()
        {
            if (!_isInitialized || !_settings.AutoRotate || _pieceRoot == null) return;

            _pieceRoot.Rotate(_settings.AutoRotateSpeed * Time.deltaTime, Space.Self);
        }

        public void SetPreview(TetrominoShapeSo shape)
        {
            if (!_isInitialized)
            {
                return;
            }

            ReturnAllChildren();

            if (shape == null)
            {
                return;
            }

            SpawnPreviewBlocks(shape);
        }

        private void HandleNextPieceChanged(TetrominoShapeSo shape)
        {
            SetPreview(shape);
        }

        private void CreateRenderTexture()
        {
            _renderTexture = new RenderTexture(_settings.TextureWidth, _settings.TextureHeight, 16)
            {
                name = "PiecePreview_RT",
                antiAliasing = 2
            };
            _renderTexture.Create();
        }

        private void CreateStage()
        {
            var stageGO = new GameObject("PreviewStage");
            stageGO.transform.position = _settings.StagePositon;
            stageGO.layer = PreviewLayer;
            _stage = stageGO.transform;

            var pieceRootGO = new GameObject("PieceRoot");
            pieceRootGO.transform.SetParent(_stage, worldPositionStays: false);
            pieceRootGO.layer = PreviewLayer;
            _pieceRoot = pieceRootGO.transform;
        }

        private void CreateCamera()
        {
            var camGO = new GameObject("PreviewCamera");
            camGO.transform.SetParent(_stage, worldPositionStays: false);
            camGO.transform.localPosition = new Vector3(0f, 0f, -_settings.CameraDistance);
            camGO.transform.LookAt(_stage);

            _previewCamera = camGO.AddComponent<Camera>();
            _previewCamera.fieldOfView = _settings.CameraFieldOfView;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = _settings.BackGroundColor;
            _previewCamera.cullingMask = 1 << PreviewLayer;
            _previewCamera.targetTexture = _renderTexture;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 50f;
        }

        private void CreateLight()
        {
            var lightGO = new GameObject("PreviewLight");
            lightGO.transform.SetParent(_stage, worldPositionStays: false);
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.cullingMask = 1 << PreviewLayer;
        }

        private void SpawnPreviewBlocks(TetrominoShapeSo shape)
        {
            var cells = shape.Cells;
            var color = shape.DefaultColor;
            var type = shape.Type;
            var blockSize = _layout.BlockSize;

            var center = ComputeShapeCenter(cells);

            for (var i = 0; i < cells.Length; i++)
            {
                var view = _pool.Rent(type, color, PreviewLayer);
                view.transform.SetParent(_pieceRoot, worldPositionStays: false);

                var offset = new Vector3(cells[i].x - center.x, cells[i].y - center.y, cells[i].z - center.z);
                view.transform.localPosition = offset * blockSize;
                view.transform.localRotation = Quaternion.identity;

                _children.Add(view);
            }

            _pieceRoot.localRotation = Quaternion.identity;
        }

        private void ReturnAllChildren()
        {
            for (var i = 0; i < _children.Count; i++)
            {
                _pool.Return(_children[i]);
            }
            _children.Clear();
        }

        private static Vector3 ComputeShapeCenter(Vector3Int[] cells)
        {
            if (cells.Length == 0) return Vector3.zero;

            var sum = Vector3.zero;
            for (var i = 0; i < cells.Length; i++)
            {
                sum += new Vector3(cells[i].x, cells[i].y, cells[i].z);
            }
            return sum / cells.Length;
        }
    }
}