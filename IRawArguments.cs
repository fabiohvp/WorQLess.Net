using Newtonsoft.Json.Linq;

namespace WorQLess.Net
{
	public interface IRawArguments
	{
		JArray Arguments { get; set; }
	}
}