namespace WorQLess.Models
{
    public interface IWorQLessRule : IWorQLessProjection
    {
    }

    public interface IWorQLessRuleBooster : IWorQLessRule
    {
        object Value { get; set; }
    }
}
