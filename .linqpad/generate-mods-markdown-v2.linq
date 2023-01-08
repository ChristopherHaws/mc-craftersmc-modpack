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
  <Namespace>Grynwald.MarkdownGenerator</Namespace>
</Query>

#load "libs/packwiz.linq"
#load "libs/mod-groups-file.linq"
#load "libs/markdown-generator-extensions.linq"

async Task Main() {
	var modpack = Modpack.Open();
	var mods = await modpack.GetMods();
	var modSlugs = mods.Select(x => x.Slug).ToArray();

	var groupsFilePath = Path.Combine(modpack.DirectoryPath, "groups.yml");
	var groupsFile = await ModGroupsFile.ReadFromFile(groupsFilePath, sortMods: true);
	groupsFile.Sync(modSlugs);
	await groupsFile.Save();

	var md = new MdDocument();
	md.Root.Add(new MdHeading(level: 1, "CraftersMC Modpack Mods"));

	foreach (var modGroups in mods
		.GroupByModGroupName(groupsFile)
		.OrderByModGroupsFileOrder(groupsFile)
	) {
		md.Root.Add(new MdHeading(level: 2, modGroups.Key));
		
		var table = new MdTable(new(new[] {
			"Name",
			"Side",
			"Requirement",
			""
		}));
		
		md.Root.Add(table);
		
		var sortedMods = modGroups.OrderByDescending(x => x.IsRequired).ToArray();

		foreach (var mod in sortedMods) {
			var shields = new MdCompositeSpan(
				new MdStaticShieldIOSpan(
					label: string.Empty,
					message: ".pw.toml",
					color: "blueviolet",
					linkUrl: mod.ModpackGitHubUrl("ChristopherHaws", "mc-craftersmc-modpack", "1.19/dev")
				)
			);

			if (mod.Modrinth is not null) {
				shields.Add(new MdModrinthShieldSpan(
					projectSlug: mod.Slug,
					projectId: mod.Modrinth.Value.Id,
					projectUrl: mod.Modrinth.Value.Url,
					label: string.Empty,
					style: "flat",
					color: "26292f",
					logo: true
				));
			}

			if (mod.CurseForge is not null) {
				shields.Add(new MdCurseForgeShieldSpan(
					projectSlug: mod.Slug,
					projectId: mod.CurseForge.Value.Id,
					projectUrl: mod.CurseForge.Value.Url,
					style: "short"
				));
			}

			table.Add(new(new MdSpan[] {
				new MdRawMarkdownSpan(mod.Name),
				new MdRawMarkdownSpan(mod.Side == "both" ? "client/server" : mod.Side),
				new MdRawMarkdownSpan(mod.IsRequired ? "Required" : "Optional"),
				shields
			}));
		}
	}

	var markdown = md.AsMarkdown();
	var modsFilePath = Path.Combine(modpack.DirectoryPath, "MODS.md");
	md.Save(modsFilePath);
	md.DumpAsHtml();
}

public static class ModSortOrder {
	public static IEnumerable<IGrouping<string, PackwizMod>> GroupByModGroupName(
		this IEnumerable<PackwizMod> mods,
		ModGroupsFile groupsFile
	) {
		return mods.GroupBy(x => groupsFile.GetByModSlug(x.Slug)?.Name ?? "Unknown");
	}
	
	public static IOrderedEnumerable<T> OrderByModGroupsFileOrder<T>(
		this IEnumerable<T> grouping,
		ModGroupsFile groupsFile
	) where T : IGrouping<string, PackwizMod> {
		return grouping.OrderBy(x => {
			var groupName = x.Key;
			var group = groupsFile.Groups.SingleOrDefault(g => g.Name == groupName);
			if (group is null) {
				return int.MaxValue;
			}

			return groupsFile.Groups.IndexOf(group);
		});
	}
}

public static class PackwizModExtensions {
	public static string ModpackRelativeUrl(this PackwizMod mod) {
		return mod.ModpackRelativeFilePath.Replace('\\', '/').TrimStart('/');
	}
	
	public static string ModpackGitHubUrl(this PackwizMod mod, string userOrOrganizationName, string projectName, string branchName) {
		return GitHubDirectUrl(userOrOrganizationName, projectName, branchName, mod.ModpackRelativeUrl());
	}

	private static string GitHubDirectUrl(string userOrOrganizationName, string projectName, string branchName, string relativePath) {
		return "https://github.com/" + userOrOrganizationName + "/" + projectName + "/blob/" + branchName + "/" + relativePath.TrimStart('/');
	}
}
