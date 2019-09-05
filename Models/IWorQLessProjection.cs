namespace WorQLess.Models
{

    public interface IWorQLessProjection
    {
        IFieldExpression FieldExpression { get; set; }
    }

    public interface IWorQLessDynamicProjection : IWorQLessProjection
    {
    }
}
