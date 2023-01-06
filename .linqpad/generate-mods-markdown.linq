<Query Kind="Program">
  <NuGetReference>Markdig</NuGetReference>
  <NuGetReference>RestSharp</NuGetReference>
  <NuGetReference>Tomlyn</NuGetReference>
  <Namespace>Markdig</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Tomlyn</Namespace>
  <Namespace>Tomlyn.Model</Namespace>
</Query>

async Task Main() {
	var modpackRootPath = GetModpackRootPath();
	var modsPath = Path.Combine(modpackRootPath, "mods");
	var modPaths = Directory.EnumerateFiles(modsPath, "*.pw.toml");

	var mods = new List<ModInfo>();
	var modSlugs = new List<string>();
	
	foreach (var modPath in modPaths) {
		var mod = await PackwizMod.ReadFromFile(modPath);
		var relativeModPath = Path.GetRelativePath(modpackRootPath, modPath);
		var slug = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(modPath));
		modSlugs.Add(slug);
		mods.Add(ModInfo.FromPackwizMod(relativeModPath, slug, mod));
	}

	var groupsFilePath = Path.Combine(modpackRootPath, "groups.toml");
	var groups = await ModGroupsFile.ReadFromFile(groupsFilePath, sortMods: true);
	groups.Sync(modSlugs);
	//await groups.Save();

	var markdown = ModListMarkdownGenerator.GroupedByModGroupsAndRequired(groups, mods);
	//var markdown = ModListMarkdownGenerator.GroupedByCategoryAndRequired(mods);

	//var modsFilePath = Path.Combine(modpackRootPath, "MODS.md");
	//await File.WriteAllTextAsync(modsFilePath, markdown);
	
	var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
	var html = Markdown.ToHtml(markdown, pipeline);
	Util.RawHtml(html).Dump();
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

public static class ModListMarkdownGenerator {
	public static string GroupedByCategoryAndRequired(IEnumerable<ModInfo> mods) {
		var modList = new ModListMarkdownBuilder();
		modList.AppendLine($"# CraftersMC Modpack Mods");

		foreach (var modsByCategory in mods.GroupBy(x => x.Category).OrderBy(x => x.Key)) {
			modList.AppendLine($"## {modsByCategory.Key}");

			foreach (var modsByRequired in modsByCategory.GroupBy(x => x.IsRequired).OrderByDescending(x => x.Key)) {
				modList.AppendLine($"### {(modsByRequired.Key ? "Required" : "Optional")}");
				modList.AppendMods(modsByRequired);
			}
		}

		return modList.Build();
	}

	internal static string GroupedByModGroupsAndRequired(ModGroupsFile groups, List<ModInfo> mods) {
		var modList = new ModListMarkdownBuilder();
		modList.AppendLine($"# CraftersMC Modpack Mods");

		foreach (var modsByCategory in mods
			.GroupBy(x => groups.GetByModSlug(x.Slug)?.Name ?? x.Category)
			.OrderBy(x => {
				var group = groups.Groups.SingleOrDefault(g => g.Name == x.Key);
				if (group is null) {
					return int.MaxValue;
				}
				
				return groups.Groups.IndexOf(group);
			})
		) {
			modList.AppendLine($"## {modsByCategory.Key}");

			foreach (var modsByRequired in modsByCategory.GroupBy(x => x.IsRequired).OrderByDescending(x => x.Key)) {
				modList.AppendLine($"### {(modsByRequired.Key ? "Required" : "Optional")}");
				modList.AppendMods(modsByRequired);
			}
		}

		return modList.Build();
	}
}

public class MarkdownBuilder {
	private readonly StringBuilder sb = new();

	public void Append(string? markdown) {
		this.sb.Append(markdown);
	}

	public void AppendLine(string? markdown = null) {
		this.sb.AppendLine(markdown);
	}

	public void AppendLink(string? text, string url) {
		this.Append($"[{text}]({url})");
	}
	
	public string Build() {
		return sb.ToString();
	}
}

public class ModListMarkdownBuilder : MarkdownBuilder {
	public void AppendMods(string title, IEnumerable<ModInfo> mods) {
		this.AppendLine(title);
		this.AppendMods(mods);
		this.AppendLine();
		this.AppendLine();
	}

	public void AppendMods(IEnumerable<ModInfo> mods) {
		foreach (var mod in mods) {
			this.AppendMod(mod);
		}
	}

	public void AppendMod(ModInfo mod) {
		this.Append($"* {mod.Name}");

		if (mod.IsRequired) {
			this.Append($" *(required)*");
		}

		if (mod.Path is not null) {
			this.AppendLink(" [packwiz]", mod.Path);
		}

		if (mod.ModrinthUrl is not null) {
			this.AppendLink(" [modrinth]", mod.ModrinthUrl);
		}

		if (mod.CurseForgeUrl is not null) {
			this.AppendLink(" [curseforge]", mod.CurseForgeUrl);
		}

		this.AppendLine();
	}
}

