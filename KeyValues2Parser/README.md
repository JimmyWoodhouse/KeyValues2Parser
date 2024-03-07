A library for parsing through kv2 files.

For parsing VMAP files for example, you will need to decode the file to a kv2 format first, using 'dmxconvert.exe' (this is provided by Valve).
Tested with Counter-Strike 2 VMAP files only.

Data should be retrieved by calling the GetParsedVMapData() method in ParsedVMapDataGatherer.cs.
You need to provide 2 arguments to this method, -game and -vmapFilepath, explained more below.



Example code:


using Configuration.Constants;
using Configuration.Models;
using Configuration;
using KeyValues2Parser;

namespace YourProject
{
	public static class ExampleClass
	{
        // takes 2 arguments: -game and -vmapFilepath
        // -game is the path to your '...\game\csgo' folder. Eg: 'C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo'
        // -vmapFilepath is the path to the vmap file that you are parsing
        // Take a look in SetArgs() in GameConfigurationValues.cs
        static void Main(string[] args)
        {
            GetVmapRequiredData(args);
        }
        
        private static void GetVmapRequiredData(string[] args)
        {
            var parsedVMapData = ParsedVMapDataGatherer.GetParsedVMapData(args);

            List<VBlock> allWorldMeshes = parsedVMapData.VMapContents.AllWorldMeshes
            List<VBlock> allEntities = parsedVMapData.VMapContents.AllEntities
            List<VBlock> allInstanceGroups = parsedVMapData.VMapContents.AllInstanceGroups
            List<VBlock> allInstances = parsedVMapData.VMapContents.AllInstances
            List<VBlock> allPrefabs = parsedVMapData.VMapContents.AllPrefabs
            
            Console.WriteLine();
            Console.WriteLine("Getting required data from the main vmap and prefabs...");

            // prefabs contents
            if (ConfigurationSorter.prefabEntityIdsByVmap != null && ConfigurationSorter.prefabEntityIdsByVmap.Any())
            {
                foreach (var prefabEntityIdByVmap in ConfigurationSorter.prefabEntityIdsByVmap)
                {
                    var prefabEntityId = prefabEntityIdByVmap.Value;
                    var prefab = prefabEntityIdByVmap.Key;

                    AllEntitiesAndHiddenEntityMeshIdsInSpecificVmap allEntitiesAndHiddenEntityMeshIdsInPrefab = ConfigurationSorter.GetAllEntitiesInSpecificVmap(prefab, ConfigurationSorter.hiddenElementIdsInMainVmap) ?? new(prefab.MapName);
                    List<VBlock> allWorldMeshesInPrefab = ConfigurationSorter.GetAllWorldMeshesInSpecificVmap(prefab, ConfigurationSorter.hiddenElementIdsInMainVmap, allEntitiesAndHiddenEntityMeshIdsInPrefab.hiddenEntityMeshIds);
                    //List<VBlock> allMeshEntitiesInPrefab = ConfigurationSorter.GetAllMeshEntitiesInListOfEntities(allEntitiesAndHiddenEntityMeshIdsInPrefab.allEntities, ConfigurationSorter.hiddenElementIdsInMainVmap);
                    List<VBlock> allInstanceGroupsInPrefab = ConfigurationSorter.GetAllInstanceGroupsInSpecificVmap(prefab, ConfigurationSorter.hiddenElementIdsInMainVmap);
                    List<VBlock> allInstancesInPrefab = ConfigurationSorter.GetAllInstancesInSpecificVmap(prefab, ConfigurationSorter.hiddenElementIdsInMainVmap);

                    // change the ids
                    foreach (var entity in allEntitiesAndHiddenEntityMeshIdsInPrefab.allEntities)
                    {
                        var oldEntityId = Guid.Parse(entity.Variables["id"]);
                        var newEntityId = Guid.NewGuid();

                        // replaces the Id in the entity itself
                        entity.Variables.Remove("id");
                        entity.Variables.Add("id", newEntityId.ToString());

                        // replaces the Id that the selection set contains, to ensure that it points to the new Id instead
                        foreach (var selectionSet in selectionSetsInPrefabByPrefabEntityId[prefabEntityIdByVmap.Value].GetAllInList())
						{
                            if (selectionSet == null)
                                continue;

                            if (selectionSet.SelectedObjectIds.Any(x => x.Equals(oldEntityId)))
                            {
                                selectionSet.SelectedObjectIds?.RemoveAll(x => x.Equals(oldEntityId));
                                selectionSet.SelectedObjectIds?.Add(newEntityId);
                            }

                            if (selectionSet.MeshIds.Any(x => x.Equals(oldEntityId))) // this might break face selection sets
                            {
                                selectionSet.MeshIds?.RemoveAll(x => x.Equals(oldEntityId));
                                selectionSet.MeshIds?.Add(newEntityId);
                            }
                        }

                        // the meshes inside the entity
                        var entityMeshes = entity.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                            .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh" && y.InnerBlocks != null));

                        foreach (var entityMesh in entityMeshes)
                        {
                            var oldEntityMeshId = Guid.Parse(entityMesh.Variables["id"]);
                            var newEntityMeshId = Guid.NewGuid();

                            // replaces the Id in the entity mesh itself
                            entityMesh.Variables.Remove("id");
                            entityMesh.Variables.Add("id", newEntityMeshId.ToString());

                            // replaces the Id that the selection set contains, to ensure that it points to the new Id instead
                            foreach (var selectionSet in selectionSetsInPrefabByPrefabEntityId[prefabEntityIdByVmap.Value].GetAllInList())
						    {
                                if (selectionSet == null)
                                    continue;

                                if (selectionSet.SelectedObjectIds.Any(x => x.Equals(oldEntityMeshId)))
                                {
                                    selectionSet.SelectedObjectIds?.RemoveAll(x => x.Equals(oldEntityMeshId));
                                    selectionSet.SelectedObjectIds?.Add(newEntityMeshId);
                                }

                                if (selectionSet.MeshIds.Any(x => x.Equals(oldEntityMeshId))) // this might break face selection sets
                                {
                                    selectionSet.MeshIds?.RemoveAll(x => x.Equals(oldEntityMeshId));
                                    selectionSet.MeshIds?.Add(newEntityMeshId);
                                }
                            }
                        }
                        //
                    }
                    foreach (var mesh in allWorldMeshesInPrefab)
                    {
                        var oldMeshId = Guid.Parse(mesh.Variables["id"]);
                        var newMeshId = Guid.NewGuid();

                        // replaces the Id in the mesh itself
                        mesh.Variables.Remove("id");
                        mesh.Variables.Add("id", newMeshId.ToString());

                        // replaces the Id that the selection set contains, to ensure that it points to the new Id instead
                        foreach (var selectionSet in selectionSetsInPrefabByPrefabEntityId[prefabEntityIdByVmap.Value].GetAllInList())
						{
                            if (selectionSet == null)
                                continue;

                            if (selectionSet.SelectedObjectIds.Any(x => x.Equals(oldMeshId)))
                            {
                                selectionSet.SelectedObjectIds?.RemoveAll(x => x.Equals(oldMeshId));
                                selectionSet.SelectedObjectIds?.Add(newMeshId);
                            }

                            if (selectionSet.MeshIds.Any(x => x.Equals(oldMeshId))) // this might break face selection sets
                            {
                                selectionSet.MeshIds?.RemoveAll(x => x.Equals(oldMeshId));
                                selectionSet.MeshIds?.Add(newMeshId);
                            }
                        }
                    }
                    foreach (var instanceGroup in allInstanceGroupsInPrefab)
                    {
                        var oldInstanceGroupId = Guid.Parse(instanceGroup.Variables["id"]);
                        var newInstanceGroupId = Guid.NewGuid();

                        // replaces the Id in the instanceGroup itself
                        instanceGroup.Variables.Remove("id");
                        instanceGroup.Variables.Add("id", newInstanceGroupId.ToString());

                        // replaces the Id that the selection set contains, to ensure that it points to the new Id instead
                        foreach (var selectionSet in selectionSetsInPrefabByPrefabEntityId[prefabEntityIdByVmap.Value].GetAllInList())
						{
                            if (selectionSet == null)
                                continue;

                            if (selectionSet.SelectedObjectIds.Any(x => x.Equals(oldInstanceGroupId)))
                            {
                                selectionSet.SelectedObjectIds?.RemoveAll(x => x.Equals(oldInstanceGroupId));
                                selectionSet.SelectedObjectIds?.Add(newInstanceGroupId);
                            }

                            if (selectionSet.MeshIds.Any(x => x.Equals(oldInstanceGroupId))) // this might break face selection sets
                            {
                                selectionSet.MeshIds?.RemoveAll(x => x.Equals(oldInstanceGroupId));
                                selectionSet.MeshIds?.Add(newInstanceGroupId);
                            }
                        }
                    }
                    foreach (var instance in allInstancesInPrefab)
                    {
                        var oldInstanceId = Guid.Parse(instance.Variables["id"]);
                        var newInstanceId = Guid.NewGuid();

                        // replaces the Id in the instance itself
                        instance.Variables.Remove("id");
                        instance.Variables.Add("id", newInstanceId.ToString());

                        // replaces the Id that the selection set contains, to ensure that it points to the new Id instead
                        foreach (var selectionSet in selectionSetsInPrefabByPrefabEntityId[prefabEntityIdByVmap.Value].GetAllInList())
						{
                            if (selectionSet == null)
                                continue;

                            if (selectionSet.SelectedObjectIds.Any(x => x.Equals(oldInstanceId)))
                            {
                                selectionSet.SelectedObjectIds?.RemoveAll(x => x.Equals(oldInstanceId));
                                selectionSet.SelectedObjectIds?.Add(newInstanceId);
                            }

                            if (selectionSet.MeshIds.Any(x => x.Equals(oldInstanceId))) // this might break face selection sets
                            {
                                selectionSet.MeshIds?.RemoveAll(x => x.Equals(oldInstanceId));
                                selectionSet.MeshIds?.Add(newInstanceId);
                            }
                        }
                    }

                    allWorldMeshes.AddRange(allWorldMeshesInPrefab);
                    allEntities.AddRange(allEntitiesAndHiddenEntityMeshIdsInPrefab.allEntities);
                    allInstanceGroups.AddRange(allInstanceGroupsInPrefab);
                    allInstances.AddRange(allInstancesInPrefab);
                }
            }


            var allWorldMeshesInExampleSelectionSet = GetAllVBlocksInCorrectSelectionSet(allWorldMeshes, allInstances, selectionSetsInMainVmap.ExampleSelectionSet, selectionSetsInPrefabByPrefabEntityId.Values.Select(x => x.ExampleSelectionSet));


            // meshes
            var allSelectionSetsInVmapAndAllPrefabs = selectionSetsInMainVmap.GetAllInList().Concat(selectionSetsInPrefabByPrefabEntityId.Values.SelectMany(x => x.GetAllInList())).ToList();
            var allWorldMeshesInNoSpecificSelectionSet = GetAllMeshesInNoSpecificSelectionSet(allWorldMeshes, allSelectionSetsInVmapAndAllPrefabs);


            // mesh entities
            var buyzoneMeshEntities = ConfigurationSorter.GetEntitiesByClassname(allEntities, Classnames.Buyzone);


            // point entities
            var hostageEntities = ConfigurationSorter.GetEntitiesByClassnameInSelectionSetList(allEntities, Classnames.HostageList, selectionSetsInMainVmap, selectionSetsInPrefabByPrefabEntityId);


            // props
            var allEntitiesInExampleSelectionSet = ConfigurationSorter.GetEntitiesInSpecificSelectionSet(allEntities, SelectionSetNames.ExampleSelectionSetName, selectionSetsInMainVmap, selectionSetsInPrefabByPrefabEntityId);

            Console.WriteLine("Finished getting required data from the main vmap and prefabs");
        }


