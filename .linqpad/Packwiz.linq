<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>Newtonsoft.Json.Schema</NuGetReference>
  <NuGetReference>RestSharp</NuGetReference>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Newtonsoft.Json.Schema.Generation</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

void Main() {
	var generator = new JSchemaGenerator();
	generator.GenerationProviders.Add(new StringEnumGenerationProvider());
	
	var schema = generator.Generate(typeof(Mod));
	schema.ToString().Dump();
}

// You can define other methods, fields, classes and namespaces here
public class Mod {
	public Download download { get; set; }
}

[KnownType(typeof(UrlDownload))]
[KnownType(typeof(CurseForgeDownload))]
[JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
public class Download {
	public string hash { get; set; }
}

public class UrlDownload : Download {
	public string url { get; set; }

}

public class CurseForgeDownload : Download {
	public string mode { get; set; }
}