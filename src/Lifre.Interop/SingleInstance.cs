using System.IO;
using Lifre.Core;

namespace Lifre.Interop;

/// <summary>
/// Менеджер одиночного екземпляра застосунку.
/// Спочатку намагається створити глобальний м'ютекс Global\Lifre_SingleInstance_Mutex;
/// якщо це неможливо (наприклад, обмеження сесії) — локальний Local\...
/// М'ютекс утримується весь час роботи процесу.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private Mutex? _mutex;

    /// <summary><c>true</c>, якщо цей процес — єдиний екземпляр.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Яке ім'я м'ютекса вдалося захопити.</summary>
    public string? MutexNameUsed { get; private set; }

    /// <summary>
    /// Намагається захопити м'ютекс одиночного екземпляра.
    /// </summary>
    /// <returns><c>true</c> — екземпляр primary; <c>false</c> — вже запущений інший.</returns>
    public bool Acquire()
    {
        foreach (var name in new[] { LifreConstants.MutexName, LifreConstants.MutexNameLocal })
        {
            try
            {
                var createdNew = false;
                var mutex = new Mutex(initiallyOwned: true, name, out createdNew);
                if (!createdNew)
                {
                    // М'ютекс уже існує — інший екземпляр працює (не пробуємо інше ім'я: інакше запуститься дублікат).
                    mutex.Dispose();
                    IsPrimary = false;
                    return false;
                }

                _mutex = mutex;
                IsPrimary = true;
                MutexNameUsed = name;
                return true;
            }
            catch
            {
                // Переходимо до наступного імені.
            }
        }

        IsPrimary = false;
        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch
        {
            // М'ютекс міг бути не захоплений у цьому потоці — ігноруємо.
        }
        _mutex?.Dispose();
    }
}
