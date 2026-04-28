# Brief — Интеграция Water Distortion с Game Logic

## Контекст

В арт-чате реализован screen-space эффект "колебания воды" — деформация изображения волной от точки эпицентра. Используется для двух событий: перемещение/поворот фигуры и удаление линии.

Эффект тематически правильный для подводной сцены (вода, не камера, реагирует на действия).

## Что уже сделано (в арт-чате)

Четыре файла добавлены в проект:

- `Assets/_Project/Art/Shaders/WaterDistortion.shader`
- `Assets/_Project/Code/Runtime/Rendering/WaterDistortionRenderPass.cs`
- `Assets/_Project/Code/Runtime/Rendering/WaterDistortionRenderFeature.cs`
- `Assets/_Project/Code/Runtime/Rendering/WaterDistortionManager.cs`

`WaterDistortionRenderFeature` подключен к `PC_Renderer` с назначенным шейдером.
`WaterDistortionManager` — singleton, висит на отдельном GameObject в сцене.

Компонент готов к использованию. Тебе нужно только **вызывать его API** из game logic.

## API для интеграции

`WaterDistortionManager` — singleton с одним публичным методом:

```csharp
public void TriggerWave(WaveType type, Vector3 worldOrigin, Vector2 directionBias);
```

**Параметры:**

- `type` — `WaveType.PieceMove` или `WaveType.LineClear`
- `worldOrigin` — world-space позиция эпицентра волны (Vector3)
- `directionBias` — направленность волны:
  - `Vector2.zero` → радиальная волна (расходится во все стороны от эпицентра)
  - Ненулевой вектор → волна имеет преимущественное направление

**Namespace:** `TetrisAR.Rendering`

## Интеграция — точки вызова

### 1. Перемещение / поворот фигуры

**Где:** в системе которая обрабатывает input или в Piece State (Falling state).

**Когда:** после успешного применения движения / поворота к фигуре.

**Что вызывать:**

```csharp
using TetrisAR.Rendering;

// При движении влево
WaterDistortionManager.Instance?.TriggerWave(
    WaveType.PieceMove,
    piece.transform.position,
    new Vector2(-1f, 0f)  // волна "идёт" влево
);

// При движении вправо
WaterDistortionManager.Instance?.TriggerWave(
    WaveType.PieceMove,
    piece.transform.position,
    new Vector2(1f, 0f)
);

// При движении вниз (soft drop / hard drop)
WaterDistortionManager.Instance?.TriggerWave(
    WaveType.PieceMove,
    piece.transform.position,
    new Vector2(0f, -1f)
);

// При повороте
WaterDistortionManager.Instance?.TriggerWave(
    WaveType.PieceMove,
    piece.transform.position,
    Vector2.zero  // радиальная — поворот это "twist", не направленное движение
);
```

**Важные моменты:**

- Используй `Instance?` (null-conditional) — Manager может не быть в сцене (например на главном меню), не должно крашить
- `worldOrigin` — позиция фигуры **в world space** (используй `Transform.position`, не grid coordinates)
- `directionBias` — это направление **в screen space** условно. Передавай вектор движения (нормализованный), Manager сам спроецирует
- Для повороτа используй `Vector2.zero` — будет красивая радиальная рябь от точки фигуры

### 2. Удаление линии (line clear)

**Где:** в системе которая обрабатывает clear events (LineClearService / GameLogic).

**Когда:** в момент когда линии помечены к удалению (или когда они физически удаляются — на твоё усмотрение, главное чтобы момент был визуально синхронизирован с пропаданием блоков).

**Что вызывать:**

```csharp
using TetrisAR.Rendering;

// Для каждой удаляемой линии
foreach (var clearedRowY in clearedRowYs)
{
    Vector3 rowCenterWorld = GetRowCenterWorldPosition(clearedRowY);

    WaterDistortionManager.Instance?.TriggerWave(
        WaveType.LineClear,
        rowCenterWorld,
        Vector2.zero  // радиальная — линия исчезает, эффект кругом
    );
}
```

**Важные моменты:**

