using KeyValues2Parser.Constants;

namespace KeyValues2Parser.ParsingKV2
{
	public  class VSelectionSet
	{
		public VSelectionSet(VBlock cMapSelectionSet)
		{
			if (cMapSelectionSet == null)
				return;

			Id = Guid.Parse(cMapSelectionSet.Variables.First(x => x.Key == "id").Value);

			SelectionSetName = cMapSelectionSet.Variables.FirstOrDefault(x => x.Key == "selectionSetName").Value.Trim();
			SelectedObjectIds = cMapSelectionSet.InnerBlocks.FirstOrDefault(x => x.Id == "selectionSetData")?.Arrays.FirstOrDefault(x => x.Id == "selectedObjects")?.AllLinesInArrayByLineSplit?.Select(x => Guid.Parse(x.Replace($"\"", string.Empty).Replace("element", string.Empty).Trim()))?.ToList() ?? new List<Guid>();
			FaceIds = cMapSelectionSet.InnerBlocks.FirstOrDefault(x => x.Id == "selectionSetData")?.Arrays.FirstOrDefault(x => x.Id == "faces")?.AllLinesInArrayByLineSplit?.Select(x => int.Parse(x, Globalization.Style, Globalization.Culture) % reset32BitCounterValue)?.ToList() ?? new List<int>();
			MeshIds = cMapSelectionSet.InnerBlocks.FirstOrDefault(x => x.Id == "selectionSetData")?.Arrays.FirstOrDefault(x => x.Id == "meshes")?.AllLinesInArrayByLineSplit?.Select(x => Guid.Parse(x.Replace($"\"", string.Empty).Replace("element", string.Empty).Trim()))?.ToList() ?? new List<Guid>();
		}

		public Guid Id;
		public string SelectionSetName;
		public List<Guid>? SelectedObjectIds; // Mesh selection sets
		public List<int>? FaceIds; // Face selection sets
		public List<Guid>? MeshIds; // Face selection sets

		private static readonly int max32BitCounterValue = 4194303; // "32" bit might be wrong terminology. it is (first number is uint32's reset number):   (4,294,967,296 / 1024) - 1   -=-  Also, this is: 2^22 - 1
		public static readonly int reset32BitCounterValue = max32BitCounterValue + 1; // sometimes faceId values can be exceedingly high and not map to an index number in the mesh itself. It is just going past max32BitCounterValue, so that just needs deducting
	}
}
