using KeyValues2Parser.Constants;
using KeyValues2Parser.Enums;
using KeyValues2Parser.Models;
using KeyValues2Parser.ParsingKV2;
using System.Diagnostics;

namespace KeyValues2Parser
{
	public static class ConfigurationSorter
	{
        public static VMap vmap;

        public static List<string> allDecodedVmapFilepaths = new();

        public static readonly Dictionary<VMap, Guid> prefabEntityIdsByVmap = new();

        public static string vmapDecodedFolderPath => GameConfigurationValues.vmapFilepathDirectory;

        public static string dmxConvertFilepath;

        public static List<Guid> hiddenElementIdsInMainVmap { get; set; } = new();
        public static Dictionary<Guid, List<Guid>> hiddenElementIdsInPrefabByPrefabEntityId { get; set; } = new();

        public static bool includeHiddenObjects = true;


        public static bool HandleArgs(string[] args)
        {
            // add quotes to param values if necessary
            for (int i = 0; i < args.Length; i++)
            {
                if (GameConfigurationValues.allArgumentNamesAndNumOfFollowingInputs.Keys.Any(x => x.Any(y => y.ToLower() == args[i].ToLower())))
                {
                    args[i] = args[i].ToLower();
                }

                if (i == 0)
                    continue;

                if (!args[i].StartsWith("\"") && GameConfigurationValues.allArgumentNamesAndNumOfFollowingInputs.Any(x => x.Key.Any(y => y.ToLower() == args[i - 1].ToLower())) && GameConfigurationValues.allArgumentNamesAndNumOfFollowingInputs.FirstOrDefault(x => x.Key.Any(y => y.ToLower() == args[i].ToLower())).Value > 1)
                {
                    args[i] = "\"" + args[i];
                }

                if (!args[i].EndsWith("\"") &&
                        (i == args.Length - 1 ||
                            (i < args.Length - 1 && GameConfigurationValues.allArgumentNamesAndNumOfFollowingInputs.Any(x => x.Key.Any(y => y.ToLower() == args[i - 1].ToLower())))) &&
                        GameConfigurationValues.allArgumentNamesAndNumOfFollowingInputs.FirstOrDefault(x => x.Key.Any(y => y.ToLower() == args[i].ToLower())).Value > 1)
                {
                    args[i] += "\"";
                }
            }


            var argsSetCorrectly = GameConfigurationValues.SetArgs(args);


            if (!argsSetCorrectly || !GameConfigurationValues.VerifyAllValuesSet())
            {
                Console.WriteLine("Game configuration filepaths missing. Potentially too many parameters given. Check the compile configuration's parameters.");
                return false;
            }

            if (!argsSetCorrectly || GameConfigurationValues.binFolderPath.Split(@"\").Reverse().Skip(1).FirstOrDefault() != "bin" || !GameConfigurationValues.binFolderPath.Replace("/", @"\").Replace(@"\\", @"\").Contains(@"\game\bin"))
            {
                Console.WriteLine(@"bin folder set incorrectly");
                return false;
            }

            return true;
        }


        public static VMapContents? SetVmapAndContentsAndConfigurationValues(string dmxConvertFilepath, string vmapDecodedFolderPath)
        {
            var successfullySetVmap = SetVmap(dmxConvertFilepath, vmapDecodedFolderPath);
            if (!successfullySetVmap)
                return null;


            SetHiddenElementIdsInMainVmap();
            SetHiddenElementIdsInPrefabsByPrefabEntityId();


            // main vmap contents
            AllEntitiesAndHiddenEntityMeshIdsInSpecificVmap mainMapEntitiesAndHiddenEntityMeshIds = GetAllEntitiesInSpecificVmap(vmap, hiddenElementIdsInMainVmap) ?? new(vmap.MapName); // needs to be called after GetAllEntitiesInSpecificVmap() because certain keyvalues are be used inside this method
            List<VBlock> mainMapWorldMeshes = GetAllWorldMeshesInSpecificVmap(vmap, hiddenElementIdsInMainVmap, mainMapEntitiesAndHiddenEntityMeshIds.hiddenEntityMeshIds);
            //List<VBlock> mainMapMeshEntities = GetAllMeshEntitiesInListOfEntities(mainMapEntitiesAndHiddenEntityMeshIds.allEntities, hiddenElementIdsInMainVmap);
            List<VBlock> mainMapPrefabs = GetAllPrefabsInSpecificVmap(vmap, hiddenElementIdsInMainVmap);

            mainMapEntitiesAndHiddenEntityMeshIds.allEntities = mainMapEntitiesAndHiddenEntityMeshIds.allEntities.Distinct().ToList();
            mainMapWorldMeshes = mainMapWorldMeshes.Distinct().ToList();
            //mainMapMeshEntities = mainMapMeshEntities.Distinct().ToList();
            mainMapPrefabs = mainMapPrefabs.Distinct().ToList();

            // main vmap contents
            List<VBlock> allEntities = new();
            List<VBlock> allWorldMeshes = new();
            //List<VBlock> allMeshEntities = new();
            List<VBlock> allPrefabs = new();
            //

            allEntities.AddRange(mainMapEntitiesAndHiddenEntityMeshIds.allEntities);
            allWorldMeshes.AddRange(mainMapWorldMeshes);
            //allMeshEntities.AddRange(mainMapMeshEntities);
            allPrefabs.AddRange(mainMapPrefabs);
            //

            // sort out instances
            List<VBlock> allInstanceGroups = GetAllInstanceGroupsInSpecificVmap(vmap, hiddenElementIdsInMainVmap);
            List<VBlock> allInstances = GetAllInstancesInSpecificVmap(vmap, hiddenElementIdsInMainVmap);

            GetAllInstances(ref allInstanceGroups, ref allInstances);

            LinkAllChildrenElementsIdsThatNeedSearchingFor(allEntities, allWorldMeshes, allInstanceGroups, allInstances);


            if (allInstances.Any())
            {
                Console.WriteLine($"Parsing instances in vmap '{GameConfigurationValues.vmapName}'...");
                var successfullyParsedInstances = SortInstances(GameConfigurationValues.vmapName, ref allEntities, ref allWorldMeshes, ref allPrefabs, allInstanceGroups, allInstances);
                if (!successfullyParsedInstances)
                {
                    return null;
                }
                Console.WriteLine();
                Console.WriteLine($"Finished parsing instances in vmap '{GameConfigurationValues.vmapName}'");
            }
            else
            {
                Console.WriteLine($"No instances to parse in vmap '{GameConfigurationValues.vmapName}'");
            }
            //


            return new VMapContents(
                allEntities,
                allWorldMeshes,
                //allMeshEntities,
                allPrefabs,
                allInstanceGroups,
                allInstances
            );
        }


        public static void LinkAllChildrenElementsIdsThatNeedSearchingFor(List<VBlock> allEntities, List<VBlock> allWorldMeshes, List<VBlock> allInstanceGroups, List<VBlock> allInstances)
        {
            Console.WriteLine("Linking child element ids...");
            Console.WriteLine("If you are using meshes that have lots of faces (therefore vertices), this might take a while...");

            foreach (var entity in allEntities)
            {
                var childrenLines = entity.Arrays.First(x => x.Id == "children").AllLinesInArrayByLineSplit ?? new List<string>();
                var childrenElementsIdsThatNeedSearchingFor = childrenLines.Any() ? childrenLines.Where(x => x.Split(" ")[0].ToLower() == "element").Select(x => Guid.Parse(x.Split(" ")[1].Trim())).ToList() : new List<Guid>();

                foreach (var elementId in childrenElementsIdsThatNeedSearchingFor)
                {
                    var meshVBlock = allWorldMeshes.Where(x => x.Variables.FirstOrDefault(y => y.Key.ToLower() == "id").Value.Equals(elementId.ToString())).FirstOrDefault();
                    if (meshVBlock != null)
                    {
                        entity.Arrays.First(x => x.Id == "children").InnerBlocks.Add(meshVBlock);
                    }

                    var entityVBlock = allEntities.Where(x => x.Variables.FirstOrDefault(y => y.Key.ToLower() == "id").Value.Equals(elementId.ToString())).FirstOrDefault();
                    if (entityVBlock != null)
                    {
                        entity.Arrays.First(x => x.Id == "children").InnerBlocks.Add(entityVBlock);
                    }
                }

                entity.Arrays.First(x => x.Id == "children").InnerBlocks = entity.Arrays.First(x => x.Id == "children").InnerBlocks.Distinct().ToList();
            }

            foreach (var instanceGroup in allInstanceGroups)
            {
                var childrenLines = instanceGroup.Arrays.First(x => x.Id == "children").AllLinesInArrayByLineSplit ?? new List<string>();
                var childrenElementsIdsThatNeedSearchingFor = childrenLines.Any() ? childrenLines.Where(x => x.Split(" ")[0].ToLower() == "element").Select(x => Guid.Parse(x.Split(" ")[1].Trim())).ToList() : new List<Guid>();

                foreach (var elementId in childrenElementsIdsThatNeedSearchingFor)
                {
                    var meshVBlock = allWorldMeshes.Where(x => x.Variables.FirstOrDefault(y => y.Key.ToLower() == "id").Value.Equals(elementId.ToString())).FirstOrDefault();
                    if (meshVBlock != null)
                    {
                        instanceGroup.Arrays.First(x => x.Id == "children").InnerBlocks.Add(meshVBlock);
                    }

                    var entityVBlock = allEntities.Where(x => x.Variables.FirstOrDefault(y => y.Key.ToLower() == "id").Value.Equals(elementId.ToString())).FirstOrDefault();
					if (entityVBlock != null)
					{
                        instanceGroup.Arrays.First(x => x.Id == "children").InnerBlocks.Add(entityVBlock);
                    }
                }

                instanceGroup.Arrays.First(x => x.Id == "children").InnerBlocks = instanceGroup.Arrays.First(x => x.Id == "children").InnerBlocks.Distinct().ToList();
            }

            foreach (var instance in allInstances)
            {
                var childrenLines = instance.Arrays.First(x => x.Id == "children").AllLinesInArrayByLineSplit ?? new List<string>();
                var childrenElementsIdsThatNeedSearchingFor = childrenLines.Any() ? childrenLines.Where(x => x.Split(" ")[0].ToLower() == "element").Select(x => Guid.Parse(x.Split(" ")[1].Trim())).ToList() : new List<Guid>();

                foreach (var elementId in childrenElementsIdsThatNeedSearchingFor)
                {
                    var meshVBlock = allWorldMeshes.Where(x => x.Variables.FirstOrDefault(y => y.Key.ToLower() == "id").Value.Equals(elementId.ToString())).FirstOrDefault();
                    if (meshVBlock != null)
                    {
                        instance.Arrays.First(x => x.Id == "children").InnerBlocks.Add(meshVBlock);
                    }

                    var entityVBlock = allEntities.Where(x => x.Variables.FirstOrDefault(y => y.Key.ToLower() == "id").Value.Equals(elementId.ToString())).FirstOrDefault();
                    if (entityVBlock != null)
                    {
                        instance.Arrays.First(x => x.Id == "children").InnerBlocks.Add(entityVBlock);
                    }
                }

                instance.Arrays.First(x => x.Id == "children").InnerBlocks = instance.Arrays.First(x => x.Id == "children").InnerBlocks.Distinct().ToList();
            }

            Console.WriteLine("Finished linking child element ids...");
        }


        public static void GetAllInstances(ref List<VBlock> allInstanceGroups, ref List<VBlock> allInstances)
        {
            foreach (var instance in allInstances)
            {
                var instanceGroup = allInstanceGroups.FirstOrDefault(x => x.Variables.FirstOrDefault(y => y.Key.ToLower() == "id").Value.Equals(instance.Variables.FirstOrDefault(y => y.Key.ToLower() == "target").Value));
                if (instanceGroup == null)
                {
                    Console.WriteLine($"Instance with origin {instance.Variables.FirstOrDefault(x => x.Key.ToLower() == "origin")} has no matching instance group. Skipping.");
                    continue;
                }

                var instanceInstanceGroups = instanceGroup.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapGroup")?.ToList() ?? new();
                var instanceInstances = instanceGroup.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapInstance")?.ToList() ?? new();

                GetAllInstances(ref instanceInstanceGroups, ref instanceInstances);

                allInstanceGroups.AddRange(instanceInstanceGroups);
                instanceInstances.AddRange(instanceInstances);
            }
        }


        public static bool SortInstances(string vmapName, ref List<VBlock> allEntities, ref List<VBlock> allWorldMeshes, ref List<VBlock> allPrefabs, List<VBlock> allInstanceGroupsInVmap, List<VBlock> allInstancesInVmap, Vertices? parentOriginOverride = null, Angle? parentAnglesOverride = null, int numOrRecursiveLoops = 1)
        {
            if (parentOriginOverride == null)
                parentOriginOverride = new Vertices(0,0,0);

            if (parentAnglesOverride == null)
                parentAnglesOverride = new Angle(0,0,0);

            foreach (var instanceInVmap in allInstancesInVmap)
            {
                var instanceGroup = allInstanceGroupsInVmap.FirstOrDefault(x => x.Variables.FirstOrDefault(y => y.Key.ToLower() == "id").Value.Equals(instanceInVmap.Variables.FirstOrDefault(y => y.Key.ToLower() == "target").Value));
                if (instanceGroup == null)
                {
                    Console.WriteLine($"Instance with origin {instanceInVmap.Variables.FirstOrDefault(x => x.Key.ToLower() == "origin")} has no matching instance group. Skipping.");
                    continue;
                }


                var instanceGroupEntities = instanceGroup.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapEntity")?.ToList() ?? new();
                var allMeshesInsideInstanceGroupEntities = instanceGroupEntities?
                    .SelectMany(x => x.Arrays.Where(y => y.Id == "children" && y.InnerBlocks != null)?
                        .SelectMany(y => y.InnerBlocks.Where(z => z.Id == "CMapMesh" && z.Arrays != null)));
                var instanceGroupMeshes = instanceGroup.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapMesh" && !allMeshesInsideInstanceGroupEntities.Any(y => y.Variables.FirstOrDefault(z => z.Key == "id").Value.Equals(x.Variables.FirstOrDefault(z => z.Key == "id").Value)))?.ToList() ?? new();
                var instanceGroupPrefabs = instanceGroup.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapPrefab")?.ToList() ?? new();
                var instanceGroupInstanceGroups = instanceGroup.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapGroup")?.ToList() ?? new();
                var instanceGroupInstances = instanceGroup.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapInstance")?.ToList() ?? new();


                List<VBlock> instanceMeshes = new();
                List<VBlock> instanceEntities = new();
                List<VBlock> instancePrefabs = new();
                List<VBlock> instanceInstanceGroups = new();
                List<VBlock> instanceInstances = new();

                //
                var instanceName = instanceInVmap.Variables.FirstOrDefault(x => x.Key.ToLower() == "id").Value;

                var instanceOrigin = new Vertices(instanceInVmap.Variables.FirstOrDefault(x => x.Key.ToLower() == "origin").Value);
                var instanceGroupOrigin = new Vertices(instanceGroup.Variables.FirstOrDefault(x => x.Key.ToLower() == "origin").Value);

                var instanceAngles = new Angle(instanceInVmap.Variables.FirstOrDefault(x => x.Key.ToLower() == "angles").Value);
                var instanceGroupAngles = new Angle(instanceGroup.Variables.FirstOrDefault(x => x.Key.ToLower() == "angles").Value);

                var instanceDifferenceOrigin = instanceOrigin - instanceGroupOrigin;
                var instanceDifferenceAngles = instanceAngles - instanceGroupAngles;
                //

                foreach (var groupMesh in instanceGroupMeshes)
                {
                    allWorldMeshes.RemoveAll(x => x.Equals(groupMesh)); // removes any meshes that were added to the list already, as they need to be added with their origin & rotation offsets instead

                    var meshNew = new VBlock(groupMesh);

                    var allVerticesInMesh = meshNew?
                            .InnerBlocks.Where(z => z.Id == "meshData" && z.InnerBlocks != null)?
                                .SelectMany(z => z.InnerBlocks.Where(a => a.Id == "vertexData" && a.Arrays != null)?
                                    .SelectMany(a => a.Arrays.Where(b => b.Id == "streams" && b.InnerBlocks != null)?
                                        .SelectMany(b => b.InnerBlocks.Where(c => c.Id == "CDmePolygonMeshDataStream" && c.Arrays != null)?
                                            .Select(c => c.Arrays.FirstOrDefault(d => d.Id == "data"))))).FirstOrDefault();

                    if (allVerticesInMesh == null)
                        continue;

                    var allVerticesInMeshList = allVerticesInMesh.AllLinesInArrayByLineSplit.ToList();

                    if (!allVerticesInMeshList.Any())
                        continue;

                    MeshAndEntityAdjuster.MoveAndRotateAllMeshFacesInInstance(instanceOrigin, instanceAngles, instanceGroupOrigin, instanceGroupAngles, instanceDifferenceOrigin, instanceDifferenceAngles, new Vertices(meshNew.Variables.FirstOrDefault(x => x.Key.ToLower() == "origin").Value), new Angle(meshNew.Variables.FirstOrDefault(x => x.Key.ToLower() == "angles").Value), ref allVerticesInMeshList);

                    meshNew?
                        .InnerBlocks.Where(z => z.Id == "meshData" && z.InnerBlocks != null)?
                            .SelectMany(z => z.InnerBlocks.Where(a => a.Id == "vertexData" && a.Arrays != null)?
                                .SelectMany(a => a.Arrays.Where(b => b.Id == "streams" && b.InnerBlocks != null)?
                                    .SelectMany(b => b.InnerBlocks.Where(c => c.Id == "CDmePolygonMeshDataStream" && c.Arrays != null)?
                                        .Select(c => c.Arrays.FirstOrDefault(d => d.Id == "data"))))).FirstOrDefault()
                                            .SetAllLinesInArrayByLineSplit(allVerticesInMeshList);

                    instanceMeshes.Add(meshNew);
                }

                foreach (var groupEntity in instanceGroupEntities)
                {
                    allEntities.RemoveAll(x => x.Equals(groupEntity)); // removes any entities that were added to the list already, as they need to be added with their origin & rotation offsets instead

                    var entityNew = new VBlock(groupEntity);
                    entityNew.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.RemoveAll(x => x.Id == "CMapMesh"); // removes all of the templated meshes (so that they don't show in their original position in the radar)

                    var groupEntityMeshes = groupEntity.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                        .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh" && y.InnerBlocks != null));

                    foreach (var groupEntityMesh in groupEntityMeshes)
                    {
                        allWorldMeshes.RemoveAll(x => x.Equals(groupEntityMesh)); // removes any entity meshes that were added to the list already, as they need to be added with their origin & rotation offsets instead

                        var entityMesh = new VBlock(groupEntityMesh);

                        var allVerticesInEntityMesh = entityMesh?
                                .InnerBlocks.Where(z => z.Id == "meshData" && z.InnerBlocks != null)?
                                    .SelectMany(z => z.InnerBlocks.Where(a => a.Id == "vertexData" && a.Arrays != null)?
                                        .SelectMany(a => a.Arrays.Where(b => b.Id == "streams" && b.InnerBlocks != null)?
                                            .SelectMany(b => b.InnerBlocks.Where(c => c.Id == "CDmePolygonMeshDataStream" && c.Arrays != null)?
                                                .Select(c => c.Arrays.FirstOrDefault(d => d.Id == "data"))))).FirstOrDefault();

                        if (allVerticesInEntityMesh == null)
                            continue;

                        var allVerticesInEntityMeshList = allVerticesInEntityMesh.AllLinesInArrayByLineSplit.ToList();

                        if (!allVerticesInEntityMeshList.Any())
                            continue;

                        //var entityMeshOriginToUse = new Vertices(groupEntity.Variables.FirstOrDefault(x => x.Key.ToLower() == "origin").Value) - new Vertices(entityNew.Variables.FirstOrDefault(x => x.Key.ToLower() == "origin").Value);
                        //var entityMeshAnglesToUse = new Angle(groupEntity.Variables.FirstOrDefault(x => x.Key.ToLower() == "angles").Value) - new Angle(entityNew.Variables.FirstOrDefault(x => x.Key.ToLower() == "angles").Value);
                        MeshAndEntityAdjuster.MoveAndRotateAllMeshFacesInInstance(instanceOrigin, instanceAngles, instanceGroupOrigin, instanceGroupAngles, instanceDifferenceOrigin, instanceDifferenceAngles, new(0,0,0), new(0,0,0), ref allVerticesInEntityMeshList); // INSTANCE GROUP INSTEAD OF INSTANCE ??????

                        entityMesh?
                            .InnerBlocks.Where(z => z.Id == "meshData" && z.InnerBlocks != null)?
                                .SelectMany(z => z.InnerBlocks.Where(a => a.Id == "vertexData" && a.Arrays != null)?
                                    .SelectMany(a => a.Arrays.Where(b => b.Id == "streams" && b.InnerBlocks != null)?
                                        .SelectMany(b => b.InnerBlocks.Where(c => c.Id == "CDmePolygonMeshDataStream" && c.Arrays != null)?
                                            .Select(c => c.Arrays.FirstOrDefault(d => d.Id == "data"))))).FirstOrDefault()
                                                .SetAllLinesInArrayByLineSplit(allVerticesInEntityMeshList);

                        AddNewVBlockToChildrenArray(entityNew, entityMesh);
                    }

                    instanceEntities.Add(entityNew);
                }

                foreach (var groupPrefab in instanceGroupPrefabs)
                {
                    allPrefabs.RemoveAll(x => x.Equals(groupPrefab)); // removes any prefabs that were added to the list already, as they need to be added with their origin & rotation offsets instead

                    var prefabNew = new VBlock(groupPrefab);
                    // no need to remove the original meshes and entities from prefab because they aren't in this vmap, they're in the prefab's vmap

                    var currentOriginPrefab = prefabNew.Variables.FirstOrDefault(x => x.Key == "origin").Value.ToString();
                    var currentAnglesPrefab = prefabNew.Variables.FirstOrDefault(x => x.Key == "angles").Value.ToString();

                    prefabNew.Variables.Remove("origin");
                    prefabNew.Variables.Remove("angles");

                    prefabNew.Variables.Add("origin", new Vertices(0,0,0).GetStringFormat());
                    prefabNew.Variables.Add("angles", (new Angle(currentAnglesPrefab) + instanceDifferenceAngles).GetStringFormat());

                    // used in SortPrefabs() to offset & rotate the meshes and entities correctly
                    // not done in here because the VMap files aren't read & parsed yet, and that is the only way to know what the prefab has inside of it
                    prefabNew.Variables.Add("fake_instance_origin_difference", instanceDifferenceOrigin.GetStringFormat());
                    prefabNew.Variables.Add("fake_instance_angles_difference", instanceDifferenceAngles.GetStringFormat());


                    // prefabs are sorted recursively, so their contents will be sorted later on
                    var groupPrefabMeshes = groupPrefab.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                        .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh" && y.InnerBlocks != null));

                    var groupPrefabEntities = groupPrefab.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                        .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapEntity" && y.InnerBlocks != null));

                    foreach (var groupPrefabMesh in groupPrefabMeshes)
                    {
                        var prefabMesh = new VBlock(groupPrefabMesh);

                        /*
                        var currentOriginPrefabMesh = prefabNew.Variables.FirstOrDefault(x => x.Key == "origin").Value.ToString();
                        var currentAnglesPrefabMesh = prefabNew.Variables.FirstOrDefault(x => x.Key == "angles").Value.ToString();

                        prefabNew.Variables.Remove("origin");
                        prefabNew.Variables.Remove("angles");

                        prefabNew.Variables.Add("origin", (new Vertices(currentOriginPrefabMesh) + instanceDifferenceOrigin).GetStringFormat());
                        prefabNew.Variables.Add("angles", (new Angle(currentAnglesPrefabMesh) + instanceDifferenceAngles).GetStringFormat());
                        */

                        AddNewVBlockToChildrenArray(prefabNew, prefabMesh);
                    }

                    foreach (var groupPrefabEntity in groupPrefabEntities)
                    {
                        var groupPrefabEntityMeshes = groupPrefabEntity.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                            .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh" && y.InnerBlocks != null));

                        /*
                        var currentOriginPrefabEntity = prefabNew.Variables.FirstOrDefault(x => x.Key == "origin").Value.ToString();
                        var currentAnglesPrefabEntity = prefabNew.Variables.FirstOrDefault(x => x.Key == "angles").Value.ToString();

                        prefabNew.Variables.Remove("origin");
                        prefabNew.Variables.Remove("angles");

                        prefabNew.Variables.Add("origin", (new Vertices(currentOriginPrefabEntity) + instanceDifferenceOrigin).GetStringFormat());
                        prefabNew.Variables.Add("angles", (new Angle(currentAnglesPrefabEntity) + instanceDifferenceAngles).GetStringFormat());
                        */

                        var prefabEntity = new VBlock(groupPrefabEntity);

                        foreach (var groupPrefabEntityMesh in groupPrefabEntityMeshes)
                        {
                            var prefabEntityMesh = new VBlock(groupPrefabEntityMesh);

                            /*
                            var currentOriginPrefabEntityMesh = prefabNew.Variables.FirstOrDefault(x => x.Key == "origin").Value.ToString();
                            var currentAnglesPrefabEntityMesh = prefabNew.Variables.FirstOrDefault(x => x.Key == "angles").Value.ToString();

                            prefabNew.Variables.Remove("origin");
                            prefabNew.Variables.Remove("angles");

                            prefabNew.Variables.Add("origin", (new Vertices(currentOriginPrefabEntityMesh) + instanceDifferenceOrigin).GetStringFormat());
                            prefabNew.Variables.Add("angles", (new Angle(currentAnglesPrefabEntityMesh) + instanceDifferenceAngles).GetStringFormat());
                            */

                            AddNewVBlockToChildrenArray(prefabEntity, prefabEntityMesh);
                        }

                        AddNewVBlockToChildrenArray(prefabNew, prefabEntity);
                    }

                    instancePrefabs.Add(prefabNew);
                }

                foreach (var groupInstanceGroup in instanceGroupInstanceGroups)
                {
                    instanceInstanceGroups.RemoveAll(x => x.Equals(groupInstanceGroup)); // removes any instance groups that were added to the list already, as they need to be added with their origin & rotation offsets instead

                    var instanceGroupNew = new VBlock(groupInstanceGroup);

                    var currentOrigin = instanceGroupNew.Variables.FirstOrDefault(x => x.Key == "origin").Value.ToString();
                    var currentAngles = instanceGroupNew.Variables.FirstOrDefault(x => x.Key == "angles").Value.ToString();

                    instanceGroupNew.Variables.Remove("origin");
                    instanceGroupNew.Variables.Remove("angles");

                    instanceGroupNew.Variables.Add("origin", (new Vertices(currentOrigin) + instanceDifferenceOrigin).GetStringFormat());
                    instanceGroupNew.Variables.Add("angles", (new Angle(currentAngles) + instanceDifferenceAngles).GetStringFormat());

                    instanceInstanceGroups.Add(instanceGroupNew);
                }

                instanceInstanceGroups = instanceInstanceGroups.Distinct().ToList();

                foreach (var groupInstance in instanceGroupInstances)
                {
                    instanceInstances.RemoveAll(x => x.Equals(groupInstance)); // removes any instances that were added to the list already, as they need to be added with their origin & rotation offsets instead

                    var instanceNew = new VBlock(groupInstance);

                    var currentOrigin = instanceNew.Variables.FirstOrDefault(x => x.Key == "origin").Value.ToString();
                    var currentAngles = instanceNew.Variables.FirstOrDefault(x => x.Key == "angles").Value.ToString();

                    instanceNew.Variables.Remove("origin");
                    instanceNew.Variables.Remove("angles");

                    instanceNew.Variables.Add("origin", (new Vertices(currentOrigin) + instanceDifferenceOrigin).GetStringFormat());
                    instanceNew.Variables.Add("angles", (new Angle(currentAngles) + instanceDifferenceAngles).GetStringFormat());

                    instanceInstances.Add(instanceNew);
                }

                instanceInstances = instanceInstances.Distinct().ToList();

                allEntities.AddRange(instanceEntities);
                allWorldMeshes.AddRange(instanceMeshes);
                allPrefabs.AddRange(instancePrefabs);

                allEntities = allEntities.Distinct().ToList();
                allWorldMeshes = allWorldMeshes.Distinct().ToList();
                allPrefabs = allPrefabs.Distinct().ToList();

                var indentationString = string.Empty;
                for (int i = 1; i <= numOrRecursiveLoops; i++)
                {
                    indentationString += "\t- ";
                }

                if (instancePrefabs.Any())
                {
                    Console.WriteLine(indentationString + $"Parsing prefabs in instance '{instanceName}' at origin '{instanceOrigin.GetStringFormat()}' inside vmap '{vmapName}'...");
                    var successfullyParsedPrefabs = SortPrefabs(vmapName, instancePrefabs, true, instanceInstanceGroups, instanceInstances, parentOriginOverride: instanceOrigin, numOrRecursiveLoops: numOrRecursiveLoops+1); // instanceDifferenceOrigin instead of instanceOrigin ??  Add on the instance angles too ??
                    if (!successfullyParsedPrefabs)
                    {
                        return false;
                    }
                    Console.WriteLine();
                    Console.WriteLine(indentationString + $"Finished parsing prefabs in instance '{instanceName}' at origin '{instanceOrigin.GetStringFormat()}' inside vmap '{vmapName}'");
                }
                else
                {
                    Console.WriteLine(indentationString + $"No prefabs to parse in instance '{instanceName}' at origin '{instanceOrigin.GetStringFormat()}' inside vmap '{vmapName}'");
                }


                // instance contents
                List<VBlock> allEntitiesInInstance = new();
                List<VBlock> allWorldMeshesInInstance = new();
                //List<VBlock> allMeshEntitiesInInstance = new();
                List<VBlock> allPrefabsInInstance = new();

                if (instanceInstances.Any())
                {
                    Console.WriteLine(indentationString + $"Parsing instances in instance '{instanceName}' at origin '{instanceOrigin.GetStringFormat()}' inside vmap '{vmapName}'...");
                    var successfullyParsedInstances = SortInstances(vmapName, ref allEntitiesInInstance, ref allWorldMeshesInInstance, ref allPrefabsInInstance, instanceInstanceGroups, instanceInstances, parentOriginOverride: instanceOrigin, numOrRecursiveLoops: numOrRecursiveLoops+1); // instanceDifferenceOrigin instead of instanceOrigin ??  Add on the instance angles too ??
                    if (!successfullyParsedInstances)
                    {
                        return false;
                    }
                    Console.WriteLine();
                    Console.WriteLine(indentationString + $"Finished parsing instances in instance '{instanceName}' at origin '{instanceOrigin.GetStringFormat()}' inside vmap '{vmapName}'");
                }
                else
                {
                    Console.WriteLine(indentationString + $"No instances to parse in instance '{instanceName}' at origin '{instanceOrigin.GetStringFormat()}' inside vmap '{vmapName}'");
                }
            }

            return true;
        }


        public static bool SortPrefabs(string parentVmapName, List<VBlock> allPrefabsInParentVMap, bool calledByAnInstance, List<VBlock>? allParentVmapInstanceGroups = null, List<VBlock>? allParentVmapInstances = null, Vertices? parentOriginOverride = null, Angle? parentAnglesOverride = null, int numOrRecursiveLoops = 1)
        {
            if (parentOriginOverride == null)
                parentOriginOverride = new Vertices(0,0,0);

            if (parentAnglesOverride == null)
                parentAnglesOverride = new Angle(0,0,0);

            Console.WriteLine();
            Console.WriteLine(string.Concat(allPrefabsInParentVMap.Count(), " prefab", allPrefabsInParentVMap.Count() == 1 ? string.Empty : "s", " found in ", parentVmapName, "..."));
            Console.WriteLine();

            // create Prefabs from the entity key values
            var prefabs = new List<Prefab>();
            foreach (var prefabEntity in allPrefabsInParentVMap)
            {
                var newPrefab = new Prefab(prefabEntity);

                if (newPrefab != null && !string.IsNullOrWhiteSpace(newPrefab.targetMapPath) && newPrefab.angles != null && newPrefab.origin != null)
                {
                    if (calledByAnInstance)
                    {
                        prefabs.Add(newPrefab);
                    }
                    else if (allParentVmapInstanceGroups != null && allParentVmapInstances != null) // don't add prefabs that are inside instances, because they are already sorted in SortInstances()
                    {
                        var prefabIsInAnInstance = false;

                        foreach (var parentVmapInstance in allParentVmapInstances)
                        {
                            var parentVmapInstanceGroup = allParentVmapInstanceGroups.FirstOrDefault(x => x.Variables.FirstOrDefault(y => y.Key.ToLower() == "id").Value.Equals(parentVmapInstance.Variables.FirstOrDefault(y => y.Key.ToLower() == "target").Value));
                            if (parentVmapInstanceGroup == null)
                            {
                                Console.WriteLine($"Instance with origin {parentVmapInstance.Variables.FirstOrDefault(x => x.Key.ToLower() == "origin")} has no matching instance group. Skipping.");
                                continue;
                            }

                            var parentVmapInstanceGroupPrefabs = parentVmapInstanceGroup.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapPrefab")?.ToList() ?? new();

                            foreach (var parentVmapGroupPrefab in parentVmapInstanceGroupPrefabs)
                            {
                                var id = Guid.Parse(parentVmapGroupPrefab.Variables.FirstOrDefault(x => x.Key == "id").Value.ToString());
                                if (id.Equals(newPrefab.originalId))
                                {
                                    prefabIsInAnInstance = true;
                                    break;
                                }
                            }

                            if (prefabIsInAnInstance)
                                break;
                        }

                        if (!prefabIsInAnInstance)
                        {
                            prefabs.Add(newPrefab);
                        }
                    }
                }
            }

            //List<string> prefabNamesSkipped = new();

            // Parse the prefab VMAPs
            foreach (var prefab in prefabs)
            {
                prefab.filepath = string.Concat(GameConfigurationValues.vmapFilepathDirectory, prefab.targetMapPath);

                if (!File.Exists(prefab.filepath))
                {
                    Console.WriteLine("Prefab filepath does not exist, skipping: " + prefab.filepath);
                    continue;
                }

                VMap newVmap = null;

                try
                {
                    // parse prefab vmap (and potentially decode it from dmx to keyvalues2)
                    string[] lines = Array.Empty<string>();

                    Console.WriteLine($"Decoding prefab VMAP to keyvalues2...: {prefab.filepath}");

                    // doesn't need to decode the prefab again if it has already been done before, but it DOES still need to know about the prefab entity being in this vmap
                    if (allDecodedVmapFilepaths.Any(x => x.ToLower() == prefab.filepath.ToLower()))
                    {
                        Console.WriteLine("Skipped decoding prefab as it has already previously been decoded: " + prefab.filepath);
                        //prefabNamesSkipped.Add(prefab.filepath);
                    }
                    else
                    {
                        bool successfullyDecompiledVmap = ConvertSpecificVmapFileToText(prefab.filepath, true, dmxConvertFilepath, vmapDecodedFolderPath);
                        if (successfullyDecompiledVmap)
                        {
                            Console.WriteLine($"Prefab VMAP decoded successfully: {prefab.filepath}");

                            var decodedFilepath = string.Concat(vmapDecodedFolderPath, @"\prefabs\", Path.GetFileName(prefab.filepath), ".txt");
                            lines = File.ReadAllLines(decodedFilepath);

                            allDecodedVmapFilepaths.Add(prefab.filepath);
                        }
                        else
                        {
                            Console.WriteLine($"Prefab VMAP decoding failed: {prefab.filepath}");
                        }
                    }
                    //

                    newVmap = new VMap(prefab.filepath, lines);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not read prefab vmap: {prefab.targetMapPath}, it is potentially locked due to saving, aborting");
                    continue;
                }

                if (newVmap == null)
                {
                    Console.WriteLine("Prefab vmap data was null, skipping. Entity ID: " + prefab.id);
                    continue;
                }


                // prefab vmap contents
                List<VBlock> allEntitiesInPrefab = new();
                List<VBlock> allWorldMeshesInPrefab = new();
                //List<VBlock> allMeshEntitiesInPrefab = new();
                List<VBlock> allPrefabsInPrefab = new();
                //

                var prefabHiddenElementIds = hiddenElementIdsInPrefabByPrefabEntityId.Any(x => x.Key.Equals(prefab.id)) ? hiddenElementIdsInPrefabByPrefabEntityId[prefab.id] : new();

                // sort out instances
                List<VBlock> allInstanceGroups = GetAllInstanceGroupsInSpecificVmap(newVmap, prefabHiddenElementIds);
                List<VBlock> allInstances = GetAllInstancesInSpecificVmap(newVmap, prefabHiddenElementIds);


                if (parentOriginOverride != new Vertices(0,0,0))
                {
                    foreach (var innerInstanceGroup in allInstanceGroups)
                    {
                        var currentOriginValue = innerInstanceGroup.Variables.First(x => x.Key == "origin").Value;
                        innerInstanceGroup.Variables.Remove("origin");
                        innerInstanceGroup.Variables.Add("origin", (new Vertices(currentOriginValue) + (parentOriginOverride ?? new Vertices(0,0,0))).GetStringFormat());
                    }
                    foreach (var innerInstance in allInstances)
                    {
                        var currentOriginValue = innerInstance.Variables.First(x => x.Key == "origin").Value;
                        innerInstance.Variables.Remove("origin");
                        innerInstance.Variables.Add("origin", (new Vertices(currentOriginValue) + (parentOriginOverride ?? new Vertices(0,0,0))).GetStringFormat());
                    }
                }

                if (parentAnglesOverride != new Angle(0,0,0))
                {
                    foreach (var innerInstanceGroup in allInstanceGroups)
                    {
                        var currentAnglesValue = innerInstanceGroup.Variables.First(x => x.Key == "angles").Value;
                        innerInstanceGroup.Variables.Remove("angles");
                        innerInstanceGroup.Variables.Add("angles", (new Angle(currentAnglesValue) + (parentAnglesOverride ?? new Angle(0,0,0))).GetStringFormat());
                    }
                    foreach (var innerInstance in allInstances)
                    {
                        var currentAnglesValue = innerInstance.Variables.First(x => x.Key == "angles").Value;
                        innerInstance.Variables.Remove("angles");
                        innerInstance.Variables.Add("angles", (new Angle(currentAnglesValue) + (parentAnglesOverride ?? new Angle(0,0,0))).GetStringFormat());
                    }
                }


                GetAllInstances(ref allInstanceGroups, ref allInstances);

                var indentationString = string.Empty;
                for (int i = 1; i <= numOrRecursiveLoops; i++)
                {
                    indentationString += "\t- ";
                }

                if (allInstances.Any())
                {
                    Console.WriteLine(indentationString + $"Parsing instances in prefab vmap '{prefab.targetMapPath}' at origin '{prefab.origin.GetStringFormat()}'...");
                    var successfullyParsedInstances = SortInstances(prefab.targetMapPath, ref allEntitiesInPrefab, ref allWorldMeshesInPrefab, ref allPrefabsInPrefab, allInstanceGroups, allInstances, parentOriginOverride: prefab.origin, numOrRecursiveLoops: numOrRecursiveLoops+1);
                    if (!successfullyParsedInstances)
                    {
                        return false;
                    }
                    Console.WriteLine();
                    Console.WriteLine(indentationString + $"Finished parsing instances in prefab vmap '{prefab.targetMapPath}' at origin '{prefab.origin.GetStringFormat()}'");
                }
                else
                {
                    Console.WriteLine(indentationString + $"No instances to parse in prefab vmap '{prefab.targetMapPath}' at origin '{prefab.origin.GetStringFormat()}'");
                }
                //

                // prefab vmap contents
                AllEntitiesAndHiddenEntityMeshIdsInSpecificVmap prefabMapEntitiesAndHiddenEntityMeshIds = GetAllEntitiesInSpecificVmap(newVmap, prefabHiddenElementIds) ?? new(vmap.MapName);
                List<VBlock> prefabMapWorldMeshes = GetAllWorldMeshesInSpecificVmap(newVmap, prefabHiddenElementIds, prefabMapEntitiesAndHiddenEntityMeshIds.hiddenEntityMeshIds);
                //List<VBlock> prefabMapMeshEntities = GetAllMeshEntitiesInListOfEntities(prefabMapEntitiesAndHiddenEntityMeshIds.allEntities, prefabHiddenElementIds);
                List<VBlock> prefabMapPrefabs = GetAllPrefabsInSpecificVmap(newVmap, prefabHiddenElementIds);

                prefabMapEntitiesAndHiddenEntityMeshIds.allEntities = prefabMapEntitiesAndHiddenEntityMeshIds.allEntities.Distinct().ToList();
                prefabMapWorldMeshes = prefabMapWorldMeshes.Distinct().ToList();
                //prefabMapMeshEntities = prefabMapMeshEntities.Distinct().ToList();
                prefabMapPrefabs = prefabMapPrefabs.Distinct().ToList();

                allEntitiesInPrefab.AddRange(prefabMapEntitiesAndHiddenEntityMeshIds.allEntities);
                allWorldMeshesInPrefab.AddRange(prefabMapWorldMeshes);
                //allMeshEntitiesInPrefab.AddRange(prefabMapMeshEntities);
                allPrefabsInPrefab.AddRange(prefabMapPrefabs);
                //


                if (parentOriginOverride != new Vertices(0,0,0))
                {
                    foreach (var innerEntity in prefabMapEntitiesAndHiddenEntityMeshIds.allEntities)
                    {
                        var currentOriginValue = innerEntity.Variables.First(x => x.Key == "origin").Value;
                        innerEntity.Variables.Remove("origin");
                        innerEntity.Variables.Add("origin", (new Vertices(currentOriginValue) + (parentOriginOverride ?? new Vertices(0,0,0))).GetStringFormat());
                    }
                    foreach (var innerMesh in prefabMapWorldMeshes)
                    {
                        var currentOriginValue = innerMesh.Variables.First(x => x.Key == "origin").Value;
                        innerMesh.Variables.Remove("origin");
                        innerMesh.Variables.Add("origin", (new Vertices(currentOriginValue) + (parentOriginOverride ?? new Vertices(0,0,0))).GetStringFormat());


                        var allVerticesInMesh = innerMesh?
                                .InnerBlocks.Where(z => z.Id == "meshData" && z.InnerBlocks != null)?
                                    .SelectMany(z => z.InnerBlocks.Where(a => a.Id == "vertexData" && a.Arrays != null)?
                                        .SelectMany(a => a.Arrays.Where(b => b.Id == "streams" && b.InnerBlocks != null)?
                                            .SelectMany(b => b.InnerBlocks.Where(c => c.Id == "CDmePolygonMeshDataStream" && c.Arrays != null)?
                                                .Select(c => c.Arrays.FirstOrDefault(d => d.Id == "data"))))).FirstOrDefault();

                        if (allVerticesInMesh == null)
                            continue;

                        var verticesList = allVerticesInMesh.AllLinesInArrayByLineSplit.ToList();

                        if (!verticesList.Any())
                            continue;

                        for (int i = 0; i < verticesList.Count; i++)
                        {
                            var originValue = new Vertices(innerMesh.Variables.First(x => x.Key == "origin").Value);

                            // temp add the origin offset
                            var verticesWithOriginOffset = MeshAndEntityAdjuster.GetVerticesFromString(verticesList[i]) + originValue; // + instanceGroupOrigin;
                            verticesList[i] = verticesWithOriginOffset.GetStringFormat(); // vertices are the distance from the origin by default

                            // rotate the vertices around the instance origin
                            verticesList[i] = MeshAndEntityAdjuster.MoveAndRotateVerticesInInstance(prefab.origin, prefab.angles, verticesList[i]);

                            // remove the temp added origin offset
                            var verticesWithOriginOffset2 = MeshAndEntityAdjuster.GetVerticesFromString(verticesList[i]) - originValue; // - instanceGroupOrigin;
                            verticesList[i] = verticesWithOriginOffset2.GetStringFormat(); // vertices are the distance from the origin by default
                        }
                    }
                    foreach (var innerPrefab in prefabMapPrefabs)
                    {
                        var currentOriginValue = innerPrefab.Variables.First(x => x.Key == "origin").Value;
                        innerPrefab.Variables.Remove("origin");
                        innerPrefab.Variables.Add("origin", (new Vertices(currentOriginValue) + (parentOriginOverride ?? new Vertices(0,0,0))).GetStringFormat());
                    }
                }

                if (parentAnglesOverride != new Angle(0,0,0))
                {
                    foreach (var innerEntity in prefabMapEntitiesAndHiddenEntityMeshIds.allEntities)
                    {
                        var currentAnglesValue = innerEntity.Variables.First(x => x.Key == "angles").Value;
                        innerEntity.Variables.Remove("angles");
                        innerEntity.Variables.Add("angles", (new Angle(currentAnglesValue) + (parentAnglesOverride ?? new Angle(0,0,0))).GetStringFormat());
                    }
                    foreach (var innerMesh in prefabMapWorldMeshes)
                    {
                        var currentAnglesValue = innerMesh.Variables.First(x => x.Key == "angles").Value;
                        innerMesh.Variables.Remove("angles");
                        innerMesh.Variables.Add("angles", (new Angle(currentAnglesValue) + (parentAnglesOverride ?? new Angle(0,0,0))).GetStringFormat());
                    }
                    foreach (var innerPrefab in prefabMapPrefabs)
                    {
                        var currentAnglesValue = innerPrefab.Variables.First(x => x.Key == "angles").Value;
                        innerPrefab.Variables.Remove("angles");
                        innerPrefab.Variables.Add("angles", (new Angle(currentAnglesValue) + (parentAnglesOverride ?? new Angle(0,0,0))).GetStringFormat());
                    }
                }


                // correct entity meshes' origins and angles
                foreach (var entity in allEntitiesInPrefab)
                {
                    // entity id is not changed

                    string originalOriginValue = null;

                    var origin = entity.Variables["origin"];
                    if (origin != null)
                    {
                        originalOriginValue = origin;
                        origin = MeshAndEntityAdjuster.MoveAndRotateVerticesInPrefab(prefab, origin);
                    }

                    ////
                    var entityMeshes = entity.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                        .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh" && y.InnerBlocks != null));

                    foreach (var entityMesh in entityMeshes)
                    {
                        var allVerticesInEntityMesh = entityMesh?
                                .InnerBlocks.Where(z => z.Id == "meshData" && z.InnerBlocks != null)?
                                    .SelectMany(z => z.InnerBlocks.Where(a => a.Id == "vertexData" && a.Arrays != null)?
                                        .SelectMany(a => a.Arrays.Where(b => b.Id == "streams" && b.InnerBlocks != null)?
                                            .SelectMany(b => b.InnerBlocks.Where(c => c.Id == "CDmePolygonMeshDataStream" && c.Arrays != null)?
                                                .Select(c => c.Arrays.FirstOrDefault(d => d.Id == "data"))))).FirstOrDefault();

                        if (allVerticesInEntityMesh == null)
                            continue;

                        var entityMeshOrigin = entityMesh.Variables.First(x => x.Key == "origin").Value;
                        var entityMeshAngles = entityMesh.Variables.First(x => x.Key == "angles").Value;
                        //var entityMeshScales = entityMesh.Variables.First(x => x.Key == "scales").Value; // TODO: should it be scaled? I think the vertices positions values are changed by Hammer instead when scaling

                        var allVerticesInEntityMeshList = allVerticesInEntityMesh.AllLinesInArrayByLineSplit.ToList();

                        if (!allVerticesInEntityMeshList.Any())
                            continue;

                        MeshAndEntityAdjuster.MoveAndRotateAllMeshFacesInPrefab(prefab, entityMeshOrigin, entityMeshAngles, ref allVerticesInEntityMeshList);

                        allVerticesInEntityMesh.SetAllLinesInArrayByLineSplit(allVerticesInEntityMeshList);
                    }
                    ////

                    // overlays (before the fake mesh is created, the vertices need rotating)
                    var entityProperties = entity.InnerBlocks.First(x => x.Id == "entity_properties");
                    if (entityProperties != null)
                    {
                        if (!string.IsNullOrWhiteSpace(originalOriginValue) && entityProperties.Variables.ContainsKey("classname") && (entityProperties.Variables["classname"].ToLower() == "info_overlay"))
                        {
                            var width = entityProperties.Variables["width"];
                            var height = entityProperties.Variables["height"];

                            if (width != null && height != null)
                            {
                                var pos1 = new Vertices(float.Parse(width, Globalization.Style, Globalization.Culture), float.Parse(height, Globalization.Style, Globalization.Culture), 0).GetStringFormat();
                                var pos2 = new Vertices(-float.Parse(width, Globalization.Style, Globalization.Culture), float.Parse(height, Globalization.Style, Globalization.Culture), 0).GetStringFormat();
                                var pos3 = new Vertices(-float.Parse(width, Globalization.Style, Globalization.Culture), -float.Parse(height, Globalization.Style, Globalization.Culture), 0).GetStringFormat();
                                var pos4 = new Vertices(float.Parse(width, Globalization.Style, Globalization.Culture), -float.Parse(height, Globalization.Style, Globalization.Culture), 0).GetStringFormat();

                                var allVerticesOffsetsInOverlay = new List<string>() { pos1, pos2, pos3, pos4 };
                                MeshAndEntityAdjuster.RotateOverlayVerticesInPrefabAndSetFakeVertices(prefab, entityProperties, allVerticesOffsetsInOverlay, originalOriginValue);
                            }
                        }
                    }
                }

                foreach (var mesh in allWorldMeshesInPrefab)
                {
                    ////
                    var allWorldMeshVertices = mesh.InnerBlocks.Where(x => x.Id == "meshData" && x.InnerBlocks != null)?
                        .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "vertexData" && y.Arrays != null)?
                            .SelectMany(y => y.Arrays.Where(z => z.Id == "streams" && z.InnerBlocks != null)?
                                .SelectMany(z => z.InnerBlocks.Where(a => a.Id == "CDmePolygonMeshDataStream" && a.Arrays != null)?
                                    .Select(a => a.Arrays.FirstOrDefault(b => b.Id == "data"))))).FirstOrDefault();

                    if (allWorldMeshVertices == null)
                        continue;

                    var meshOrigin = mesh.Variables.First(x => x.Key == "origin").Value;
                    var meshAngles = mesh.Variables.First(x => x.Key == "angles").Value;
                    //var meshScales = mesh.Variables.First(x => x.Key == "scales").Value; // TODO: should it be scaled? I think the vertices positions values are changed by Hammer instead when scaling

                    var allWorldMeshVerticesList = allWorldMeshVertices.AllLinesInArrayByLineSplit.ToList();

                    if (!allWorldMeshVerticesList.Any())
                        continue;

                    MeshAndEntityAdjuster.MoveAndRotateAllMeshFacesInPrefab(prefab, meshOrigin, meshAngles, ref allWorldMeshVerticesList);

                    allWorldMeshVertices.SetAllLinesInArrayByLineSplit(allWorldMeshVerticesList);
                    ////
                }

                // recursively sort out prefabs inside other vmaps
                if (allPrefabsInPrefab.Any())
                {
                    Console.WriteLine(indentationString + $"Parsing prefabs in prefab vmap '{prefab.targetMapPath}' at origin '{prefab.origin.GetStringFormat()}'...");
                    var successfullyParsedPrefabs = SortPrefabs(prefab.targetMapPath, allPrefabsInPrefab, false, parentOriginOverride: prefab.origin, numOrRecursiveLoops: numOrRecursiveLoops+1);
                    if (!successfullyParsedPrefabs)
                    {
                        return false;
                    }
                    Console.WriteLine();
                    Console.WriteLine(indentationString + $"Finished parsing prefabs in prefab vmap '{prefab.targetMapPath}' at origin '{prefab.origin.GetStringFormat()}'");
                }
                else
                {
                    Console.WriteLine(indentationString + $"No prefabs to parse in prefab vmap '{prefab.targetMapPath}' at origin '{prefab.origin.GetStringFormat()}'");
                }

                prefabEntityIdsByVmap.Add(newVmap, prefab.id);
            }

            var numOfPrefabsSuccessfullyParsed = prefabEntityIdsByVmap.Where(x => prefabs.Any(y => y.id.Equals(x.Value))).Count();

            Console.WriteLine();
            Console.WriteLine(string.Concat(numOfPrefabsSuccessfullyParsed, " prefab", numOfPrefabsSuccessfullyParsed == 1 ? string.Empty : "s", " successfully parsed in ", parentVmapName));

            var numOfPrefabsUnsuccessfullyParsed = prefabs.Count() - numOfPrefabsSuccessfullyParsed;
            if (numOfPrefabsUnsuccessfullyParsed < 0)
            {
                Console.WriteLine("Parsed more prefabs successfully than there are prefab entities, how?");
            }
            else if (numOfPrefabsUnsuccessfullyParsed > 0)
            {
                var unsuccessfulPrefabEntities = prefabs.Where(x => prefabEntityIdsByVmap.Values.All(y => y != x.id)).ToList();

                Console.WriteLine();
                Console.WriteLine(string.Concat(numOfPrefabsUnsuccessfullyParsed, " prefab", numOfPrefabsUnsuccessfullyParsed == 1 ? string.Empty : "s", " unsuccessfully parsed in ", parentVmapName, ":"));

                /*foreach (var prefab in unsuccessfulPrefabEntities)
                {
                    Console.WriteLine(string.Concat("Entity ID: ", prefab.id));
                }*/
            }

            return true;
        }



        private static void AddNewVBlockToChildrenArray(VBlock parentVBlock, VBlock childVBlock)
        {
            if (!parentVBlock.Arrays.Any(x => x.Id == "children"))
                parentVBlock.Arrays.Add(new VArray("children"));

            parentVBlock.Arrays.First(x => x.Id == "children").InnerBlocks.Add(childVBlock);
        }

        private static void SetHiddenElementIdsInMainVmap()
        {
            hiddenElementIdsInMainVmap = GetHiddenElementIdsInVmap(vmap);
        }


        private static void SetHiddenElementIdsInPrefabsByPrefabEntityId()
        {
            if (prefabEntityIdsByVmap != null && prefabEntityIdsByVmap.Any())
            {
                foreach (var prefab in prefabEntityIdsByVmap)
                {
                    var hiddenIds = GetHiddenElementIdsInVmap(prefab.Key);

                    if (hiddenElementIdsInPrefabByPrefabEntityId.ContainsKey(prefab.Value))
                        continue;

                    hiddenElementIdsInPrefabByPrefabEntityId.Add(prefab.Value, hiddenIds);
                }
            }
        }


        private static List<Guid> GetHiddenElementIdsInVmap(VMap vmap)
        {
            if (vmap.CMapRootElement == null)
                return new();

            var hiddenElementIdsInVmap = new List<Guid>();

            var nodesArray = (from x in vmap.CMapRootElement.InnerBlocks
                              where x.Id == "visbility" // misspelled in vmap
                              from y in x.Arrays
                              where y.Id == "nodes"
                              from z in y.AllLinesInArrayByLineSplit
                              where !string.IsNullOrWhiteSpace(z.Replace("element", string.Empty).Trim())
                              select Guid.Parse(z.Replace("element", string.Empty).Trim())).ToList();

            var hiddenFlagsArray = (from x in vmap.CMapRootElement.InnerBlocks
                                      where x.Id == "visbility" // misspelled in vmap
                                      from y in x.Arrays
                                      where y.Id == "hiddenFlags"
                                      from z in y.AllLinesInArrayByLineSplit
                                      select int.Parse(z, Globalization.Style, Globalization.Culture)).ToList();

            if (nodesArray.Count() != hiddenFlagsArray.Count())
            {
                Console.WriteLine($"Found {nodesArray.Count()} nodes but {hiddenFlagsArray.Count()} hiddenFlags values in vmap '{vmap.MapName}'. These should match. There might be an issue with showing/hiding the correct things here.");
            }

            for (int i = 0; i < nodesArray.Count(); i++)
            {
                if (hiddenFlagsArray.Count() < i) // failsafe incase there aren't the same number of values in both arrays, for some reason
                    break;

                if (hiddenFlagsArray[i] != (int)VisibilityHiddenFlags.Visible)
                {
                    hiddenElementIdsInVmap.Add(nodesArray[i]);
                }
            }

            return hiddenElementIdsInVmap;
        }


        private static bool SetVmap(string dmxConvertFilepath, string vmapDecodedFolderPath)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("Reading VMAP...");

                // parse main vmap (and potentially decode it from dmx to keyvalues2)
                string[] lines = Array.Empty<string>();

                Console.WriteLine("Decoding VMAP to keyvalues2...");

                bool successfullyDecompiledVmap = ConvertSpecificVmapFileToText(GameConfigurationValues.vmapFilepath, false, dmxConvertFilepath, vmapDecodedFolderPath);

                if (successfullyDecompiledVmap)
                {
                    Console.WriteLine("VMAP decoded successfully");

                    var decodedVmapFilepath = string.Concat(vmapDecodedFolderPath, @"\", Path.GetFileName(GameConfigurationValues.vmapFilepath), ".txt");
                    lines = File.ReadAllLines(decodedVmapFilepath);

                    allDecodedVmapFilepaths.Add(GameConfigurationValues.vmapFilepath);
                }
                else
                {
                    Console.WriteLine("VMAP decoding failed");
                    return false;
                }
                //

                Console.WriteLine();
                Console.WriteLine("Parsing VMAP contents...");

                vmap = new VMap(Path.GetFileName(GameConfigurationValues.vmapFilepath), lines);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not find, decode, or read main vmap. Check the filepath is correct and if the file is potentially locked due to saving or try closing Hammer. Aborting");
                return false;
            }

            if (vmap == null)
            {
                Console.WriteLine("Error parsing VMAP, aborting");
                return false;
            }

            Console.WriteLine();
            Console.WriteLine("VMAP parsed successfully");
            Console.WriteLine();

            return true;
        }

