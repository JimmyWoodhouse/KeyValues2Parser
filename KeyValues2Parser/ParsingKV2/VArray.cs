namespace KeyValues2Parser.ParsingKV2
{
	public class VArray
	{
		public VArray(string id)
		{
			Id = id;
		}

		public VArray(string id, List<string> allLinesInArray)
		{
			Id = id;

			AllLinesInArrayByLineSplitUnformatted = allLinesInArray ?? new();

			CheckForInnerBlocks();
		}

        // Used when found in instances
		public VArray(VArray varray)
		{
            Id = varray.Id.ToString();

			foreach (var line in varray.AllLinesInArrayByLineSplitUnformatted)
			{
				AllLinesInArrayByLineSplitUnformatted.Add(line.ToString());
			}

			foreach (var innerBlock in varray.InnerBlocks)
			{
				InnerBlocks.Add(new VBlock(innerBlock));
			}
		}

		private void CheckForInnerBlocks()
		{
			var numOfBracketsInside = 0;

			var lines = AllLinesInArrayByLineSplitUnformatted;

            for (int i = 0; i < lines.Count - 1; i++)
			{
				if (lines[i+1].Trim().Replace("\"", string.Empty) == "{")
				{
					var lineFormatted = VMap.GetFormattedLine(ref numOfBracketsInside, lines[i]);

					InnerBlocks.Add(VBlockExtensions.GetNewVBlock(lineFormatted, lines, i));
				}
			}
		}

		public void SetAllLinesInArrayByLineSplit(List<string> newValues)
		{
			AllLinesInArrayByLineSplitUnformatted.Clear();

			foreach (var newValue in newValues)
			{
				AllLinesInArrayByLineSplitUnformatted.Add(string.Concat("\"", newValue, "\""));
			}
		}


        public string Id { get; set; }
		public List<string> AllLinesInArrayByLineSplitUnformatted { get; set; } = new();
		public List<string> AllLinesInArrayByLineSplit { get { return AllLinesInArrayByLineSplitUnformatted?.Select(x => x?.Trim()?.Replace($"\"", string.Empty))?.ToList() ?? new(); } }
		public int NumberOfVmapLinesInside { get { return AllLinesInArrayByLineSplitUnformatted.Count; } }
        public List<VBlock> InnerBlocks { get; set; } = new();
	}
}
