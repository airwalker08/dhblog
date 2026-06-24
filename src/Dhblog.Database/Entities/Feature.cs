namespace Dhblog.Database.Entities;

public class Feature
{
    public string FeatureId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NavPath { get; set; } = string.Empty;
    public string ParentFeatureId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
