using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ElasticSearchQueries.Models;
using Nest;
using ElasticSearchQueries;

public static class ElasticsearchExtensions
{
    public static void AddElasticsearch(
        this IServiceCollection services, IConfiguration configuration)
    {
        var elasticSearchSettings = new ElasticSearchSettings();
        configuration.GetSection(ElasticSearchSettings.Position).Bind(elasticSearchSettings);

        Console.WriteLine(configuration.GetSection(ElasticSearchSettings.Position).ToString());

        Console.WriteLine($"URL: {elasticSearchSettings.url}. Index: {elasticSearchSettings.index}");

        var settings = new ConnectionSettings(new Uri(elasticSearchSettings.url))
            .DefaultIndex(elasticSearchSettings.index)
            .DefaultMappingFor<Post>(m => m
                .Ignore(p => p.IsPublished)
                .PropertyName(p => p.ID, "id")
            )
            .DefaultMappingFor<Comment>(m => m
                .Ignore(c => c.Email)
                .Ignore(c => c.IsAdmin)
                .PropertyName(c => c.ID, "id")
            );

        var client = new ElasticClient(settings);

        services.AddSingleton<IElasticClient>(client);
    }
}