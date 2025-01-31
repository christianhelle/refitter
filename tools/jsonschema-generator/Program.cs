using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;
using Refitter.Core;

JSchemaGenerator generator = new JSchemaGenerator();
generator.DefaultRequired = Required.DisallowNull;
generator.ContractResolver = new CamelCasePropertyNamesContractResolver();
generator.GenerationProviders.Add(new StringEnumGenerationProvider());

JSchema schema = generator.Generate(typeof(RefitGeneratorSettings));
var json = schema.ToString();

Console.WriteLine(json);
