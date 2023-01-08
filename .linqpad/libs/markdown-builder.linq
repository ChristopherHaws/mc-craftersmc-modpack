<Query Kind="Program">
  <NuGetReference>Markdig</NuGetReference>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>Markdig</Namespace>
  <Namespace>Xunit</Namespace>
  <Namespace>System.Runtime.CompilerServices</Namespace>
</Query>

#load "xunit"

void Main() {
	RunTests();  // Call RunTests() or press Alt+Shift+T to initiate testing.
}

public class MarkdownBuilder {
	private readonly StringBuilder sb = new();

	public MarkdownBuilder Append(char markdown) {
		this.sb.Append(markdown);
		return this;
	}

	public MarkdownBuilder Append(string? markdown) {
		this.sb.Append(markdown);
		return this;
	}

	public MarkdownBuilder AppendLine(string? markdown = null) {
		this.sb.AppendLine(markdown);
		return this;
	}

	public MarkdownBuilder AppendLink(
		string linkUrl,
		string? title = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		// If the text starts with a space, put it before the link, not within it
		if (title?.StartsWith(' ') ?? false) {
			title = title.TrimStart(' ');
			this.Append(' ');
		}
		
		this.AppendLink(
			linkUrl: linkUrl,
			titleBuilder: x => x.Append(title),
			skipPrefixSpace: skipPrefixSpace,
			skipSuffixSpace: skipSuffixSpace
		);

		return this;
	}

	public MarkdownBuilder AppendLink(
		string linkUrl,
		Action<MarkdownBuilder> titleBuilder,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		if (!skipPrefixSpace) {
			this.Append(' ');
		}
		
		var md = new MarkdownBuilder();
		titleBuilder(md);
		var title = md.AsMarkdown();
		
		if (string.IsNullOrEmpty(title)) {
			this.Append(linkUrl);
		} else {
			this.Append('[');
			titleBuilder(this);
			this.Append(']');
			this.Append('(');
			this.Append(linkUrl);
			this.Append(')');
		}

		if (!skipSuffixSpace) {
			this.Append(' ');
		}

		return this;
	}

	public MarkdownBuilder AppendImage(
		string imageUrl,
		string? altText = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		if (!skipPrefixSpace) {
			this.Append(' ');
		}

		this.Append("!");
		this.AppendLink(
			linkUrl: imageUrl,
			title: altText?.TrimStart(' '),
			skipPrefixSpace: true,
			skipSuffixSpace: skipSuffixSpace
		);

		return this;
	}

	public MarkdownBuilder AppendImageLink(
		string imageUrl,
		string linkUrl,
		string? altText = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		this.AppendLink(
			linkUrl: linkUrl,
			titleBuilder: builder => {
				builder.AppendImage(
					imageUrl: imageUrl,
					altText: altText?.TrimStart(' ')
				);
			},
			skipPrefixSpace: skipPrefixSpace,
			skipSuffixSpace: skipSuffixSpace
		);

		return this;
	}
	
	public MarkdownBuilder AppendShield(
		string shieldUrl,
		string? linkUrl = null,
		string? altText = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		if (linkUrl is null) {
			this.AppendImage(
				imageUrl: shieldUrl,
				altText: altText,
				skipPrefixSpace: skipPrefixSpace,
				skipSuffixSpace: skipSuffixSpace
			);
		} else {
			this.AppendImageLink(
				imageUrl: shieldUrl,
				linkUrl: linkUrl,
				altText: linkUrl,
				skipPrefixSpace: skipPrefixSpace,
				skipSuffixSpace: skipSuffixSpace
			);
		}

		return this;
	}

	public MarkdownBuilder AppendShield(
		string label,
		string message,
		string color,
		string? linkUrl = null,
		string? altText = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		var shieldUrl = $"https://img.shields.io/static/v1?label={label}&message={message}&color={color}";
		if (linkUrl is null) {
			this.AppendShield(
				shieldUrl: shieldUrl,
				altText: altText,
				skipPrefixSpace: skipPrefixSpace,
				skipSuffixSpace: skipSuffixSpace
			);
		} else {
			this.AppendShield(
				shieldUrl: shieldUrl,
				linkUrl: linkUrl,
				altText: linkUrl,
				skipPrefixSpace: skipPrefixSpace,
				skipSuffixSpace: skipSuffixSpace
			);
		}

		return this;
	}

	public string AsMarkdown() {
		return sb.ToString();
	}