        private static List<VBlock> GetAllVBlocksInCorrectSelectionSet(List<VBlock> vBlocksSearchingThrough, List<VBlock> allInstances, VSelectionSet selectionSetInMainVmap, IEnumerable<VSelectionSet> selectionSetsInEachPrefab)
        {
            return
                (from x in vBlocksSearchingThrough
                where (selectionSetInMainVmap != null &&
                        (selectionSetInMainVmap.SelectedObjectIds.Any(y => y.Equals(Guid.Parse(x.Variables.First(z => z.Key == "id").Value))) ||
                            selectionSetInMainVmap.MeshIds.Any(y => y.Equals(Guid.Parse(x.Variables.First(z => z.Key == "id").Value))) ||
                            selectionSetInMainVmap.SelectedObjectIds.Any(y => allInstances.Any(z => Guid.Parse(z.Variables.First(z => z.Key == "id").Value).Equals(y))) ||
                            selectionSetInMainVmap.MeshIds.Any(y => allInstances.Any(z => Guid.Parse(z.Variables.First(z => z.Key == "id").Value).Equals(y))))) ||
                    selectionSetsInEachPrefab.Any(y =>
                        y != null &&
                            (y.SelectedObjectIds.Any(z => z.Equals(Guid.Parse(x.Variables.First(z => z.Key == "id").Value))) ||
                            y.MeshIds.Any(z => z.Equals(Guid.Parse(x.Variables.First(z => z.Key == "id").Value))) ||
                            y.SelectedObjectIds.Any(y => allInstances.Any(z => Guid.Parse(z.Variables.First(z => z.Key == "id").Value).Equals(y))) ||
                            y.MeshIds.Any(y => allInstances.Any(z => Guid.Parse(z.Variables.First(z => z.Key == "id").Value).Equals(y)))))
                select x).Distinct().ToList()
                ?? new List<VBlock>();
        }


