using Newtonsoft.Json.Linq;

namespace WorQLess
{
	public interface IRawArguments
	{
		JArray Arguments { get; set; }
	}
}