public class ModInfo {
	required public string Path { get; init; }
	required public string Slug { get; init; }
	required public string Side { get; init; }
	required public string Name { get; init; }
	required public bool IsRequired { get; init; }
	required public string Category { get; init; }
	public string? CurseForgeUrl { get; private set; }
	public string? CurseForgeFileUrl { get; private set; }
	public string? ModrinthUrl { get; private set; }
	public string? ModrinthFileUrl { get; private set; }
	public string? License { get; private set; }

	public static ModInfo FromPackwizMod(string path, string slug, PackwizMod mod) {
		var info = new ModInfo() {
			Path = path,
			Slug = slug,
			Side = mod.Side,
			Name = mod.Name,
			IsRequired = mod.IsRequired,
			Category = "Unknown"
		};

		if (mod.Update?.CurseForge is not null) {
			var cf = mod.Update.CurseForge;
			info.CurseForgeUrl = @$"https://www.curseforge.com/minecraft/mc-mods/{cf.ProjectId}";
			info.CurseForgeFileUrl = @$"https://www.curseforge.com/minecraft/mc-mods/{cf.ProjectId}/files/{cf.FileId}";
		}

		if (mod.Update?.Modrinth is not null) {
			var mr = mod.Update.Modrinth;
			info.ModrinthUrl = @$"https://modrinth.com/mod/{mr.ModId}";
			info.ModrinthFileUrl = @$"https://modrinth.com/mod/{mr.ModId}/version/{mr.Version}";
		}
		
		return info;
	}
}

public class ModGroupsFile : ITomlMetadataProvider {
	private static TomlModelOptions tomlOptions = new() {
		IncludeFields = true,
		IgnoreMissingProperties = false,
		ConvertPropertyName = name => name
	};

	[JsonIgnore]
	public string FilePath { get; private set; } = default!;
	
	[JsonPropertyName("groups")]
	public List<ModGroup> Groups { get; set; } = new();
	
	public static async Task<ModGroupsFile> ReadFromFile(string path, bool sortMods) {
		var toml = await File.ReadAllTextAsync(path);
		var groups = Toml.ToModel<ModGroupsFile>(toml, path, tomlOptions);
		groups.FilePath = path;
		
		if (sortMods) {
			foreach (var group in groups.Groups) {
				group.ModSlugs.Sort();
			}
		}
		return groups;
	}

	public async Task Save() {
		var toml = Toml.FromModel(this, tomlOptions);
		toml.Dump();
		await Task.Delay(0);
		await File.WriteAllTextAsync(this.FilePath, toml);
	}
	
	/// <summary>
	/// Removes mods that are not passed in and adds new mods to the "Unknown" group
	/// </summary>
	public void Sync(IEnumerable<string> modSlugs) {
		foreach (var group in this.Groups) {
			group.ModSlugs.RemoveAll(x => !modSlugs.Contains(x, StringComparer.OrdinalIgnoreCase));
		}
		
		var currentModIds = this.Groups.SelectMany(x => x.ModSlugs);
		var addedModIds = modSlugs.Except(currentModIds, StringComparer.OrdinalIgnoreCase);
		
		var unknownGroup = this.GetByName("Unknown");
		if (unknownGroup is null) {
			unknownGroup = new() {
				Name = "Unknown"
			};
			this.Groups.Add(unknownGroup);
		}
		
		unknownGroup.ModSlugs.AddRange(addedModIds);
	}
	
	public ModGroup? GetByModSlug(string slug) {
		var groups = this.Groups.Where(x => x.ModSlugs.Any(m => m == slug)).ToArray();
		if (groups.Length <= 0) {
			return null;
		}
		
		if (groups.Length > 1) {
			throw new("Found multiple groups");
		}
		
		return groups.Single();
	}

	public ModGroup? GetByName(string groupName) {
		return this.Groups.SingleOrDefault(x => string.Equals(x.Name, groupName, StringComparison.OrdinalIgnoreCase));
	}

	// storage for comments and whitespace
	TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
}

public class ModGroup : ITomlMetadataProvider {
	[DataMember(Name = "name")]
	public string Name { get; set; } = default!;
	[DataMember(Name = "mods")]
	public List<string> ModSlugs { get; set; } = new();
	// storage for comments and whitespace
	TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
}

// https://packwiz.infra.link/reference/pack-format/mod-toml/
public class PackwizMod {
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
	
	public bool IsRequired => this.Option is null ? true : !this.Option.Optional;

	public static async Task<PackwizMod> ReadFromFile(string path) {
		var file = await File.ReadAllTextAsync(path);
		var mod = Toml.ToModel<PackwizMod>(file);
		return mod;
	}
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