	public string AsHtml() {
		var markdown = this.AsMarkdown();
		var pipeline = new MarkdownPipelineBuilder()
			.UseAdvancedExtensions()
			.UseEmojiAndSmiley()
			.UseAutoLinks()
			.Build();
		return Markdown.ToHtml(markdown, pipeline);
	}

	public MarkdownBuilder DumpAsHtml() {
		var html = this.AsHtml();
		Util.RawHtml(html).Dump();
		return this;
	}
}

public static class PipeTableBuilder {
	public static MarkdownBuilder AppendPipeTable<T>(
		this MarkdownBuilder md,
		ICollection<T> rows,
		Dictionary<string, Func<T, string>> columns
	) {
		var columnHeaders = new string[columns.Count];
		var columnWidths = new int[columns.Count];

		foreach (var column in columns.WithIndex()) {
			// Column Headers
			var header = column.Value.Key;
			columnHeaders[column.Index] = header;
			
			// Column Widths
			var maxItemLength = rows.Select(column.Value.Value).Max(x => x.Length);
			maxItemLength = Math.Max(maxItemLength, header.Length);
			columnWidths[column.Index] = maxItemLength;
		}
		
		md.AppendPipeTableHeader(
			columnHeaders: columnHeaders,
			columnWidths: columnWidths
		);

		foreach (var row in rows.WithIndex()) {
			md.AppendPipeTableRow(
				row: row.Value,
				columns: columns.Values,
				columnWidths: columnWidths
			);
		}
		
		md.AppendLine();

		return md;
	}

	private static MarkdownBuilder AppendPipeTableHeader(
		this MarkdownBuilder md,
		string[] columnHeaders,
		int[] columnWidths
	) {
		foreach (var columnHeader in columnHeaders.WithIndex()) {
			var isLastColumn = columnHeader.Index == columnHeaders.Length - 1;

			md.AppendPipeTableCell(
				value: columnHeader.Value,
				width: columnWidths[columnHeader.Index],
				appendPipeSuffix: isLastColumn
			);
		}

		md.AppendLine();

		foreach (var columnHeader in columnHeaders.WithIndex()) {
			var isLastColumn = columnHeader.Index == columnHeaders.Length - 1;
			var columnWidth = columnWidths[columnHeader.Index];

			md.AppendPipeTableCell(
				value: string.Empty.PadLeft(columnWidth, '-'),
				width: columnWidth,
				appendPipeSuffix: isLastColumn
			);
		}

		md.AppendLine();
		return md;
	}

	private static MarkdownBuilder AppendPipeTableRow<TRow>(
		this MarkdownBuilder md,
		TRow row,
		ICollection<Func<TRow, string>> columns,
		int[] columnWidths
	) {
		foreach (var column in columns.WithIndex()) {
			var columnValue = column.Value.Invoke(row);
			var columnWidth = columnWidths[column.Index];
			var isLastColumn = column.Index == columns.Count - 1;

			md.AppendPipeTableCell(
				value: columnValue,
				width: columnWidth,
				appendPipeSuffix: isLastColumn
			);
		}

		md.AppendLine();
		return md;
	}

	private static MarkdownBuilder AppendPipeTableRow<TRow>(
		this MarkdownBuilder md,
		string[] columns,
		int[] columnWidths
	) {
		foreach (var column in columns.WithIndex()) {
			var columnWidth = columnWidths[column.Index];
			var isLastColumn = column.Index == columns.Length - 1;

			md.AppendPipeTableCell(
				value: column.Value,
				width: columnWidth,
				appendPipeSuffix: isLastColumn
			);
		}

		md.AppendLine();
		return md;
	}

	private static MarkdownBuilder AppendPipeTableCell(
		this MarkdownBuilder md,
		string value,
		int width,
		bool appendPipeSuffix = false
	) {
		md.Append("| ");
		md.Append(value.PadRight(width));

		if (appendPipeSuffix) {
			md.Append(" |");
		} else {
			md.Append(" ");
		}
		
		return md;
	}
}

public static class Extensions {
	public static IEnumerable<(int Index, T Value)> WithIndex<T>(this IEnumerable<T> values) {
		var index = 0;
		foreach (var value in values) {
			yield return (index, value);
			index++;
		}
	}
}

public static class CurseForgeShields {
	private static readonly string baseUrl = @"https://cf.way2muchnoise.eu/";

