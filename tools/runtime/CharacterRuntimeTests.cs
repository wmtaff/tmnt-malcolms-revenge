// Original synthetic native-shaped fixtures. Never compile this file into the launcher.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Paris.Game.Data {
    public class CharacterInfo {
        public string InternalName { get; set; }
        public string ActorTemplate { get; set; }
        public string AnimationProjectName { get; set; }
    }
}
namespace Paris.Game.System { public class GamePlayerInfo {} }
namespace Paris.Game.Actor {
    public class Player {
        public Paris.Game.Data.CharacterInfo CharacterInfo { get; set; }
        public int NativeLoadCount;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LoadPlayerInfo(Paris.Game.System.GamePlayerInfo info) { NativeLoadCount++; }
    }
    public class Leonardo : Player {}
    public class Raphael : Player {}
}
namespace Paris.Game.Menu {
    public class NameControl { public string OverrideString { get; set; } }
    public class CharacterSelectionPanel {
        private Paris.Game.Data.CharacterInfo _selectedCharacter;
        private NameControl _characterName = new NameControl();
        public string Label { get { return _characterName.OverrideString; } }
        public int NativeUpdateCount;
        public void Select(Paris.Game.Data.CharacterInfo info) { _selectedCharacter = info; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UpdateCharacterSelection() {
            NativeUpdateCount++;
            _characterName.OverrideString = ""; // Native method clears the override each update.
        }
    }
}
namespace Malcolm.Runtime {
    internal static class CharacterRuntimeTests {
        private static void Check(bool value, string reason) { if (!value) throw new Exception(reason); }
        private static Paris.Game.Data.CharacterInfo Donor() {
            return new Paris.Game.Data.CharacterInfo { InternalName="Leo", ActorTemplate="Player\\Leo", AnimationProjectName="Leonardo" };
        }
        public static int Main() {
            try {
                var donor=Donor();
                Check(CharacterRuntime.IsMalcolmCharacterInfo(donor), "Exact native donor rejected");
                Check(!CharacterRuntime.IsMalcolmCharacterInfo(null), "Null matched");
                var wrong=Donor(); wrong.ActorTemplate="Player\\Raphael";
                Check(!CharacterRuntime.IsMalcolmCharacterInfo(wrong), "Wrong native template matched");
                wrong=Donor(); wrong.InternalName="leo";
                Check(!CharacterRuntime.IsMalcolmCharacterInfo(wrong), "Different native identity matched");
                wrong=Donor(); wrong.AnimationProjectName="Raphael";
                Check(!CharacterRuntime.IsMalcolmCharacterInfo(wrong), "Wrong animation project matched");
                Check(!CharacterRuntime.IsMalcolmCharacterInfo(new object()), "Foreign object matched");
                var player=new Paris.Game.Actor.Leonardo { CharacterInfo=donor };
                Check(CharacterRuntime.IsMalcolmPlayer(player), "Native donor actor rejected");
                Check(!CharacterRuntime.IsMalcolmPlayer(new Paris.Game.Actor.Raphael { CharacterInfo=donor }), "Different actor type matched");

                var messages=new List<string>(); var harmony=new Harmony("Malcolm.CharacterRuntime.Tests");
                CharacterRuntime.Install(harmony, Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly(), Environment.CurrentDirectory, messages.Add);
                var panel=new Paris.Game.Menu.CharacterSelectionPanel(); panel.Select(donor); panel.UpdateCharacterSelection();
                Check(panel.Label=="Malcolm" && panel.NativeUpdateCount==1, "Real Harmony postfix did not present Malcolm");
                Check(donor.InternalName=="Leo" && donor.ActorTemplate=="Player\\Leo", "Native identity changed");
                panel.Select(wrong); panel.UpdateCharacterSelection();
                Check(panel.Label=="" && panel.NativeUpdateCount==2, "Other character label was overridden");
                panel.Select(donor); panel.UpdateCharacterSelection();
                Check(panel.Label=="Malcolm", "Reselection failed");
                player.LoadPlayerInfo(new Paris.Game.System.GamePlayerInfo());
                Check(player.NativeLoadCount==1, "Native player initialization skipped");
                Check(messages.Exists(delegate(string s) { return s.StartsWith("MALCOLM_PLAYER_BOUND"); }), "Binding evidence missing");
                harmony.UnpatchAll(harmony.Id);
                Console.WriteLine("CHARACTER_RUNTIME_SELF_TEST_PASS exact donor selection native lifecycle unchanged identity");
                return 0;
            } catch(Exception error) { Console.Error.WriteLine(error); return 1; }
        }
    }
}
