using Amazon.DynamoDBv2;
using Dhblog.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Dhblog.DataAccess;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDhblogDataAccess(this IServiceCollection services, DynamoDbOptions options)
    {
        services.AddSingleton(options);
        services.Configure<DynamoDbOptions>(o =>
        {
            o.Env = options.Env;
            o.Endpoint = options.Endpoint;
            o.Region = options.Region;
        });
        services.AddSingleton<IAmazonDynamoDB>(_ => DynamoDbClientExtensions.CreateClient(options));
        services.AddSingleton<TableNameResolver>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        services.AddScoped<IFeatureRoleRepository, FeatureRoleRepository>();
        services.AddScoped<IBlogRepository, BlogRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IUserFollowRepository, UserFollowRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
        return services;
    }
}
