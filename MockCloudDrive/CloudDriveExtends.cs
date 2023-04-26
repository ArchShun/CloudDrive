using CloudDrive.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MockCloudDrive;

namespace DependencyInjection;

public static class CloudDriveExtends
{
    public static IServiceCollection AddMockCloudDrive(this IServiceCollection services)
    {
        services.AddSingleton<ICloudDriveProvider, MockCloudDriveProvider>();
        return services;
    }
}