        public static bool ConvertSpecificVmapFileToText(string vmapFilepath, bool isPrefab, string dmxConvertFilepath, string vmapDecodedFolderPath)
		{
            if (!File.Exists(dmxConvertFilepath))
			{
				Console.WriteLine("dmxconvert.exe not found, cannot decompile vmaps. You may need to verify your game files.");
				return false;
            }

            if (!File.Exists(vmapFilepath))
            {
				Console.WriteLine($"VMap not found at {vmapFilepath}");
				return false;
            }

			var outputFilepath = isPrefab ?
                $"{vmapDecodedFolderPath}\\prefabs\\{Path.GetFileName(vmapFilepath)}.txt" :
                $"{vmapDecodedFolderPath}\\{Path.GetFileName(vmapFilepath)}.txt";

            DirectoryAndFileHelpers.CreateDirectoryIfDoesntExist(Directory.GetParent(outputFilepath).FullName);

			ProcessStartInfo startInfo = new()
			{
				FileName = dmxConvertFilepath,
				//CreateNoWindow = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				WindowStyle = ProcessWindowStyle.Hidden,
				Arguments = $"-i \"{vmapFilepath}\" -ie binary -o \"{outputFilepath}\" -oe keyvalues2 -of vmap"
			};

			Process.Start(startInfo).WaitForExit();

            return true;
		}


