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

#load "libs/packwiz.linq"
#load "libs/mod-groups-file.linq"
#load "libs/markdown-builder.linq"

async Task Main() {
	var modpackRootPath = Modpack.GetRootPath();
	var modsPath = Modpack.GetModsDirectoryPath();
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

	var groupsFilePath = Path.Combine(modpackRootPath, "groups.yml");
	var groups = await ModGroupsFile.ReadFromFile(groupsFilePath, sortMods: true);
	groups.Sync(modSlugs);
	await groups.Save();

	var markdown = ModListMarkdownGenerator.GroupedByModGroupsAndRequired(groups, mods);
	//var markdown = ModListMarkdownGenerator.GroupedByCategoryAndRequired(mods);

	var modsFilePath = Path.Combine(modpackRootPath, "MODS.md");
	await File.WriteAllTextAsync(modsFilePath, markdown);
	
	var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
	var html = Markdown.ToHtml(markdown, pipeline);
	Util.RawHtml(html).Dump();
}

#region Markdown

public static class ModListMarkdownGenerator {
	public static string GroupedByCategoryAndRequired(IEnumerable<ModInfo> mods) {
		var md = new MarkdownBuilder();
		md.AppendLine($"# CraftersMC Modpack Mods");

		foreach (var modsByCategory in mods.GroupBy(x => x.Category).OrderBy(x => x.Key)) {
			md.AppendLine($"## {modsByCategory.Key}");

			foreach (var modsByRequired in modsByCategory.GroupBy(x => x.IsRequired).OrderByDescending(x => x.Key)) {
				md.AppendLine($"### {(modsByRequired.Key ? "Required" : "Optional")}");
				md.AppendMods(modsByRequired);
			}
		}

		return md.Build();
	}

	internal static string GroupedByModGroupsAndRequired(ModGroupsFile groups, List<ModInfo> mods) {
		var md = new MarkdownBuilder();
		md.AppendLine($"# CraftersMC Modpack Mods");

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
			md.AppendLine($"## {modsByCategory.Key}");

			foreach (var modsByRequired in modsByCategory.GroupBy(x => x.IsRequired).OrderByDescending(x => x.Key)) {
				md.AppendMods($"**{(modsByRequired.Key ? "Required" : "Optional")}**", modsByRequired);
			}
		}

		return md.Build();
	}
}

public static class ModMarkdownBuilder {
	public static void AppendMods(this MarkdownBuilder md, string title, IEnumerable<ModInfo> mods) {
		md.AppendLine(title);
		md.AppendMods(mods);
		md.AppendLine();
	}

	public static void AppendMods(this MarkdownBuilder md, IEnumerable<ModInfo> mods) {
		foreach (var mod in mods) {
			md.AppendMod(mod);
		}
	}

	public static void AppendMod(this MarkdownBuilder md, ModInfo mod) {
		md.Append($"* {mod.Name}");

		//if (mod.IsRequired) {
		//	this.Append($" *(required)*");
		//}

		if (mod.Path is not null) {
			md.AppendShield(
				shieldUrl: "https://img.shields.io/badge/packwiz-.pw.toml-blueviolet",
				linkUrl: mod.GitHubRelativePath,
				hoverText: "packwiz"
			);
		}

		if (mod.ModrinthUrl is not null) {
			md.AppendModrinthModShield(
				modSlug: mod.Slug,
				modId: mod.ModrinthId!,
				modUrl: mod.ModrinthUrl,
				label: "",
				hoverText: "modrinth",
				logo: true,
				style: "flat",
				color: "26292f"
			);
		}

		if (mod.CurseForgeUrl is not null) {
			//this.AppendLink(" [curseforge]", mod.CurseForgeUrl);
			md.AppendCurseForgeProjectShield(
				projectSlug: mod.Slug,
				projectId: mod.CurseForgeId,
				projectUrl: mod.CurseForgeUrl,
				style: "short"
			);
		}

		md.AppendLine();
	}
}

#endregion

public class ModInfo {
	required public string Path { get; init; }
	required public string GitHubRelativePath { get; init; }
	required public string Slug { get; init; }
	required public string Side { get; init; }
	required public string Name { get; init; }
	required public bool IsRequired { get; init; }
	required public string Category { get; init; }
	public int? CurseForgeId { get; private set; }
	public string? CurseForgeUrl { get; private set; }
	public string? CurseForgeFileUrl { get; private set; }
	public string? ModrinthId { get; private set; }
	public string? ModrinthUrl { get; private set; }
	public string? ModrinthFileUrl { get; private set; }
	public string? License { get; private set; }

	public static ModInfo FromPackwizMod(string path, string slug, PackwizMod mod) {
		var info = new ModInfo() {
			Path = path,
			GitHubRelativePath = "./" + path.Replace('\\', '/').TrimStart('/'),
			Slug = slug,
			Side = mod.Side,
			Name = mod.Name,
			IsRequired = mod.IsRequired,
			Category = "Unknown"
		};

		if (mod.Update?.CurseForge is not null) {
			var cf = mod.Update.CurseForge;
			info.CurseForgeId = cf.ProjectId;
			info.CurseForgeUrl = @$"https://www.curseforge.com/minecraft/mc-mods/{slug}";
			info.CurseForgeFileUrl = @$"https://www.curseforge.com/minecraft/mc-mods/{slug}/files/{cf.FileId}";
		}

		if (mod.Update?.Modrinth is not null) {
			var mr = mod.Update.Modrinth;
			info.ModrinthId = mr.ModId;
			info.ModrinthUrl = @$"https://modrinth.com/mod/{mr.ModId}";
			info.ModrinthFileUrl = @$"https://modrinth.com/mod/{mr.ModId}/version/{mr.Version}";
		}

		return info;
	}
}
