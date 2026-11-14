# Ліфре

[![Останній реліз](https://img.shields.io/github/v/release/AmakerGame/Lifre-Parrot)](https://github.com/AmakerGame/Lifre-Parrot/releases/latest)
[![Ліцензія: MIT](https://img.shields.io/badge/Ліцензія-MIT-yellow.svg)](LICENSE)

> ## [⬇ Завантажити останню версію](https://github.com/AmakerGame/Lifre-Parrot/releases/latest)

**Вітаємо у проєкті Ліфре! 🦜**

Ліфре — це папуга, який живе на вашому робочому столі. Він ходить,
стрибає, летить, їсть, спить, танцює, чепурить пір'я і пригортається
до вас — і все це з **самодостатніх `.exe`**, без жодного встановлення.

## Швидкий старт для користувача

1. **Завантажте** архів зі
   [сторінки Releases](https://github.com/AmakerGame/Lifre-Parrot/releases/latest)
   (`Lifre-x64.zip` або `Lifre-x86.zip` — див. вибір нижче)
   або зберіть самостійно — див. «Для розробників».
2. **Двічі клікніть** `StartLifre.exe` — Ліфре з'явиться на екрані.
3. **Зупинити** — пункт «Вихід» у контекстному меню папуги
   (правий клік) або в меню іконки трею. Альтернатива — запустіть
   `StopLifre.exe` поруч.

Жодних встановлень .NET, Visual C++ Redistributable, Java, Python чи
інших програм **не потрібно** — обидва `.exe` повністю автономні:
.NET runtime і всі тексти вбудовані всередину файлів.

### Який архів обрати: x64 чи x86?

- **x64** — для 64-бітної Windows (майже всі сучасні ПК).
- **x86** — для старих 32-бітних систем.
- **Не знаєте?** Беріть `x64` — він працює на будь-якій сучасній
  Windows 10/11.

У кожному архіві два файли:

| Файл | Призначення |
|------|-------------|
| `StartLifre.exe` | Запускає папугу на робочому столі |
| `StopLifre.exe` | Коректно зупиняє Ліфре (якщо запущений) |

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
  `EnableCompressionInSingleFile` + `PublishReadyToRun` — один
  `.exe` на архітектуру, без супутніх `.dll`, без .NET на машині
  користувача.
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

Потрібен .NET 8 SDK. Швидкий спосіб — `publish.bat` у корені:
він збирає всі 4 файли в `dist\x64\` і `dist\x86\`
(`StartLifre.exe` + `StopLifre.exe` у кожній папці).

Або вручну для кожної архітектури:

```bat
dotnet publish src/Lifre.Start/Lifre.Start.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true ^
  -p:DebugType=None -p:DebugSymbols=false ^
  -o out/x64/start

dotnet publish src/Lifre.Stop/Lifre.Stop.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true ^
  -p:DebugType=None -p:DebugSymbols=false ^
  -o out/x64/stop
```

Аналогічно для `win-x86` → `out/x86\start` і `out/x86\stop`.
Усі `.exe` — самодостатні, працюють на чистій Windows 10/11
без .NET, Visual C++ Redistributable та інших залежностей.

### GitHub Actions

Workflow `.github/workflows/build.yml` запускається на `push`,
PR, `workflow_dispatch` і тегах `v*`:

- **Матриця** `win-x64` × `win-x86` × `StartLifre`/`StopLifre` —
  чотири паралельні збірки, кожна завантажується як артефакт
  (`StartLifre-win-x64`, `StopLifre-win-x86` тощо) на сторінці
  Actions → Build exe → Artifacts.
- **На тезі `v*`** створюється GitHub Release з двома архівами:
  `Lifre-x64.zip` і `Lifre-x86.zip`, кожен містить
  `StartLifre.exe` + `StopLifre.exe`.

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
Copyright (c) 2026 Edytor Studio (AmakerGame).
Контрибуції вітаються: відкривайте Issue або надсилайте Pull Request!
