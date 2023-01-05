<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>Newtonsoft.Json.Schema</NuGetReference>
  <NuGetReference>RestSharp</NuGetReference>
  <NuGetReference>Tomlyn</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Schema.Generation</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Tomlyn</Namespace>
</Query>

async Task Main() {
	var modpackRootPath = GetModpackRootPath();
	var modsPath = Path.Combine(modpackRootPath, "mods");
	var modPaths = Directory.EnumerateFiles(modsPath, "*.pw.toml");
	foreach (var modPath in modPaths) {
		var file = await File.ReadAllTextAsync(modPath);
		Toml.ToModel<Mod>(file).Dump();
	}
}

private string GetModpackRootPath() {
	var currentQueryPath = Util.CurrentQueryPath;
	if (currentQueryPath is null) {
		throw new("Current query has not been saved yet");
	}
	
	var currentQueryDirectoryPath = Path.GetDirectoryName(Util.CurrentQueryPath);
	if (currentQueryDirectoryPath is null) {
		throw new("Could not determine current queries directory");
	}

	var modpackRootDirectoryPath = Directory.GetParent(currentQueryDirectoryPath);
	if (modpackRootDirectoryPath is null) {
		throw new("Could not determine modpack root directory");
	};
	
	return modpackRootDirectoryPath.FullName;
}

private void GenerateSchema() {
	var generator = new JSchemaGenerator();
	generator.GenerationProviders.Add(new StringEnumGenerationProvider());

	var schema = generator.Generate(typeof(Mod));
	schema.ToString().Dump();
}

// https://packwiz.infra.link/reference/pack-format/mod-toml/
public class Mod {
	[DataMember(Name = "name")]
	public string Name { get; set; } = default!;
	[DataMember(Name = "filename")]
	public string FileName { get; set; } = default!;
	[DataMember(Name = "side")]
	public string Side { get; set; } = default!;

	[DataMember(Name = "download")]
	public Download Download { get; set; } = default!;
	[DataMember(Name = "update")]
	public Update? Update { get; set; }
	[DataMember(Name = "option")]
	public Option? Option { get; set; }
}

public class Download {
	[DataMember(Name = "hash-format")]
	public string HashFormat { get; set; } = default!;
	[DataMember(Name = "hash")]
	public string Hash { get; set; } = default!;
	[DataMember(Name = "url")]
	public string? Url { get; set; }
	[DataMember(Name = "mode")]
	public string? Mode { get; set; }
}

public class Update {
	[DataMember(Name = "curseforge")]
	public CurseForgeUpdate? CurseForge { get; set; }
	[DataMember(Name = "modrinth")]
	public ModrinthUpdate? Modrinth { get; set; }
}

public class CurseForgeUpdate {
	[DataMember(Name = "file-id")]
	public string FileId { get; set; } = default!;
	[DataMember(Name = "project-id")]
	public string ProjectId { get; set; } = default!;
	[DataMember(Name = "release-channel")]
	public string? ReleaseChannel { get; set; }
}

public class ModrinthUpdate {
	[DataMember(Name = "mod-id")]
	public string ModId { get; set; } = default!;
	[DataMember(Name = "version")]
	public string Version { get; set; } = default!;
}

public class Option {
	[DataMember(Name = "optional")]
	public bool Optional { get; set; }
	[DataMember(Name = "default")]
	public bool? Default { get; set; }
	[DataMember(Name = "description")]
	public string? Description { get; set; }
}