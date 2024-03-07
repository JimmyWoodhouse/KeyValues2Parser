using KeyValues2Parser.ParsingKV2;

namespace KeyValues2Parser.Models
{
	public class AllEntitiesAndHiddenEntityMeshIdsInSpecificVmap
    {
        public string MapName;
        public List<VBlock> allEntities;
        public List<Guid> hiddenEntityMeshIds;


        public AllEntitiesAndHiddenEntityMeshIdsInSpecificVmap(string mapName, List<VBlock> allEntities, List<Guid> hiddenEntityMeshIds)
        {
            MapName = mapName;
            this.allEntities = allEntities;
            this.hiddenEntityMeshIds = hiddenEntityMeshIds;
        }


        public AllEntitiesAndHiddenEntityMeshIdsInSpecificVmap(string mapName)
        {
            MapName = mapName;
        }
    }
}
