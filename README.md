# WorQLess

Data Query Language. Use json to query your databases effortless

## Soon the official documentation

For now just some code samples

1. Create an endpoint

```
[Route("api/[controller]")]
[ApiController]
public class SampleController : ControllerBase
{
	internal readonly SampleContext DB;

	public TesteController(SampleContext context)
	{
		DB = context;
	}

	[HttpGet]
	public object Query([FromBody] IEnumerable<WorkflowRequest> requests)
	{
		var worQLess = new WorQLess(DB);
		var results = worQLess.Execute(requests);

		return results;
	}
}
```

1. Make requests sending a json

```
[{
	entity: 'SampleEntity', //your table
	name: '$Select', //default query selector
	rules: [
		{
			name: "EsferaAdministrativaIgualA`1",
			args: {Codigos: ['001']},
		},
		{
			name: "AnoIgualA`1",
			args: {Anos: [2018]},
		}
	],
}]
```