        public static AllEntitiesAndHiddenEntityMeshIdsInSpecificVmap GetAllEntitiesInSpecificVmap(VMap vmap, List<Guid> hiddenElementIds)
        {
            List<Guid> hiddenEntityMeshIds = new();

            var allEntitiesBeforeCheckingHiddenStatus = vmap.CMapEntities;
            if (vmap.CMapWorld != null)
            {
                var entitiesToAdd = vmap.CMapWorld.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapEntity");
                if (entitiesToAdd != null)
                    allEntitiesBeforeCheckingHiddenStatus.AddRange(entitiesToAdd);
            }
            else // should never be entered, because CMapWorld is set to 'vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")' in VMap.cs
            {
                var worldChildrenInnerBlocks = vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")?.Arrays.First(x => x.Id == "children").InnerBlocks ?? new List<VBlock>();
                allEntitiesBeforeCheckingHiddenStatus.AddRange(worldChildrenInnerBlocks.Where(x => x.Id == "CMapEntity"));
            }

            allEntitiesBeforeCheckingHiddenStatus = allEntitiesBeforeCheckingHiddenStatus.Distinct().ToList();


            //
            List<VBlock> allEntities = new();

            if (allEntitiesBeforeCheckingHiddenStatus != null && allEntitiesBeforeCheckingHiddenStatus.Any())
            {
                foreach (var entity in allEntitiesBeforeCheckingHiddenStatus)
                {
                    var entityId = Guid.Parse(entity.Variables.First(x => x.Key == "id").Value);

                    var entityMeshes = entity.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                        .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh" && y.InnerBlocks != null))?.ToList() ?? new();

                    if (includeHiddenObjects)
                    {
                        allEntities.Add(entity);
                    }
                    else
                    {
                        if (!hiddenElementIds.Any(x => x.Equals(entityId))) // checks if the element is hidden
                        {
                            allEntities.Add(entity);
                        }

                        foreach (var mesh in entityMeshes) // set all of the entity meshes to hidden if applicable
                        {
                            var meshIsHidden = mesh.InnerBlocks.First(x => x.Id == "meshData")
                                                        .InnerBlocks.First(x => x.Id == "faceData")
                                                            .Arrays.First(x => x.Id == "streams")
                                                                .InnerBlocks.First(x => x.Id == "CDmePolygonMeshDataStream" && x.Variables.Any(y => y.Key == "semanticName" && y.Value == "flags"))
                                                                    .Arrays.First(x => x.Id == "data")
                                                                        .AllLinesInArrayByLineSplit.All(x => int.Parse(x, Globalization.Style, Globalization.Culture) > 0); // 1 == quick hidden, 2 == in a hidden selection set   (TODO: treats all mesh faces in the mesh together, so all faces are shown UNLESS all are hidden)

                            if (meshIsHidden) // checks if the mesh faces are hidden
                            {
                                var hiddenEntityMeshId = Guid.Parse(mesh.Variables.First(x => x.Key == "id").Value);

                                hiddenEntityMeshIds.Add(hiddenEntityMeshId);
                            }
                        }
                    }
                }
            }
            //

            return new AllEntitiesAndHiddenEntityMeshIdsInSpecificVmap(vmap.MapName, allEntities.Distinct().ToList(), hiddenEntityMeshIds);
        }


