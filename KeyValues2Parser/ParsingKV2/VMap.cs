namespace KeyValues2Parser.ParsingKV2
{
    public class VMap
	{
		public string MapName { get; set; }
		public VBlock prefix_element { get; set; }
		public VBlock CMapRootElement { get; set; }
		public List<VBlock> CMapMeshes { get; set; } = new List<VBlock>();
		public List<VBlock> CMapEntities { get; set; } = new List<VBlock>();
		public List<VBlock> CMapPrefabs { get; set; } = new List<VBlock>();
		public List<VBlock> CMapGroups { get; set; } = new List<VBlock>(); // instances that are the same all share a group
		public List<VBlock> CMapInstances { get; set; } = new List<VBlock>(); //  each instance that links to a group
		public VBlock CMapWorld { get; set; }


		public VMap(string mapName, string[] lines)
		{
			MapName = mapName;

			ParseFromVmap(lines.ToList());
		}

		public void ParseFromVmap(List<string> lines)
		{
			var numOfBracketsInside = 0;

            for (int i = 0; i < lines.Count; i++)
			{
				var lineFormatted = GetFormattedLine(ref numOfBracketsInside, lines[i]);

				if (lineFormatted == null)
					continue;

				switch (lineFormatted)
				{
					case "$prefix_element$":
						prefix_element = VBlockExtensions.GetNewVBlock(lineFormatted, lines, i);
						break;
					case "CMapRootElement":
						CMapRootElement = VBlockExtensions.GetNewVBlock(lineFormatted, lines, i);
						break;
					case "CMapMesh":
						CMapMeshes.Add(VBlockExtensions.GetNewVBlock(lineFormatted, lines, i));
						break;
					case "CMapEntity":
						CMapEntities.Add(VBlockExtensions.GetNewVBlock(lineFormatted, lines, i));
						break;
					case "CMapPrefab":
						CMapPrefabs.Add(VBlockExtensions.GetNewVBlock(lineFormatted, lines, i));
						break;
					case "CMapGroup":
						CMapGroups.Add(VBlockExtensions.GetNewVBlock(lineFormatted, lines, i));
						break;
					case "CMapInstance":
						CMapInstances.Add(VBlockExtensions.GetNewVBlock(lineFormatted, lines, i));
						break;
					case "CMapWorld":
						CMapWorld = VBlockExtensions.GetNewVBlock(lineFormatted, lines, i);
						break;
				}
			}

			CMapWorld = GetWorldInSpecificVmap();
		}

        public VBlock GetWorldInSpecificVmap()
        {
            var mapWorld = CMapWorld;

            if (mapWorld == null && CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world") != null)
                mapWorld = CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world");

            return mapWorld;
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