using Application.Abstracts;
using Application.Extensions;
using Application.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.Branches;

public class GetBranchesQuery : FilterQuery,
    IRequest<Response<PaginatedResult<BranchSummaryDTO>>>,
    IFilterable<Branch>
{
    /// <summary>Searches NameEN and NameAR (case-insensitive contains).</summary>
    public string? Name { get; set; }

    /// <summary>Searches CityEN and CityAR (case-insensitive contains).</summary>
    public string? City { get; set; }

    /// <summary>Filter by active / inactive. Null = return all.</summary>
    public bool? IsActive { get; set; }
    public IQueryable<Branch> ApplyFilters(IQueryable<Branch> query)
    {
        if (!string.IsNullOrWhiteSpace(Name))
            query = query.Where(b =>
                b.NameEN.Contains(Name) ||
                b.NameAR.Contains(Name));

        if (!string.IsNullOrWhiteSpace(City))
            query = query.Where(b =>
                b.CityEN.Contains(City) ||
                b.CityAR.Contains(City));

        if (IsActive.HasValue)
            query = query.Where(b => b.IsActive == IsActive.Value);

        return query;
    }

    public IQueryable<Branch> ApplySort(IQueryable<Branch> query)
    {
        // Map the string key coming from the URL to a strongly-typed selector.
        // Unknown / missing keys fall back to Id so the result is always deterministic.
        return SortBy?.ToLower() switch
        {
            "name" => query.OrderByDirection(b => b.NameEN, IsDescending()),
            "city" => query.OrderByDirection(b => b.CityEN, IsDescending()),
            "active" => query.OrderByDirection(b => b.IsActive, IsDescending()),
            _ => query.OrderByDirection(b => b.Id, IsDescending())
        };
    }
}