        public static List<VBlock> GetAllWorldMeshesInSpecificVmap(VMap vmap, List<Guid> hiddenElementIds, List<Guid> hiddenEntityMeshFaceIds)
        {
            var allWorldMeshesBeforeCheckingHiddenStatus = vmap.CMapMeshes;
            if (vmap.CMapWorld != null)
            {
                var meshesToAdd = vmap.CMapWorld.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapMesh");
                if (meshesToAdd != null)
                    allWorldMeshesBeforeCheckingHiddenStatus.AddRange(meshesToAdd);
            }
            else // should never be entered, because CMapWorld is set to 'vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")' in VMap.cs
            {
                var worldChildrenInnerBlocks = vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")?.Arrays.First(x => x.Id == "children").InnerBlocks ?? new List<VBlock>();
                allWorldMeshesBeforeCheckingHiddenStatus.AddRange(worldChildrenInnerBlocks.Where(x => x.Id == "CMapMesh")); // Maybe they will never be here? No idea
            }

            allWorldMeshesBeforeCheckingHiddenStatus = allWorldMeshesBeforeCheckingHiddenStatus.Distinct().ToList();


            //
            List<VBlock> allWorldMeshes = new();

            foreach (var mesh in allWorldMeshesBeforeCheckingHiddenStatus)
            {
                var meshId = Guid.Parse(mesh.Variables.First(x => x.Key == "id").Value);

                if (includeHiddenObjects)
                {
                    allWorldMeshes.Add(mesh);
                }
                else if (!hiddenElementIds.Any(x => x.Equals(meshId)) && !hiddenEntityMeshFaceIds.Any(x => x.Equals(meshId))) // checks if the element is hidden, or if the faces are entity meshes and they were hidden
                {
                    var meshIsHidden = mesh.InnerBlocks.First(x => x.Id == "meshData")
                                                    .InnerBlocks.First(x => x.Id == "faceData")
                                                        .Arrays.First(x => x.Id == "streams")
                                                            .InnerBlocks.First(x => x.Id == "CDmePolygonMeshDataStream" && x.Variables.Any(y => y.Key == "semanticName" && y.Value == "flags"))
                                                                .Arrays.First(x => x.Id == "data")
                                                                    .AllLinesInArrayByLineSplit.All(x => int.Parse(x, Globalization.Style, Globalization.Culture) > 0); // 1 == quick hidden, 2 == in a hidden selection set   (TODO: treats all mesh faces in the mesh together, so all faces are shown UNLESS all are hidden)

                    if (!meshIsHidden) // checks if the mesh faces are hidden
                        allWorldMeshes.Add(mesh);
                }
            }
            //

            return allWorldMeshes.Distinct().ToList();
        }


