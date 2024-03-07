namespace KeyValues2Parser.ParsingKV2
{
    public class Model
	{
		public VBlock DmElement { get; set; }
		public List<VBlock> DmeModel { get; set; } = new List<VBlock>(); // can there be multiple? Unsure
		public List<VBlock> DmeDag { get; set; } = new List<VBlock>();
		public List<VBlock> DmeVertexData { get; set; } = new List<VBlock>();


		public Model(string[] lines)
		{
			ParseFromDmx(lines.ToList());
		}

		public void ParseFromDmx(List<string> lines)
		{
			var numOfBracketsInside = 0;

            for (int i = 0; i < lines.Count; i++)
			{
				var lineFormatted = GetFormattedLine(ref numOfBracketsInside, lines[i]);

				if (lineFormatted == null)
					continue;

				switch (lineFormatted)
				{
					case "DmElement":
						DmElement = VBlockExtensions.GetNewVBlock(lineFormatted, lines, i);
						break;
					case "DmeModel":
						DmeModel.Add(VBlockExtensions.GetNewVBlock(lineFormatted, lines, i));
						break;
					case "DmeDag":
						DmeDag.Add(VBlockExtensions.GetNewVBlock(lineFormatted, lines, i));
						break;
					case "DmeVertexData":
						DmeVertexData.Add(VBlockExtensions.GetNewVBlock(lineFormatted, lines, i));
						break;
				}
			}
		}

		public static string? GetFormattedLine(ref int numOfBracketsInside, string lineToFormat)
		{
			var line = lineToFormat.Trim().Replace("\"", string.Empty).Replace("\t", string.Empty);

            if (string.IsNullOrWhiteSpace(line.Trim()))
                return null;

			if (line.EndsWith(","))
				line = line.Substring(0, line.Length - 1);

			if (line == "}" ||
				line == "]")
			{
				numOfBracketsInside--;
			}

			if (line == "{" ||
				line == "[")
			{
				numOfBracketsInside++;
			}

			if (numOfBracketsInside > 0)
                return null;

			if (line.TrimStart().StartsWith("<!--"))
                return null;

			return line;
		}
	}
}