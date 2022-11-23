<Query Kind="Program">
  <NuGetReference>RestSharp</NuGetReference>
  <NuGetReference>Tomlyn</NuGetReference>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Tomlyn</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
</Query>

async Task Main() {
	var filePath = @"C:\dev\chaws\mc\chaws-modpack\index.toml";
	var toml = await File.ReadAllTextAsync(filePath);
	var model = Toml.ToModel<IndexFileData>(toml);
	
	foreach (var file in model.Files) {
		if (file.Metafile ?? false) {
			continue;
		}
		
		if (file.Preserve != null) {
			continue;
		}

		if (file.File.StartsWith(@"config/") ||
			file.File.StartsWith(@"dynmap/") ||
			file.File.StartsWith(@"resourcepacks/") ||
			file.File.StartsWith(@"resourcepacks/") ||
			file.File.StartsWith(@"server.properties")) {
			file.Preserve = false;
			continue;
		}
		
		file.File.Dump("Unhandled");
	}

	toml = Toml.FromModel(model);
	toml = toml.Replace("[[files]]", Environment.NewLine + "[[files]]");
	toml.Dump();
	
	await File.WriteAllTextAsync(filePath, toml);
}

// You can define other methods, fields, classes and namespaces here
public class IndexFileData {
	[DataMember(Name = "hash-format")]
	public string HashFormat { get; set; } = default!;
	[DataMember(Name = "files")]
	public List<FileData> Files { get; set; } = new();
}

public class FileData {
	[DataMember(Name = "file")] public string File { get; set; } = default!;
	[DataMember(Name = "hash")] public string Hash { get; set; } = default!;
	[DataMember(Name = "alias")] public string? Alias { get; set; }
	[DataMember(Name = "hash-format")] public string? HashFormat { get; set; }
	[DataMember(Name = "metafile")]	public bool? Metafile { get; set; }
	[DataMember(Name = "preserve")]	public bool? Preserve { get; set; }
}