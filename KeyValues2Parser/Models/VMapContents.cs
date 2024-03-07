using KeyValues2Parser.ParsingKV2;

namespace KeyValues2Parser.Models
{
	public class VMapContents
	{
		public List<VBlock> AllEntities;
		public List<VBlock> AllWorldMeshes;
		//public List<VBlock> AllMeshEntities;
		public List<VBlock> AllPrefabs;
		public List<VBlock> AllInstanceGroups;
		public List<VBlock> AllInstances;

		public VMapContents(
			List<VBlock> allEntities,
			List<VBlock> allWorldMeshes,
			/*List<VBlock> allMeshEntities,*/
			List<VBlock> allPrefabs,
			List<VBlock> allInstanceGroups,
			List<VBlock> allInstances)
		{
			AllEntities = allEntities;
			AllWorldMeshes = allWorldMeshes;
			//AllMeshEntities = allMeshEntities;
			AllPrefabs = allPrefabs;
			AllInstanceGroups = allInstanceGroups;
			AllInstances = allInstances;
		}
	}
}
