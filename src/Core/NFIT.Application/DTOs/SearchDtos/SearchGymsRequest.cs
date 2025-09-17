namespace NFIT.Application.DTOs.SearchDtos;

public class SearchGymsRequest
{
    public string? Name { get; set; }

 
    public List<Guid> SubscriptionIds { get; set; } = new();
    public List<Guid> CategoryIds { get; set; } = new();


    public List<string> SubscriptionNames { get; set; } = new();
    public List<string> CategoryNames { get; set; } = new();


    public bool RequireAllSubscriptions { get; set; } = false; 
    public bool RequireAllCategories { get; set; } = false;    


    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
