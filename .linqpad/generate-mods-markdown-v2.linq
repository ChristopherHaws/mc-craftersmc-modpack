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
	var mods = await PackwizMod.ReadFromFiles(modPaths);
	var modSlugs = mods.Select(x => x.Slug).ToArray();

	var groupsFilePath = Path.Combine(modpackRootPath, "groups.yml");
	var groups = await ModGroupsFile.ReadFromFile(groupsFilePath, sortMods: true);
	groups.Sync(modSlugs);
	await groups.Save();

	var md = new MarkdownBuilder();
	md.AppendLine($"# CraftersMC Modpack Mods");

	foreach (var modsByCategory in mods
		.GroupBy(x => groups.GetByModSlug(x.Slug)?.Name ?? "Unknown")
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
			md.AppendLine($"**{(modsByRequired.Key ? "Required" : "Optional")}**");

			foreach (var mod in mods) {
				md.Append($"* {mod.Name}");

				if (mod.FullFilePath is not null) {
					md.AppendShield(
						shieldUrl: "https://img.shields.io/badge/packwiz-.pw.toml-blueviolet",
						linkUrl: mod.ModpackGitHubUrl("ChristopherHaws", "mc-craftersmc-modpack", "1.19/dev"),
						hoverText: "packwiz"
					);
				}

				if (mod.Modrinth is not null) {
					md.AppendModrinthModShield(
						modSlug: mod.Slug,
						modId: mod.Modrinth.Value.Id,
						modUrl: mod.Modrinth.Value.Url,
						label: "",
						hoverText: "modrinth",
						logo: true,
						style: "flat",
						color: "26292f"
					);
				}

				if (mod.CurseForge is not null) {
					//this.AppendLink(" [curseforge]", mod.CurseForgeUrl);
					md.AppendCurseForgeProjectShield(
						projectSlug: mod.Slug,
						projectId: mod.CurseForge.Value.Id,
						projectUrl: mod.CurseForge.Value.Url,
						style: "short"
					);
				}

				md.AppendLine();
			}

			md.AppendLine();
		}
	}

	var markdown = md.Build();

	var modsFilePath = Path.Combine(modpackRootPath, "MODS.md");
	await File.WriteAllTextAsync(modsFilePath, markdown);
	
	md.DumpAsHtml();
}

public static class ModMarkdownBuilder {
	public static string ModpackRelativeUrl(this PackwizMod mod) {
		return "/" + mod.ModpackRelativeFilePath.Replace('\\', '/').TrimStart('/');
	}
	
	public static string ModpackGitHubUrl(this PackwizMod mod, string userOrOrganizationName, string projectName, string branchName) {
		return GitHubDirectUrl(userOrOrganizationName, projectName, branchName, mod.ModpackRelativeUrl());
	}

	private static string GitHubDirectUrl(string userOrOrganizationName, string projectName, string branchName, string relativePath) {
		return "https://github.com/" + userOrOrganizationName + "/" + projectName + "/blob/" + branchName + "/" + relativePath.TrimStart('/');
	}
}
