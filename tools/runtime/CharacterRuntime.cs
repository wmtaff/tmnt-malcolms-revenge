// Original opt-in presentation profile. Native roster and gameplay identity are retained.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace Malcolm.Runtime {
    internal static class CharacterRuntime {
        private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static Action<string> log;
        private static string displayName;
        private static FieldInfo selectedField, nameField;
        private static PropertyInfo overrideProperty;
        private static readonly HashSet<object> reportedPanels = new HashSet<object>();
        private static readonly HashSet<object> reportedPlayers = new HashSet<object>();

        internal static void Install(Harmony harmony, Assembly game, Assembly engine, string artDirectory, string validatedDisplayName, Action<string> logger) {
            if (harmony == null || game == null || engine == null || logger == null) throw new ArgumentNullException("Character runtime dependencies");
            if (String.IsNullOrWhiteSpace(validatedDisplayName)) throw new ArgumentException("Character display name is required");
            foreach (char value in validatedDisplayName) if (Char.IsControl(value)) throw new ArgumentException("Character display name cannot contain controls");
            if (String.IsNullOrWhiteSpace(artDirectory) || !Directory.Exists(artDirectory)) throw new DirectoryNotFoundException("Malcolm art directory is required");
            Type panel = game.GetType("Paris.Game.Menu.CharacterSelectionPanel", true);
            Type player = game.GetType("Paris.Game.Actor.Player", true);
            Type info = game.GetType("Paris.Game.System.GamePlayerInfo", true);
            selectedField = panel.GetField("_selectedCharacter", InstanceFlags);
            nameField = panel.GetField("_characterName", InstanceFlags);
            if (selectedField == null || nameField == null) throw new MissingFieldException("Native character selection fields changed");
            overrideProperty = nameField.FieldType.GetProperty("OverrideString", InstanceFlags);
            if (overrideProperty == null || overrideProperty.PropertyType != typeof(string) || !overrideProperty.CanWrite)
                throw new MissingMemberException("Native character name override changed");
            MethodInfo update = panel.GetMethod("UpdateCharacterSelection", InstanceFlags | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
            MethodInfo load = player.GetMethod("LoadPlayerInfo", InstanceFlags | BindingFlags.DeclaredOnly, null, new[] { info }, null);
            if (update == null || load == null || update.ReturnType != typeof(void) || load.ReturnType != typeof(void))
                throw new MissingMethodException("Native character selection/player lifecycle changed");
            log = logger;
            displayName = validatedDisplayName;
            harmony.Patch(update, null, new HarmonyMethod(typeof(CharacterRuntime), "SelectionUpdated"));
            harmony.Patch(load, null, new HarmonyMethod(typeof(CharacterRuntime), "PlayerBound"));
            log("MALCOLM_CHARACTER_READY selection=" + displayName + " nativeIdentity=Leo nativeMoveset=Leonardo art=" + Path.GetFullPath(artDirectory));
        }

        private static object Property(object value, string name) {
            if (value == null) return null;
            PropertyInfo property = value.GetType().GetProperty(name, InstanceFlags);
            return property == null ? null : property.GetValue(value, null);
        }

        internal static bool IsMalcolmCharacterInfo(object value) {
            if (value == null || value.GetType().FullName != "Paris.Game.Data.CharacterInfo") return false;
            string template = Convert.ToString(Property(value, "ActorTemplate")).Replace('\\', '/');
            return Convert.ToString(Property(value, "InternalName")) == "Leo"
                && String.Equals(template, "Player/Leo", StringComparison.OrdinalIgnoreCase)
                && Convert.ToString(Property(value, "AnimationProjectName")) == "Leonardo";
        }

        internal static bool IsMalcolmPlayer(object value) {
            return value != null && value.GetType().FullName == "Paris.Game.Actor.Leonardo"
                && IsMalcolmCharacterInfo(Property(value, "CharacterInfo"));
        }

        private static void SelectionUpdated(object __instance) {
            // Native UpdateCharacterSelection clears this override before choosing each character.
            // Keeping InternalName unchanged also preserves all native UI content lookup paths.
            if (!IsMalcolmCharacterInfo(selectedField.GetValue(__instance))) return;
            object name = nameField.GetValue(__instance);
            if (name == null) throw new InvalidOperationException("Native Malcolm selection name control is missing");
            overrideProperty.SetValue(name, displayName, null);
            if (reportedPanels.Add(__instance)) log("MALCOLM_SELECTION_PRESENTED name=" + displayName + " nativeIdentity=Leo");
        }

        private static void PlayerBound(object __instance) {
            // Postfix only: the native player still initializes palette, progression and control.
            if (IsMalcolmPlayer(__instance) && reportedPlayers.Add(__instance))
                log("MALCOLM_PLAYER_BOUND nativeType=Paris.Game.Actor.Leonardo identity=Leo");
        }
    }
}
