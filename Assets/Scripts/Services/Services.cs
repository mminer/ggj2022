using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Service locator pattern.
/// </summary>
class Services : MonoBehaviour
{
    /// <summary>
    /// Base class for services.
    /// </summary>
    public abstract class Service : MonoBehaviour {}

    static Services instance;
    Dictionary<Type, Service> services;

    void Awake()
    {
        instance = this;
        services = FindObjectsOfType<Service>().ToDictionary(service => service.GetType());
    }

    public static T Get<T>() where T : Service
    {
        var type = typeof(T);
        return (T)instance.services[type];
    }
}