        private static List<VBlock> GetAllMeshesInNoSpecificSelectionSet(
            List<VBlock> allWorldMeshesInVmap,
            List<VSelectionSet> selectionSetsToExclude)
		{
            selectionSetsToExclude.RemoveAll(x => x == null);

            if (selectionSetsToExclude != null && selectionSetsToExclude.Any())
            {
                foreach (var selectionSetToExclude in selectionSetsToExclude)
                {
                    allWorldMeshesInVmap.RemoveAll(x => selectionSetToExclude.SelectedObjectIds.Any(y => y.Equals(x.Id)));
                }
            }

            return allWorldMeshesInVmap ?? new();
        }


        private static IEnumerable<VBlock> GetMeshesByTextureName(
            IEnumerable<VBlock> allWorldMeshesInVmap,
            string textureName,
            IEnumerable<VBlock> allWorldMeshesInSpecificSelectionSet = null)
		{
            var allWorldMeshes = (from x in allWorldMeshesInVmap
                    from y in x.InnerBlocks
                    where y.Id == "meshData"
                    from z in y.Arrays
                    where z.Id == "materials"
                    from a in z.AllLinesInArrayByLineSplit
                    where a.ToLower().Replace("materials/", string.Empty).Replace(".vmat", string.Empty) == textureName.ToLower()
                    select x).Distinct().ToList();

            if (allWorldMeshesInSpecificSelectionSet != null && allWorldMeshesInSpecificSelectionSet.Any())
                allWorldMeshes.AddRange(allWorldMeshesInSpecificSelectionSet);

            allWorldMeshes = allWorldMeshes.Distinct().ToList();

            return allWorldMeshes ?? new();
        }