        /*public static List<VBlock> GetAllMeshEntitiesInListOfEntities(List<VBlock> allEntities, List<Guid> hiddenElementIds)
        {
            var allMeshEntities = allEntities.SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh")).ToList();
            if (allMeshEntities != null && allMeshEntities.Any())
            {
                foreach (var meshToAdd in allMeshEntities)
                {
                    var meshToAddId = Guid.Parse(meshToAdd.Variables.First(x => x.Key == "id").Value);
                    if (includeHiddenObjects || !hiddenElementIds.Any(x => x.Equals(meshToAddId))) // checks if the element is hidden
                        allMeshEntities.Add(meshToAdd);
                }
            }

            return allMeshEntities;
        }*/


        public static List<VBlock> GetAllPrefabsInSpecificVmap(VMap vmap, List<Guid> hiddenElementIds)
        {
            var allPrefabsBeforeCheckingHiddenStatus = vmap.CMapPrefabs;
            if (vmap.CMapWorld != null)
            {
                var prefabsToAdd = vmap.CMapWorld.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapPrefab");
                if (prefabsToAdd != null)
                    allPrefabsBeforeCheckingHiddenStatus.AddRange(prefabsToAdd);
            }
            else // should never be entered, because CMapWorld is set to 'vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")' in VMap.cs
            {
                var worldChildrenInnerBlocks = vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")?.Arrays.First(x => x.Id == "children").InnerBlocks ?? new List<VBlock>();
                allPrefabsBeforeCheckingHiddenStatus.AddRange(worldChildrenInnerBlocks.Where(x => x.Id == "CMapPrefab"));
            }

            allPrefabsBeforeCheckingHiddenStatus = allPrefabsBeforeCheckingHiddenStatus.Distinct().ToList();


            //
            List<VBlock> allPrefabs = new();

            foreach (var prefabToAdd in allPrefabsBeforeCheckingHiddenStatus)
            {
                var prefabToAddId = Guid.Parse(prefabToAdd.Variables.First(x => x.Key == "id").Value);
                if (includeHiddenObjects || !hiddenElementIds.Any(x => x.Equals(prefabToAddId))) // checks if the element is hidden
                    allPrefabs.Add(prefabToAdd);
            }
            //

            return allPrefabs.Distinct().ToList();
        }


