namespace WorQLess.Requests
{
	public interface IRequest
	{
		string Name { get; set; }
		object Args { get; set; }
	}
}