        private static IEnumerable<VBlock> GetMeshEntityMeshesByTextureName(
            IEnumerable<VBlock> allMeshEntities,
            string textureName,
			IEnumerable<VBlock> allMeshEntityMeshesInSpecificSelectionSet = null)
		{
            var allMeshEntityMeshes = (from x in allMeshEntities
                    from y in x.InnerBlocks
                    where y.Id == "entity_properties"
                    from y2 in x.Arrays
                    where y2.Id == "children"
                    from z2 in y2.InnerBlocks
                    where z2.Id == "CMapMesh"
                    from a2 in z2.InnerBlocks
                    where a2.Id == "meshData"
                    from b2 in a2.Arrays
                    where b2.Id == "materials"
                    from c2 in b2.AllLinesInArrayByLineSplitUnformatted
                    where c2.ToLower().Replace("materials/", string.Empty).Replace(".vmat", string.Empty) == textureName.ToLower()
                    select z2).Distinct().ToList();

			if (allMeshEntityMeshesInSpecificSelectionSet != null && allMeshEntityMeshesInSpecificSelectionSet.Any())
				allMeshEntityMeshes.AddRange(allMeshEntityMeshesInSpecificSelectionSet);

            allMeshEntityMeshes = allMeshEntityMeshes.Distinct().ToList();

            return allMeshEntityMeshes ?? new();
		}


        private static IEnumerable<VBlock> GetMeshEntityMeshesByClassname(IEnumerable<VBlock> allMeshEntities, string classname)
        {
            return (from x in allMeshEntities
                    from y in x.InnerBlocks
                    where y.Id == "entity_properties"
                    where y.Variables.Any(z => z.Key == "classname" && z.Value.ToLower() == classname.ToLower())
                    from y2 in x.Arrays
                    where y2.Id == "children"
                    from z2 in y2.InnerBlocks
                    where z2.Id == "CMapMesh"
                    select z2).Distinct() ?? new List<VBlock>();
        }
	}
}