        public static List<VBlock> GetAllInstanceGroupsInSpecificVmap(VMap vmap, List<Guid> hiddenElementIds)
        {
            var allInstanceGroupsBeforeCheckingHiddenStatus = vmap.CMapGroups;
            if (vmap.CMapWorld != null)
            {
                var instanceGroupsToAdd = vmap.CMapWorld.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapGroup");
                if (instanceGroupsToAdd != null)
                    allInstanceGroupsBeforeCheckingHiddenStatus.AddRange(instanceGroupsToAdd);
            }
            else // should never be entered, because CMapWorld is set to 'vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")' in VMap.cs
            {
                var worldChildrenInnerBlocks = vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")?.Arrays.First(x => x.Id == "children").InnerBlocks ?? new List<VBlock>();
                allInstanceGroupsBeforeCheckingHiddenStatus.AddRange(worldChildrenInnerBlocks.Where(x => x.Id == "CMapGroup"));
            }

            allInstanceGroupsBeforeCheckingHiddenStatus = allInstanceGroupsBeforeCheckingHiddenStatus.Distinct().ToList();


            //
            List<VBlock> allInstanceGroups = new();

            foreach (var instanceGroupToAdd in allInstanceGroupsBeforeCheckingHiddenStatus)
            {
                var instanceGroupToAddId = Guid.Parse(instanceGroupToAdd.Variables.First(x => x.Key == "id").Value);
                if (includeHiddenObjects || !hiddenElementIds.Any(x => x.Equals(instanceGroupToAddId))) // checks if the element is hidden
                    allInstanceGroups.Add(instanceGroupToAdd);
            }
            //

            return allInstanceGroups.Distinct().ToList();
        }


