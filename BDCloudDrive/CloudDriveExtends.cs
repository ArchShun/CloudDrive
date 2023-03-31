using BDCloudDrive;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection;

public static class CloudDriveExtends
{
    public static IServiceCollection AddBDCloudDrive(this IServiceCollection services)
    {
        services.AddSingleton<ICloudDriveProvider, BDCloudDriveProvider>();
        return services;
    }
}
