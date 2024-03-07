namespace KeyValues2Parser.Models
{
	public class ParsedVMapData
	{
		public SelectionSetsInVmap SelectionSetsInMainVmap { get; set; }
        public Dictionary<Guid, SelectionSetsInVmap> SelectionSetsInPrefabByPrefabEntityId { get; set; }
        public VMapContents VMapContents { get; set; }

		public ParsedVMapData(
			SelectionSetsInVmap selectionSetsInMainVmap,
			Dictionary<Guid, SelectionSetsInVmap> selectionSetsInPrefabByPrefabEntityId,
			VMapContents vmapContents)
		{
			SelectionSetsInMainVmap = selectionSetsInMainVmap;
			SelectionSetsInPrefabByPrefabEntityId = selectionSetsInPrefabByPrefabEntityId;
			VMapContents = vmapContents;
		}
	}
}