	public static MarkdownBuilder AppendCurseForgeProjectShield(
		this MarkdownBuilder md,
		string projectSlug,
		int? projectId,
		string? title = null,
		string? style = null,
		string? extra = null,
		string? badgeStyle = null,
		string? projectUrl = null
	) {
		var imageUrl = baseUrl;

		if (style is not null) {
			imageUrl += style + "_";
		}

		imageUrl += projectId?.ToString() ?? projectSlug;

		if (extra is not null) {
			imageUrl += "_" + extra;
		}

		imageUrl += ".svg";

		if (badgeStyle is not null) {
			imageUrl += "?badge_style=" + badgeStyle;
		}

		if (projectUrl is null) {
			md.AppendImage(
				imageUrl: imageUrl,
				altText: title ?? projectSlug
			);
		} else {
			md.AppendImageLink(
				imageUrl: imageUrl,
				linkUrl: projectUrl,
				altText: title ?? projectSlug
			);
		}

		return md;
	}

	public static MarkdownBuilder AppendCurseForgeVersionsShield(
		this MarkdownBuilder md,
		string projectSlug,
		int? projectId,
		string? title = null,
		string? style = null,
		string? text = null,
		string? badgeStyle = null
	) {
		var imageUrl = "https://cf.way2muchnoise.eu/versions/";

		if (text is not null) {
			imageUrl += text + "_";
		}

		imageUrl += projectId?.ToString() ?? projectSlug;

		if (style is not null) {
			imageUrl += "_" + style;
		}

		imageUrl += ".svg";

		if (badgeStyle is not null) {
			imageUrl += "?badge_style=" + badgeStyle;
		}

		md.AppendImage(title ?? projectSlug, imageUrl);

		return md;
	}
}

public static class ModrinthShields {
	private static readonly string baseUrl = @"https://img.shields.io/modrinth/dt/";

	public static MarkdownBuilder AppendModrinthModShield(
		this MarkdownBuilder md,
		string modSlug,
		string? modId,
		string modUrl,
		string? label = null,
		string? hoverText = null,
		bool logo = true,
		string? style = "flat",
		string? color = null,
		string? logoColor = null
	) {
		var imageUrl = baseUrl;
		imageUrl += modId;
		imageUrl += "?label=" + (label ?? modSlug);

		if (color is not null) {
			imageUrl += "&color=" + color;
		}
		
		if (logo) {
			imageUrl += "&logo=modrinth";
		}

		if (logoColor is not null) {
			imageUrl += "&logoColor=" + logoColor;
		}

		if (style is not null) {
			imageUrl += "&style=" + style;
		}

		md.AppendImageLink(
			imageUrl: imageUrl,
			linkUrl: modUrl,
			altText: hoverText ?? label ?? modSlug
		);
		
		return md;
	}
}

#region private::Tests

[Fact]
void Link() => Test(md => md.AppendLink(
	linkUrl: "https://google.com",
	title: "Google"
));

[Fact]
void Image() => Test(md => md.AppendImage(
	imageUrl: "https://www.google.com/images/branding/googlelogo/1x/googlelogo_light_color_272x92dp.png",
	altText: "Google"
));

[Fact]
void ImageLink() => Test(md => md.AppendImageLink(
	imageUrl: "https://www.google.com/images/branding/googlelogo/1x/googlelogo_light_color_272x92dp.png",
	linkUrl: "https://google.com",
	altText: "Google"
));

[Fact]
void Shield() => Test(md => md.AppendShield(
	label: "label",
	message: "message",
	color: "purple",
	linkUrl: "https://google.com",
	altText: "Google"
));

[Fact]
void PipeTable() {
	var testData = new Character[] {
		new("Harry", "Potter", new[] { 1, 2, 3, 4, 5, 6, 7 }),
		new("Albus", "Percival Wulfric Brian Dumbledore", new[] { 1, 2, 3, 4, 5, 6, 7 }),
		new("Sirus", "Black", new[] { 3, 4, 5 }),
		new("Horace", "Slughorn", new[] { 6, 7 }),
	};
	
	Test(md => md.AppendPipeTable<Character>(
		rows: testData,
		columns: new() {
			["First Name"] = x => x.FirstName,
			["Last Name"] = x => x.LastName,
			["Books"] = x => string.Join(", ", x.Books)
		}
	));
}

public record Character(
	string FirstName,
	string LastName,
	int[] Books
);

void Test(Action<MarkdownBuilder> md, [CallerMemberName] string callerName = "") {
	const bool showDebugInfo = true;
	var builder = new MarkdownBuilder();
	md(builder);
	
	var html = Util.RawHtml(builder.AsHtml());
	
	if (!showDebugInfo) {
		html.Dump(callerName);
		return;
	}

	new {
		markdown = builder.AsMarkdown(),
		html = builder.AsHtml(),
		render = html
	}.Dump(callerName);
}

#endregion