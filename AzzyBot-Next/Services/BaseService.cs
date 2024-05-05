using System;
using Microsoft.Extensions.DependencyInjection;

namespace AzzyBot.Services;

internal class BaseService
{
    /// <summary>
    /// Checks if a service is registered in the service provider
    /// </summary>
    /// <typeparam name="T">The type of the service to check.</typeparam>
    /// <param name="serviceProvider">The service provider to check.</param>
    /// <returns><see langword="true"/> if the service was found, otherwise <see langword="false"/>.</returns>
    protected static bool CheckIfServiceIsRegistered<T>(IServiceProvider serviceProvider) where T : notnull
    {
        // notnull has to be used because otherwise
        // the compiler will complain about the type being nullable

        try
        {
            serviceProvider.GetRequiredService<T>();

            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
