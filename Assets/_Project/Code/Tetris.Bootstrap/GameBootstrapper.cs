using DG.Tweening;
using System;
using System.Collections.Generic;
using Tetris.Core.Commands;
using Tetris.Core.Configs;
using Tetris.Core.Events;
using Tetris.Core.Fields;
using Tetris.Core.Randomization;
using Tetris.Core.Scoring;
using Tetris.Core.Spawning;
using Tetris.Core.StateMachine;
using Tetris.Core.StateMachine.States;
using Tetris.Input;
using Tetris.Input.Configs;
using Tetris.Presentation.Configs;
using Tetris.Presentation.Pause;
using Tetris.Presentation.Pooling;
using Tetris.Presentation.Preview;
using Tetris.Presentation.Views;
using Tetris.UI;
using Tetris.UI.Configs;
using Tetris.UI.Controllers;
using Tetris.UI.Displays;
using Tetris.UI.Panels;
using UnityEngine;


namespace Tetris.Bootstrap
{
    public class GameBootstrapper : MonoBehaviour
    {
        // ============================================
        // SerializeField � Core configs
        // ============================================

        [Header("Core � Configs")]
        [SerializeField] private GameFieldSettingsSO _fieldSettings = default!;
        [SerializeField] private TetrominoSetSO _tetrominoSet = default!;
        [SerializeField] private ScoreSettingsSO _scoreSettings = default!;
        [SerializeField] private GravitySettingsSO _gravitySettings = default!;
        [SerializeField] private SpawnSettingsSO _spawnSettings = default!;

        // ============================================
        // Input configs
        // ============================================

        [Header("Input � Configs")]
        [SerializeField] private KeyBindingsSO _keyBindings = default!;
        [SerializeField] private KeyRepeatSettingsSO _keyRepeatSettings = default!;

        // ============================================
        // Presentation configs
        // ============================================

        [Header("Presentation � Configs")]
        [SerializeField] private FieldLayoutSettingsSO _fieldLayout = default!;
        [SerializeField] private DOTweenAnimationSettingsSO _animationSettings = default!;
        [SerializeField] private CameraSettingsSO _cameraSettings = default!;
        [SerializeField] private PreviewRendererSettingsSO _previewSettings = default!;
        [SerializeField] private BlockVisualConfigSO _blockVisualConfig = default!;

        // ============================================
        // UI configs
        // ============================================

        [Header("UI � Configs")]
        [SerializeField] private UIAnimationSettingsSO _uiAnimationSettings = default!;

        // ============================================
        // Materials
        // ============================================

        [Header("Materials")]
        [SerializeField] private Material _ghostMaterial = default!;

        // ============================================
        // Prefabs
        // ============================================

        [Header("Prefabs")]
        [SerializeField] private BlockView _blockPrefab = default!;

        // ============================================
        // Scene refs � Presentation
        // ============================================

        [Header("Scene � Presentation")]
        [SerializeField] private Transform _blockPoolContainer = default!;
        [SerializeField] private FieldView _fieldView = default!;
        [SerializeField] private PieceView _pieceView = default!;
        [SerializeField] private GhostView _ghostView = default!;
        [SerializeField] private ArenaView _arenaView = default!;
        [SerializeField] private OrbitalCamera _orbitalCamera = default!;
        [SerializeField] private PiecePreviewRenderer _previewRenderer = default!;
        [SerializeField] private PresentationPauseController _pauseController = default!;

        // ============================================
        // Scene refs � UI
        // ============================================

        [Header("Scene � UI")]
        [SerializeField] private ScreenManager _screenManager = default!;
        [SerializeField] private MainMenuController _mainMenuController = default!;
        [SerializeField] private HUDController _hudController = default!;
        [SerializeField] private PauseController _uiPauseController = default!;
        [SerializeField] private GameOverController _gameOverController = default!;
        [SerializeField] private ScoreDisplay _scoreDisplay = default!;
        [SerializeField] private LevelDisplay _levelDisplay = default!;
        [SerializeField] private NextPiecePanel _nextPiecePanel = default!;

        // ============================================
        // Runtime references
        // ============================================

        private GameEvents _events = default!;
        private GameField _field = default!;
        private ITetrominoCatalog _catalog = default!;
        private IRandomizer _randomizer = default!;
        private IScoringService _scoringService = default!;
        private ISpawner _spawner = default!;
        private GameContext _gameContext = default!;
        private GameStateMachine _stateMachine = default!;
        private CommandDispatcher _commandDispatcher = default!;
        private BlockPool _blockPool = default!;
        private KeyboardInputSource _inputSource = default!;

        // ============================================
        // Unity lifecycle
        // ============================================

        private void Awake()
        {
            InitializeDOTween();
            InitializeCoreServices();
            InitializeGameContext();
            InitializeStateMachine();
            InitializeCommandDispatcher();
            InitializeBlockPool();
            InitializePresentation();
            InitializeUI();
            InitializeInput();
        }

