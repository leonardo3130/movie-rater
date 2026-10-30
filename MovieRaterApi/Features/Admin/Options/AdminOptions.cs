namespace MovieRaterApi.Features.Admin.Options;

public class AdminOptions
{
    public const string SectionName = "Admin";

    // Kept as strings so an empty environment variable (e.g. Admin__UserIds__0=) does not
    // break configuration binding; entries that are not valid GUIDs are ignored.
    public List<string> UserIds { get; set; } = [];
}
