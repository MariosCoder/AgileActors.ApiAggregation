using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IReadOnlyList<IDashboardProvider> _providers;

    public DashboardService(IEnumerable<IDashboardProvider> providers)
    {
        _providers = providers.ToArray();
    }

    public async Task<DashboardResponse> GetDashboardAsync(DashboardRequest request, CancellationToken cancellationToken = default)
    {
        var requestedCategories = request.Categories.Select(category => category.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var availableCategories = _providers.Select(provider => provider.Category).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownCategories = requestedCategories.Except(availableCategories, StringComparer.OrdinalIgnoreCase).ToArray();

        if (unknownCategories.Length > 0)
        {
            throw new ArgumentException($"Unknown categories: {string.Join(", ", unknownCategories)}. Available categories: {string.Join(", ", availableCategories)}.", nameof(request.Categories));
        }

        var selectedProviders = _providers.Where(provider => requestedCategories.Count == 0 || requestedCategories.Contains(provider.Category));
        var tasks = selectedProviders.Select(provider => provider.GetSectionAsync(request, cancellationToken)).ToArray();

        var sections = await Task.WhenAll(tasks);

        var hasUsableData = sections.Any(section => section.Status != DashboardSectionStatus.Unavailable);
        var allSectionsHealthy = sections.All(section => section.Status is DashboardSectionStatus.Fresh or DashboardSectionStatus.Cached);

        var status = DashboardStatus.Unavailable;

        if (hasUsableData)
        {
            status = allSectionsHealthy ? DashboardStatus.Complete : DashboardStatus.Partial;
        }

        return new DashboardResponse
        {
            Status = status,
            Sections = sections
        };
    }
}