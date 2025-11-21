using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простая реализация Service Locator для регистрации и получения игровых сервисов.
/// Позволяет ослабить связанность между компонентами и облегчить тестирование.
/// </summary>
public static class GameServiceLocator
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    /// <summary>
    /// Регистрирует экземпляр сервиса по его типу.
    /// Повторная регистрация заменяет ранее сохранённое значение.
    /// </summary>
    public static void Register<TService>(TService service) where TService : class
    {
        if (service == null)
        {
            Debug.LogWarning($"Попытка зарегистрировать null-сервис {typeof(TService).Name}");
            return;
        }

        services[typeof(TService)] = service;
    }

    /// <summary>
    /// Пытается получить зарегистрированный сервис.
    /// </summary>
    public static bool TryGet<TService>(out TService service) where TService : class
    {
        if (services.TryGetValue(typeof(TService), out object value))
        {
            service = value as TService;
            return service != null;
        }

        service = null;
        return false;
    }

    /// <summary>
    /// Удаляет зарегистрированный сервис указанного типа.
    /// </summary>
    public static void Unregister<TService>() where TService : class
    {
        services.Remove(typeof(TService));
    }

    /// <summary>
    /// Полностью очищает локатор — полезно при смене сцены.
    /// </summary>
    public static void Clear()
    {
        services.Clear();
    }
}


