# Ліфре

[![Останній реліз](https://img.shields.io/github/v/release/mrEdytor/Lifre)](https://github.com/mrEdytor/Lifre/releases/latest)
[![Ліцензія: MIT](https://img.shields.io/badge/Ліцензія-MIT-yellow.svg)](LICENSE)

> ## [⬇ Завантажити останню версію](https://github.com/mrEdytor/Lifre/releases/latest)

**Вітаємо у проєкті Ліфре! 🦜**

Ліфре — це папуга, який живе на вашому робочому столі. Він ходить,
стрибає, летить, їсть, спить, танцює, чепурить пір'я і пригортається
до вас — і все це з **одного самодостатнього `.exe`**, без жодного
встановлення.

## Швидкий старт для користувача

1. **Завантажте** `StartLifre.exe` зі
   [сторінки Releases](https://github.com/mrEdytor/Lifre/releases/latest)
   (або зберіть самостійно — див. «Для розробників»).
2. **Двічі клікніть** `StartLifre.exe` — Ліфре з'явиться на екрані.
3. **Зупинити** — пункт «Вихід» у меню папуги чи в треї, або запустіть
   `StopLifre.exe`.

Жодних встановлень .NET, Visual C++ Redistributable, Java, Python чи
інших програм **не потрібно** — `StartLifre.exe` повністю автономний:
.NET runtime і всі тексти вбудовані всередину файлу.

### Керування

| Дія | Результат |
|-----|-----------|
| Лівий клік | Погладити папугу (кілька поспіль — він ображається, а потім вибачається) |
| Затиснути й тягнути | Підхопити папугу — він «тримається» за вас; відпустиш — впаде й приземлиться |
| Правий клік (меню на відпусканні) | Меню: Годувати / Грати / Подарунок / Літати / Живі Очі / Спати / Пауза / Вихід |
| Іконка в треї | Те саме меню + пробудження папуги по подвійному кліку |
| Повторний запуск `StartLifre.exe` | «Будить» уже запущеного Ліфре, другий екземпляр не створюється |

### Що вміє Ліфре

- **Багата поведінка**: ходьба перевалочкою, стрибки з присіданням,
  політ, приземлення з інерцією, танець, догляд за пір'ям, сон із
  позіханням, їжа, «підглядання» за курсором, реакція на надмірні
  кліки (ображений → подарунок-вибачення).
- **Потреби**: голод, енергія та настрій зменшуються з часом і
  впливають на вибір дій — проголодований хоче їсти, втомлений — спати.
- **Подарунки**: час від часу Ліфре відлітає за край екрана й
  повертається з подарунком у дзьобі — клікніть, щоб прийняти.
- **«Живі Очі»**: очі папуги стежать за курсором; режим можна
  вимкнути в меню.
- **Папужі звуки**: у бульбашці над головою з'являються звуки
  («Чірік!», «Кар-р!», «Піп-піп!») із друкарським ефектом.
- **Детальна анімація**: кінематика тіла (нахили, присідання, відскоки),
  махання крил і хвоста, кліпання, «подих» пір'я, емоційні позы —
  усе малюється програмно векторною графікою WPF.
- **Нічого не чіпає в системі**: жодних Блокнотів, натискань клавіш,
  змін чужих вікон чи записів на диск.

## Як це працює під капотом

- **Один екземпляр**: глобальний іменований м'ютекс
  `Global\Lifre_SingleInstance_Mutex` (fallback — `Local\...`).
  Повторний запуск надсилає команду `wake` через іменований канал
  `Lifre.Commands` і завершується.
- **Команди** через канал: `wake`, `pause`, `resume`, `stop`,
  `feed`, `play`, `sleep`, `fly`, `gift`. `StopLifre.exe` надсилає
  `stop` і, якщо процес не завершився, примусово зупиняє його.
- **Автономність**: `SelfContained` + `PublishSingleFile` +
  `EnableCompressionInSingleFile` — один `.exe` (~70 МБ), без
  супутніх `.dll`, без .NET на машині користувача.
- **Тексти вбудовані**: `phrases.txt` і `menu.txt` лежать у
  `src/Lifre.Persistence/Texts/` і компілюються в exe як ресурси.
  Папка `Documents\DataIMGLifre` **не створюється** — застосунок
  повністю безфайловий: ані логів, ані конфігів, ані збережень стану.

## Налаштування

Налаштування (`LifreConfig`: швидкість, темп зменшення потреб,
`alwaysOnTop`, подарунки, пауза на старті) зараз задані типовими
значеннями в коді — окремого `config.json` немає. Щоб щось змінити,
відредагуйте `src/Lifre.Core/LifreConfig.cs` і перезберіть exe.

## Для розробників

### Локальна збірка

Потрібен .NET 8 SDK. Швидкий спосіб — `publish.bat` у корені
(результат: `dist\StartLifre.exe`). Або вручну:

```bat
dotnet publish src/Lifre.Start/Lifre.Start.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true ^
  -p:DebugType=None -p:DebugSymbols=false ^
  -o dist
```

Для `StopLifre.exe` — той самий publish для
`src/Lifre.Stop/Lifre.Stop.csproj` (він менший і швидший).
Для 32-бітних систем замініть `win-x64` на `win-x86`.

> Зверніть увагу: у поточному CI зібрати `StopLifre.exe` і `win-x86`
> можна лише локально — workflow поки що публікує один артефакт
> `StartLifre.exe` для `win-x64`. Це свідоме спрощення «один файл —
> одна кнопка»; розширення матриці збірки — задача для контриб'юторів.

### GitHub Actions

Workflow `.github/workflows/build.yml` запускається на `push` і
`workflow_dispatch`. Він публікує `StartLifre.exe` (win-x64,
single-file, self-contained) і завантажує його як артефакт
`StartLifre-exe` на сторінці Actions → Build exe → Artifacts.

### Архітектура

```
src/
├─ Lifre.Core/        — моделі: PetState, Needs, PetPose, LifreConfig, константи
├─ Lifre.Rendering/   — програмний векторний рендер папуги (DrawingContext)
├─ Lifre.Behavior/    — «мозок»: FSM (Idle/Walk/Hop/Fly/Land/Sleep/Eat/
│                       Play/Preen/Peek/Petted/Grumpy/Held/Fall/FlyOff/
│                       Away/BringGift/PresentGift), потреби, подарунки
├─ Lifre.Persistence/ — вбудовані тексти (phrases.txt, menu.txt), логер-заглушка
├─ Lifre.Interop/     — WinAPI (WS_EX стилі), SingleInstance, NamedPipe клієнт
├─ Lifre.UI/          — ParrotWindow, трей, контекстне меню, хост, pipe-сервер
├─ Lifre.Start/       → StartLifre.exe
└─ Lifre.Stop/        → StopLifre.exe
```

## Ліцензія

Проєкт поширюється за ліцензією [MIT](LICENSE) —
Copyright (c) 2026 mrEdytor та контриб'ютори Ліфре.
Контрибуції вітаються: відкривайте Issue або надсилайте Pull Request!