        private void Update()
        {
            _inputSource.Tick(Time.deltaTime);
            _stateMachine.Tick(Time.deltaTime);
        }

        private void OnApplicationQuit()
        {
            DOTween.KillAll();
            if(_blockPool != null) _blockPool.Clear();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _inputSource != null)
            {
                _inputSource.OnApplicationFocusLost();
            }
        }

        // ============================================
        // Stage methods (placeholders)
        // ============================================

        private void InitializeDOTween() 
        {
            DOTween.Init(
                useSafeMode: true,
                recycleAllByDefault: false,
                logBehaviour: LogBehaviour.ErrorsOnly);

            DOTween.SetTweensCapacity(500, 100);
        }
        private void InitializeCoreServices() 
        {
            _events = new GameEvents();

            _field = new GameField(_fieldSettings);

            _catalog = new TetrominoCatalog(_tetrominoSet);

            var randomProvider = new UnityRandomProvider();
            _randomizer = new BagRandomizer(_tetrominoSet, randomProvider);

            _scoringService = new ScoringService(_scoreSettings);

            _spawner = new Spawner(_catalog, _spawnSettings);
        }
        private void InitializeGameContext() 
        {
            _gameContext = new GameContext(
                field: _field,
                randomizer: _randomizer,
                spawner: _spawner,
                scoring: _scoringService,
                catalog: _catalog,
                events: _events,
                gravitySettings: _gravitySettings,
                scoreSettings: _scoreSettings,
                spawnSettings: _spawnSettings);
        }
        private void InitializeStateMachine() 
        {
            var states = new Dictionary<Type, IGameState>
            {
                {typeof(MainMenuState), new MainMenuState()},
                {typeof(SpawningState), new SpawningState()},
                {typeof(FallingState), new FallingState()},
                {typeof(LockingState), new LockingState()},
                {typeof(LineClearingState), new LineClearingState()},
                {typeof(PausedState), new PausedState()},
                {typeof(GameOverState), new GameOverState()},
            };

            _stateMachine = new GameStateMachine(
                context: _gameContext,
                states: states,
                initialStateType: typeof(MainMenuState));
        }
        private void InitializeCommandDispatcher() 
        {
            _commandDispatcher = new CommandDispatcher(_stateMachine);
        }
        private void InitializeBlockPool()
        {
            _blockPool = new BlockPool(
                prefab: _blockPrefab,
                container: _blockPoolContainer,
                animationSettings: _animationSettings,
                ghostMaterial: _ghostMaterial,
                defaultCapacity: 64,
                maxSize: 512);

            _blockPool.Warmup(64);
        }
        private void InitializePresentation() 
        {
            var fieldCenter = ComputeFieldCenter();

            _orbitalCamera.Initialize(
                events: _events,
                cameraSettings: _cameraSettings,
                animationSettings: _animationSettings,
                target: fieldCenter);

            _fieldView.Initialize(
                events: _events,
                pool: _blockPool,
                layout: _fieldLayout,
                animationSettings:_animationSettings,
                field: _field);

            _pieceView.Initialize(
                events: _events,
                pool: _blockPool,
                layout: _fieldLayout,
                animationSettings: _animationSettings);

            _ghostView.Initialize(
                events:_events,
                pool:_blockPool,
                layout: _fieldLayout,
                animationSettings: _animationSettings);

            _previewRenderer.Initialize(
                events: _events,
                pool: _blockPool,
                settings: _previewSettings,
                layout: _fieldLayout);

            _pauseController.Initialize(_events);

            _arenaView.Initialize(_field, _fieldLayout);
        }

        private Vector3 ComputeFieldCenter()
        {
            var sizeX = _fieldSettings.SizeX;
            var sizeY = _fieldSettings.SizeY;
            var sizeZ = _fieldSettings.SizeZ;
            var blockSize = _fieldLayout.BlockSize;

            var centerGrid = new Vector3((sizeX - 1) * 0.5f, (sizeY - 1) * 0.5f, (sizeZ - 1) * 0.5f);
            return _fieldLayout.WorldOrigin + centerGrid * blockSize;
        }
        private void InitializeUI() 
        { 
            _screenManager.Initialize(_events, _uiAnimationSettings);
            _mainMenuController.Initialize(_commandDispatcher);
            _hudController.Initialize();
            _uiPauseController.Initialize(_commandDispatcher);
            _gameOverController.Initialize(_commandDispatcher, _events);
            _scoreDisplay.Initialize(_events, _stateMachine, _uiAnimationSettings);
            _levelDisplay.Initialize(_events, _stateMachine, _uiAnimationSettings);
            _nextPiecePanel.Initialize(_previewRenderer);
        }
        private void InitializeInput()
        {
            _inputSource = new KeyboardInputSource(
                disptcher: _commandDispatcher,
                bindigs: _keyBindings,
                repeatSettings: _keyRepeatSettings,
                cameraOrientation: _orbitalCamera);
        }
    }
}
