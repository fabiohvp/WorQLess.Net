using Newtonsoft.Json.Linq;

namespace WorQLess.Models
{
    public interface IRawArguments
    {
        JArray Arguments { get; set; }
    }
}