- `rowCenterWorld` — центр удаляемой линии в world space. Если не знаешь как получить — `(playfield.transform.position + new Vector3(0, clearedRowY * cellSize, 0))` или аналогично исходя из вашей системы координат
- `directionBias = Vector2.zero` — для line clear радиальная волна выглядит лучше всего (это "взрыв" атмосферы)
- Если удаляются **несколько линий одновременно** (Tetris — 4 линии) — вызывай метод **по одному разу для каждой линии** или **один раз с центром масс**. Manager поддерживает до 4 одновременных волн, перекрытие даст drama. Я бы рекомендовал по разу на линию — больше juice.

## Опциональное расширение

Если в будущем добавятся другие события (game over, level up, special spawn), можно добавить новые типы:

1. Расширить enum `WaveType` в `WaterDistortionManager.cs`
2. Добавить новое поле `WaveSettings` в Inspector
3. Дополнить `GetSettings()` switch

Это **не делать сейчас**, только при появлении конкретной потребности.

## Проверка / отладка

`WaterDistortionManager` имеет два context menu items для тестирования из Inspector (не требует game logic):

- Right-click на компоненте → **Test: Trigger Piece Move Wave**
- Right-click на компоненте → **Test: Trigger Line Clear Wave**

Запусти Play Mode, выдели manager в Hierarchy, попробуй context menu — увидишь визуальный эффект. Если эффекта нет — что-то не так с настройкой Render Feature или shader не компилируется.

## Архитектурные замечания

- Manager использует **singleton pattern** через статическое свойство `Instance`. Это OK для UI/VFX manager, но если в проекте используется DI (например VContainer / Zenject) — можно переписать на DI-injected interface. Для текущей стадии — singleton проще.
- Manager обновляется через `Update()` — каждый кадр пересчитывает amplitude по decay curve и пушит в шейдер globals. Стоимость — O(n) по активным волнам, n ≤ 4. Незаметно для производительности.
- Нет аллокаций в hot path (массивы pre-allocated в Awake, no garbage).
- Нет зависимостей от Game Logic — Manager **не знает** о Piece, GridService и т.д. Игровая логика знает о Manager, но не наоборот. Чистое одностороннее зависимостное направление.

## Тонкая настройка эффекта

Параметры волн настраиваются в Inspector компонента `WaterDistortionManager`:

- **Piece Move Settings** — defaults: amplitude 0.012, speed 2.0, wavelength 0.18, duration 0.25
- **Line Clear Settings** — defaults: amplitude 0.04, speed 1.2, wavelength 0.3, duration 0.6

После интеграции — попроси product/art owner протестировать и подкрутить значения по ощущениям. Defaults — стартовая точка.

## Список задач для тебя

- [ ] Добавить вызов `TriggerWave(WaveType.PieceMove, ...)` в обработчик движения фигуры (left/right/down)
- [ ] Добавить вызов `TriggerWave(WaveType.PieceMove, ..., Vector2.zero)` в обработчик поворота фигуры
- [ ] Добавить вызов `TriggerWave(WaveType.LineClear, ...)` в систему clear (по разу на каждую линию)
- [ ] Проверить `using TetrisAR.Rendering;` в нужных файлах
- [ ] Использовать `Instance?` (null-conditional) во всех вызовах
- [ ] Проверить что `worldOrigin` — это **world space** позиция (не grid)
- [ ] Протестировать в Play Mode

## Что НЕ нужно делать

- ❌ Не создавай новые компоненты Manager — он уже есть
- ❌ Не модифицируй файлы из `Assets/_Project/Code/Runtime/Rendering/` — это арт-территория
- ❌ Не вызывай эффект из MonoBehaviour'ов вне game logic (например из BlockView) — это нарушит инкапсуляцию
- ❌ Не вызывай эффект каждый кадр (например в Update во время движения) — только на дискретных событиях

## Контакт

Если возникают вопросы по API или интеграции — это арт-чат, спрашивай. Если нужно расширить enum / добавить новый тип волны — тоже арт-чат.