        public static List<VBlock> GetAllInstancesInSpecificVmap(VMap vmap, List<Guid> hiddenElementIds)
        {
            var allInstancesBeforeCheckingHiddenStatus = vmap.CMapInstances;
            if (vmap.CMapWorld != null)
            {
                var instancesToAdd = vmap.CMapWorld.Arrays.FirstOrDefault(x => x.Id == "children")?.InnerBlocks.Where(x => x.Id == "CMapInstance");
                if (instancesToAdd != null)
                    allInstancesBeforeCheckingHiddenStatus.AddRange(instancesToAdd);
            }
            else // should never be entered, because CMapWorld is set to 'vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")' in VMap.cs
            {
                var worldChildrenInnerBlocks = vmap.CMapRootElement.InnerBlocks.FirstOrDefault(x => x.Id == "world")?.Arrays.First(x => x.Id == "children").InnerBlocks ?? new List<VBlock>();
                allInstancesBeforeCheckingHiddenStatus.AddRange(worldChildrenInnerBlocks.Where(x => x.Id == "CMapInstance"));
            }

            allInstancesBeforeCheckingHiddenStatus = allInstancesBeforeCheckingHiddenStatus.Distinct().ToList();


            //
            List<VBlock> allInstances = new();

            foreach (var instanceToAdd in allInstancesBeforeCheckingHiddenStatus)
            {
                var instanceToAddId = Guid.Parse(instanceToAdd.Variables.First(x => x.Key == "id").Value);
                if (includeHiddenObjects || !hiddenElementIds.Any(x => x.Equals(instanceToAddId))) // checks if the element is hidden
                    allInstances.Add(instanceToAdd);
            }
            //

            return allInstances.Distinct().ToList();
        }


