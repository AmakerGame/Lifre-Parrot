# Ліфре

[![Останній реліз](https://img.shields.io/github/v/release/mrEdytor/Lifre)](https://github.com/mrEdytor/Lifre/releases/latest)
[![Ліцензія: MIT](https://img.shields.io/badge/Ліцензія-MIT-yellow.svg)](LICENSE)

> ## [⬇ Завантажити останню версію](https://github.com/mrEdytor/Lifre/releases/latest)

**Вітаємо у проєкті Ліфре! 🦜**

Ліфре — це папуга, який живе на вашому робочому столі. Він ходить,
літає, їсть, спить і реагує на вашу мишу — і все це без жодного
встановлення: одна папка, в якій можна редагувати тексти й додавати
власні зображення.

## Швидкий старт для користувача

1. **Завантажте** `StartLifre.exe` зі [сторінки Releases](https://github.com/mrEdytor/Lifre/releases/latest).
   - Для 64-бітної Windows (майже всі сучасні ПК) — папка `x64`.
   - Для старих 32-бітних систем — папка `x86`.
   - Якщо не знаєте — беріть `x64`.
2. **Двічі клікніть** `StartLifre.exe` — Ліфре з'явиться на екрані.

Жодних встановлень .NET, Visual C++ Redistributable, Java, Python чи
інших програм **не потрібно** — `.exe` повністю автономний.

### Керування

| Дія | Результат |
|-----|-----------|
| Лівий клік | Погладити (кілька поспіль — Ліфре ображається, а потім вибачається подарунком) |
| Клік по папузі з подарунком | Прийняти подарунок 🎁 |
| Затиснути й тягнути | Підхопити папугу — він дригає лапами; відпустиш — впаде й приземлиться |
| Правий клік (меню відкривається при відпусканні кнопки) | Меню: Годувати / Грати / Подарунок / Спати / Пауза / Налаштування / Вихід |
| Іконка в треї | Те саме меню + пауза |
| Повторний запуск `StartLifre.exe` | «Будить» уже запущеного Ліфре |

### Що вміє Ліфре

- **Ходить** перевалочкою, **стрибає** з присіданням і приземленням, **літає** з підібраними лапами, **танцює**, **чепурить пір'я** (дзьобом по крилу, потягується або струшується).
- **Деталізована анімація**: пружні присідання й відскоки при приземленні, інерція корпусу (гребінь відстає при розгоні), хода з колінами й без ковзання лап, обличчя «повертається» за поглядом, позіхання перед сном, жилка гніву, пір'їни в польоті, слова в бульбашці з'являються по одному.
- **Плавна анімація**: розгін і гальмування, лапи крокують у такт реальній швидкості, крило піднімається від спини при помаху, хвіст запізнюється за рухом, пил при приземленні, лінії швидкості в польоті.
- **Дивиться** очима за курсором (а коли миша стоїть — роззирається сам; при підхопленні дивується) і підходить подивитись, що ви робите.
- **Говорить** бульбашкою над головою (фрази — у `phrases.txt`).
- Раз на кілька хвилин **відлітає за край екрана й повертається з подарунком** (квітка, ягідка, зірка, пір'їнка, самоцвіт, лист, насінина, печиво, монетка).
- Їсть, спить і сумує, якщо його не гладити.
- **Нічого не чіпає в системі**: жодних Блокнотів, натискань клавіш чи пересування чужих вікон.

## Як це працює під капотом

- **Один екземпляр**: глобальний іменований м'ютекс `Global\Lifre_SingleInstance_Mutex`
  (fallback — `Local\...`). Повторний запуск надсилає команду `wake` через
  іменований канал і завершується.
- **Зупинка**: `StopLifre.exe` надсилає `stop` через канал
  `Lifre.Commands`; якщо процес не завершився — примусове завершення
  з записом у лог.
- **Папка даних** створюється автоматично:
  `Документи\DataIMGLifre\`

```
DataIMGLifre/
├─ config.json     — налаштування
├─ save.json       — стан (потреби, позиція, статистика)
├─ logs/app.log    — лог (ротація 5 МБ)
├─ texts/          — *.txt з усіма текстами
│    ├─ phrases.txt  (фрази Ліфре, один рядок = одна фраза)
│    ├─ menu.txt     — ключ=підпис для меню)
└─ images/         — ваші .png/.jpg — накладаються на папугу
```

## Налаштування та контент без перекомпіляції

### Свої тексти

Відкрийте `Документи\DataIMGLifre\texts\` у будь-якому текстовому редакторі й редагуйте
`.txt`-файли — зміни підхоплюються при наступному запуску.
У `phrases.txt` один рядок = одна фраза папуги.
У `menu.txt` формат `ключ=текст` (рядки з `#` — коментарі).

### Свої зображення

Покладіть `.png`/`.jpg` у `Документи\DataIMGLifre\images\` —
вони накладатимуться поверх намальованого папуги (капелюх, окуляри,
шарф тощо). Порожня папка = Ліфре малюється лише кодом.

### config.json (необов'язково)

```json
{
  "showSpeech": true,
  "movementSpeed": 1.0,
  "decayRate": 1.0,
  "alwaysOnTop": true,
  "idleThinkSeconds": 4.0,
  "startPaused": false,
  "giftsEnabled": true,
  "giftIntervalSeconds": 120.0
}
```

- `movementSpeed` — множник швидкості (0.2–3.0).
- `decayRate` — множник швидкості зменшення потреб (0.1–5.0).
- `alwaysOnTop` — тримати папугу поверх усіх вікон.
- `showSpeech` — бульбашки реплік над головою.
- `giftsEnabled` — чи приносить Ліфре подарунки; `giftIntervalSeconds` — середня пауза між ними (мінімум 30).

Якщо щось видалити — застосунок створить файли з типовими
українськими значеннями автоматично.

## Для розробників

### Локальна збірка

Потрібен .NET 8 SDK. Далі для кожної архітектури:

```bat
dotnet publish src/Lifre.Start/Lifre.Start.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true ^
  -p:DebugType=None -p:DebugSymbols=false -p:InvariantGlobalization=true ^
  -o out/x64\start

dotnet publish src/Lifre.Stop/Lifre.Stop.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true ^
  -p:DebugType=None -p:DebugSymbols=false -p:InvariantGlobalization=true ^
  -o out/x64\stop
```

Аналогічно для `win-x86` → `out/x86`. У `bin/`/`out/` з'являться
`StartLifre.exe` і `StopLifre.exe` — самодостатні, без .NET на машині.

### GitHub Actions

Workflow `.github/workflows/build.yml` запускається на `push`, `PR`,
`workflow_dispatch` і тегах `v*`. Він паралельно збирає матрицю
`win-x64`/`win-x86`, кешує NuGet, завантажує артефакти
`StartLifre-x64.exe`, `StopLifre-x64.exe`, `StartLifre-x86.exe`,
`StopLifre-x86.exe`. На тезі `v*` створюється Release з двома архівами:
`Lifre-x64.zip` і `Lifre-x86.zip`, кожен містить `StartLifre.exe` +
`StopLifre.exe` + `README.md` + `LICENSE`.

### Архітектура

```
src/
├─ Lifre.Core/        — моделі: PetState, Needs, PetPose, конфіг, константи
├─ Lifre.Rendering/   — програмний векторний рендер папуги (DrawingContext)
├─ Lifre.Behavior/    — «мозок»: FSM (Idle/Walk/Fly/Sleep/Eat/Play/Follow/Petted)
├─ Lifre.Persistence/ — AppPaths, JSON (атомарний запис), тексти, лог, зображення
├─ Lifre.Interop/     — WinAPI (WS_EX стилі), SingleInstance, NamedPipe клієнт
├─ Lifre.UI/          — ParrotWindow, трей, контекстне меню, хост, pipe-сервер
├─ Lifre.Start/       → StartLifre.exe
└─ Lifre.Stop/        → StopLifre.exe
```

## Ліцензія

Проєкт поширюється за ліцензією [MIT](LICENSE) —
Copyright (c) 2026 mrEdytor та контриб'ютори Ліфре.
Контрибуції вітаються: відкривайте Issue або надсилайте Pull Request!


## Автономний exe (v7)

`StartLifre.exe` — один файл: .NET і тексти (`src/Lifre.Persistence/Texts/*.txt`) вбудовані всередину.
Нічого не створюється в «Документах», зображень-аксесуарів і логів немає, стан не зберігається.
Збірка: запустіть `publish.bat` (результат — `dist\StartLifre.exe`) або GitHub Actions (`Build exe`).
