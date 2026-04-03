using Microsoft.AspNetCore.Components;
using Biotrackr.UI.Models;
using Biotrackr.UI.Services;
using Radzen;

namespace Biotrackr.UI.Components;

/// <summary>
/// Base component for data pages that support single-date and date-range queries with pagination.
/// </summary>
/// <typeparam name="TSingle">The model type returned for a single-date query.</typeparam>
/// <typeparam name="TRange">The model type returned for each item in a date-range query.</typeparam>
public abstract class DataPageBase<TSingle, TRange> : ComponentBase
{
    [Inject] protected IBiotrackrApiService ApiService { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected string? ErrorMessage { get; set; }
    protected string Mode { get; set; } = "date";
    protected string SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)).ToString("yyyy-MM-dd");
    protected string StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-8)).ToString("yyyy-MM-dd");
    protected string EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)).ToString("yyyy-MM-dd");
    protected int CurrentPage { get; set; } = 1;

    protected TSingle? SingleItem { get; set; }
    protected PaginatedResponse<TRange>? RangeItems { get; set; }

    /// <summary>Fetch a single record for the given date (yyyy-MM-dd).</summary>
    protected abstract Task<TSingle?> FetchSingleAsync(string date);
    /// <summary>Fetch a paginated set of records for the given date range.</summary>
    protected abstract Task<PaginatedResponse<TRange>> FetchRangeAsync(string start, string end, int page);
    /// <summary>Display name of the data type, used in error messages.</summary>
    protected abstract string DataName { get; }

    protected override async Task OnInitializedAsync()
    {
        await LoadByDate(SelectedDate);
    }

    /// <summary>Load a single record by date and switch to date mode.</summary>
    protected async Task LoadByDate(string date)
    {
        Mode = "date";
        SelectedDate = date;
        IsLoading = true;
        ErrorMessage = null;
        SingleItem = default;

        try
        {
            SingleItem = await FetchSingleAsync(date);
        }
        catch (Exception)
        {
            ErrorMessage = $"Failed to load {DataName} data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Load a paginated range of records and switch to range mode.</summary>
    protected async Task LoadByRange((string StartDate, string EndDate) range)
    {
        Mode = "range";
        StartDate = range.StartDate;
        EndDate = range.EndDate;
        CurrentPage = 1;
        await LoadRangePage();
    }

    protected async Task LoadRangePage()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            RangeItems = await FetchRangeAsync(StartDate, EndDate, CurrentPage);
        }
        catch (Exception)
        {
            ErrorMessage = $"Failed to load {DataName} data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Handle Radzen DataGrid paging events.</summary>
    protected async Task OnLoadData(LoadDataArgs args)
    {
        CurrentPage = (int)((args.Skip ?? 0) / 10) + 1;
        await LoadRangePage();
    }
}