        public static IEnumerable<VBlock> GetEntitiesByClassname(IEnumerable<VBlock> allEntities, string classname)
        {
            return (from x in allEntities
                    from y in x.InnerBlocks
                    where y.Id == "entity_properties"
                    where y.Variables.Any(z => z.Key == "classname" && z.Value.ToLower() == classname.ToLower())
                    select x).Distinct() ?? new List<VBlock>();
        }


        public static IEnumerable<VBlock> GetEntitiesByClassnameList(IEnumerable<VBlock> allEntities, List<string> classnameList)
        {
            List<VBlock> entitiesMatching = new();

            foreach (var classname in classnameList)
            {
                entitiesMatching.AddRange(GetEntitiesByClassname(allEntities, classname));
            }

            return entitiesMatching;
        }


        public static IEnumerable<VBlock> GetEntitiesByClassnameInSelectionSet(IEnumerable<VBlock> allEntities, string classname, SelectionSetsInVmap selectionSetsInMainVmap, Dictionary<Guid, SelectionSetsInVmap> selectionSetsInPrefabEntityIds)
        {
            return (from x in allEntities
                    from y in x.InnerBlocks
                    where y.Id == "entity_properties"
                    where y.Variables.Any(z => z.Key == "classname" && z.Value.ToLower() == classname.ToLower())
                    from y2 in x.Variables
                    where y2.Key == "id"
                    where SelectionSetNames.ExampleSelectionSetName.Any(z2 => selectionSetsInMainVmap.GetSelectionSet(z2) != null && (selectionSetsInMainVmap.GetSelectionSet(z2).SelectedObjectIds.Any(a2 => a2.Equals(Guid.Parse(y2.Value))) || selectionSetsInMainVmap.GetSelectionSet(z2).MeshIds.Any(a2 => a2.Equals(Guid.Parse(y2.Value))))) ||
                        selectionSetsInPrefabEntityIds.Values.Any(z2 => z2.ExampleSelectionSet != null && (z2.ExampleSelectionSet.SelectedObjectIds.Any(a2 => a2.Equals(Guid.Parse(y2.Value))) || z2.ExampleSelectionSet.MeshIds.Any(a2 => a2.Equals(Guid.Parse(y2.Value)))))
                    select x).Distinct() ?? new List<VBlock>();
        }


        public static IEnumerable<VBlock> GetEntitiesByClassnameInSelectionSetList(IEnumerable<VBlock> allEntities, List<string> classnameList, SelectionSetsInVmap selectionSetsInMainVmap, Dictionary<Guid, SelectionSetsInVmap> selectionSetsInPrefabEntityIds)
        {
            List<VBlock> entitiesMatching = new();

            foreach (var classname in classnameList)
            {
                entitiesMatching.AddRange(GetEntitiesByClassnameInSelectionSet(allEntities, classname, selectionSetsInMainVmap, selectionSetsInPrefabEntityIds));
            }

            return entitiesMatching;
        }


        public static IEnumerable<VBlock> GetEntitiesInSpecificSelectionSet(IEnumerable<VBlock> allEntities, List<string> selectionSetNamesList, SelectionSetsInVmap selectionSetsInMainVmap, Dictionary<Guid, SelectionSetsInVmap> selectionSetsInPrefabEntityIds)
        {
            return (from x in allEntities
                    from y in x.InnerBlocks
                    where y.Id == "entity_properties"
                    where y.Variables.Any(z => z.Key == "classname" && Classnames.GetAllClassnames().Any(x => x.ToLower() == z.Value.ToLower()))
                    from y2 in x.Variables
                    where y2.Key == "id"
                    where selectionSetNamesList.Any(z2 => selectionSetsInMainVmap.GetSelectionSet(z2) != null && (selectionSetsInMainVmap.GetSelectionSet(z2).SelectedObjectIds.Any(a2 => a2.Equals(Guid.Parse(y2.Value))) || selectionSetsInMainVmap.GetSelectionSet(z2).MeshIds.Any(a2 => a2.Equals(Guid.Parse(y2.Value))))) ||
                        selectionSetNamesList.Any(z2 => selectionSetsInPrefabEntityIds.Values.Any(a2 => a2.GetSelectionSet(z2) != null && (a2.GetSelectionSet(z2).SelectedObjectIds.Any(b2 => b2.Equals(Guid.Parse(y2.Value))) || a2.GetSelectionSet(z2).MeshIds.Any(b2 => b2.Equals(Guid.Parse(y2.Value))))))
                    select x).Distinct() ?? new List<VBlock>();
        }


        public static string GetFloatInEnglishFormatString(float? num)
        {
            if (num == null)
                return string.Empty;

            return ((float)num).ToString("R", Globalization.Culture);
        }
	}
}
