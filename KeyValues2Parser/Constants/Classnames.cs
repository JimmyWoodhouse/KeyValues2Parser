namespace KeyValues2Parser.Constants
{
	public static class Classnames
    {
        public static readonly string Buyzone = "func_buyzone";
        public static readonly string Bombsite = "func_bomb_target";
        public static readonly string RescueZone = "func_hostage_rescue";
        public static readonly List<string> HostageList = new() { "info_hostage_spawn", "hostage_entity" };
        public static readonly string CTSpawn = "info_player_counterterrorist";
        public static readonly string TSpawn = "info_player_terrorist";

        public static readonly string FuncBrush = "func_brush";
        public static readonly string FuncDoor = "func_door";
        public static readonly string FuncDoorRotating = "func_door_rotating";
        public static readonly string FuncLadder = "func_ladder";
        public static readonly string TriggerHurt = "trigger_hurt";

        public static readonly string InfoOverlay = "info_overlay";

		public static readonly string PropStatic = "prop_static";
		public static readonly List<string> PropDynamicList = new() { "prop_dynamic", "prop_dynamic_override" };
		public static readonly List<string> PropPhysicsList = new() { "prop_physics", "prop_physics_override", "prop_physics_multiplayer" };

        public static readonly string Prefab = "Prefab";


		public static List<string> GetAllClassnames()
        {
            List<string> classnames =
			[
				Buyzone,
                Bombsite,
                RescueZone,
                CTSpawn,
                TSpawn,

                FuncBrush,
                FuncDoor,
                FuncDoorRotating,
                FuncLadder,
                TriggerHurt,

                InfoOverlay,

                Prefab,

                PropStatic,
                .. HostageList,
                .. PropDynamicList,
                .. PropPhysicsList,
            ];

            return classnames;
        }
    }
}
