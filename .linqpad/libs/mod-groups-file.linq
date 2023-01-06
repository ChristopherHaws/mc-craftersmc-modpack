<Query Kind="Program">
  <NuGetReference>YamlDotNet</NuGetReference>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>YamlDotNet.Serialization</Namespace>
  <Namespace>YamlDotNet.Serialization.ObjectGraphVisitors</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>YamlDotNet.Serialization.NamingConventions</Namespace>
</Query>

#load "packwiz.linq"

async Task Main() {
	var modpackPath = Modpack.GetRootPath();
	var groupsFilePath = Path.Combine(modpackPath, "groups.yml");
	var groups = await ModGroupsFile.ReadFromFile(groupsFilePath, sortMods: true);
	groups.Dump();
}

// You can define other methods, fields, classes and namespaces here

public class ModGroupsFile {
	private static readonly ISerializer serializer = new SerializerBuilder()
		.WithNamingConvention(CamelCaseNamingConvention.Instance)
		.EnablePrivateConstructors()
		.WithIndentedSequences()
		.Build();

	private static readonly IDeserializer deserializer = new DeserializerBuilder()
		.WithNamingConvention(CamelCaseNamingConvention.Instance)
		.EnablePrivateConstructors()
		.Build();

	[YamlIgnore]
	public string FilePath { get; private set; } = default!;

	[YamlMember(Alias = "groups", ApplyNamingConventions = false)]
	public List<ModGroup> Groups { get; set; } = new();

	public static async Task<ModGroupsFile> ReadFromFile(string path, bool sortMods) {
		var yaml = await File.ReadAllTextAsync(path);
		var groups = deserializer.Deserialize<ModGroupsFile>(yaml);
		groups.FilePath = path;

		if (sortMods) {
			foreach (var group in groups.Groups) {
				group.ModSlugs.Sort();
			}
		}
		
		return groups;
	}

	public async Task Save() {
		var yaml = serializer.Serialize(this);
		await File.WriteAllTextAsync(this.FilePath, yaml);
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
		var groups = this.Groups.Where(x => x.ModSlugs.Any(m => string.Equals(m, slug, StringComparison.OrdinalIgnoreCase))).ToArray();
		if (groups.Length <= 0) {
			return null;
		}

		if (groups.Length > 1) {
			throw new("Found multiple groups with slug: " + slug);
		}

		return groups.Single();
	}

	public ModGroup? GetByName(string groupName) {
		return this.Groups.SingleOrDefault(x => string.Equals(x.Name, groupName, StringComparison.OrdinalIgnoreCase));
	}
}

public class ModGroup {
	[YamlMember(Alias = "name", ApplyNamingConventions = false)]
	public string Name { get; set; } = default!;
	[YamlMember(Alias = "mods", ApplyNamingConventions = false)]
	public List<string> ModSlugs { get; set; } = new();
}