using Buzzfield.Core;
using Buzzfield.Game;
using Buzzfield.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Window "Buzzfield > Play Test Tools": shortcuts for trying the game by hand in the
    /// editor. In edit mode it opens Main and can start a new game; in play mode it adds
    /// honey, blooms the garden, refills pollen, speeds time up and shows the swipe hint again.
    /// </summary>
    public sealed class PlayTestWindow : EditorWindow
    {
        static readonly float[] Speeds = { 1f, 3f, 10f };
        static readonly string[] SpeedLabels = { "1x", "3x", "10x" };

        [MenuItem("Buzzfield/Play Test Tools", priority = 2)]
        public static void Open() => GetWindow<PlayTestWindow>("Play Test");

        /// <summary>
        /// Opens Main, shows this window and presses Play. Also starts the editor straight into the
        /// game: Unity.exe -projectPath . -executeMethod Buzzfield.Editor.PlayTestWindow.PlayMain
        /// </summary>
        [MenuItem("Buzzfield/Play Main", priority = 2)]
        public static void PlayMain()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(EditorAssets.ScenePath);
            Open();
            // Enter play mode once the editor has finished opening the scene and the window.
            EditorApplication.delayCall += EditorApplication.EnterPlaymode;
        }

        // Keeps the status lines current while the game runs.
        void OnInspectorUpdate() => Repaint();

        void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                DrawEditMode();
                return;
            }
            var game = FindAnyObjectByType<GameManager>();
            if (game == null)
            {
                EditorGUILayout.HelpBox("No GameManager in the running scene. Stop, open Main and press Play.", MessageType.Warning);
                return;
            }
            DrawStatus(game);
            DrawShortcuts(game);
        }

        static void DrawEditMode()
        {
            EditorGUILayout.HelpBox("Press Play in Main. For a phone view pick a portrait size in the Game view " +
                                    "(Buzzfield 9:16 or 9:20) or open Window > General > Device Simulator.", MessageType.Info);
            if (GUILayout.Button("Open Main scene") && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(EditorAssets.ScenePath);

            EditorGUILayout.Space();
            SaveManager save = RealSave();
            if (save == null)
                return;
            EditorGUILayout.LabelField("Save", save.MainPath, EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show in Explorer"))
                    EditorUtility.RevealInFinder(System.IO.File.Exists(save.MainPath) ? save.MainPath : System.IO.Path.GetDirectoryName(save.MainPath));
                if (GUILayout.Button("Delete save (new game)")
                    && EditorUtility.DisplayDialog("Delete save", "Delete the editor save and its backup? The next Play starts a new game.", "Delete", "Cancel"))
                    save.Delete();
            }
        }

        static void DrawStatus(GameManager game)
        {
            EditorGUILayout.LabelField("Honey", NumberFormat.Abbreviate(game.Economy.Honey));
            EditorGUILayout.LabelField("Garden", $"{game.Prestige.GardenIndex + 1}, bloom {game.Bloom.Percent}%");
            EditorGUILayout.LabelField("Bees", game.Bees.Count.ToString());
            EditorGUILayout.LabelField("Queen", $"Lv {game.Queen.Level}, {NumberFormat.Abbreviate(game.Prestige.RoyalJelly)} jelly");
            EditorGUILayout.LabelField("Flowers shaken", game.Stats.flowersShaken.ToString());
            EditorGUILayout.Space();
        }

        static void DrawShortcuts(GameManager game)
        {
            EditorGUILayout.LabelField("Honey", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+1K"))
                    game.DebugGrantHoney(BigNumber.FromDouble(1e3));
                if (GUILayout.Button("+1M"))
                    game.DebugGrantHoney(BigNumber.FromDouble(1e6));
                if (GUILayout.Button("x10"))
                    game.DebugGrantHoney(game.Economy.Honey.IsZero ? BigNumber.FromDouble(100) : game.Economy.Honey * BigNumber.FromDouble(9));
            }

            EditorGUILayout.LabelField("Dewdrops", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+100"))
                    game.DebugGrantDew(100);
                if (GUILayout.Button("+1000"))
                    game.DebugGrantDew(1000);
                GUILayout.Label($"{game.Dew.Balance}", GUILayout.Width(110f));
            }

            EditorGUILayout.LabelField("Garden", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bloom next flower"))
                    game.DebugBloomNext();
                if (GUILayout.Button("Complete garden"))
                    game.DebugCompleteGarden();
            }
            using (new EditorGUI.DisabledScope(!game.Prestige.CanMove(game.Bloom.Fraction, game.Economy.Honey)))
            {
                if (GUILayout.Button("Move the Queen"))
                    game.TryMoveQueen();
            }

            EditorGUILayout.LabelField("Pollen", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refill pollen"))
                    game.DebugRefillPollen();
                if (GUILayout.Button("Show swipe hint again"))
                    game.DebugResetSwipeHint();
            }

            EditorGUILayout.LabelField("Butterfly and goals", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Send the butterfly"))
                    game.DebugLaunchButterfly();
                if (GUILayout.Button("With a pouch"))
                    game.DebugLaunchButterfly(true);
                GUILayout.Label(game.Goals.HasGoal ? $"Goal {game.Goals.Index + 1}/{game.Goals.Count}" : "Goals done", GUILayout.Width(110f));
            }

            EditorGUILayout.LabelField("Daily streak", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Trust device clock", "Use the device clock as trusted time when the time server cannot be reached.")))
                    game.DebugTrustDeviceClock();
                if (GUILayout.Button(new GUIContent("Next day", "Moves the date a day ahead until play mode ends. Days saved meanwhile stay ahead of the real date.")))
                    game.DebugShiftDays(1);
                if (GUILayout.Button("Open panel"))
                    game.OpenDaily();
                GUILayout.Label(game.TryGetToday(out long _) ? $"Streak {game.Daily.streak}" : "No date yet", GUILayout.Width(110f));
            }

            EditorGUILayout.LabelField("Game speed", EditorStyles.boldLabel);
            int current = System.Array.IndexOf(Speeds, Time.timeScale);
            int picked = GUILayout.Toolbar(current, SpeedLabels);
            if (picked != current && picked >= 0)
                Time.timeScale = Speeds[picked];

            EditorGUILayout.Space();
            if (GUILayout.Button("Save now"))
                game.SaveNow();
        }

        /// <summary>The save Play uses (never the play mode tests' temp folder); null when the game settings are missing.</summary>
        static SaveManager RealSave()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>($"{EditorAssets.DataRoot}/Settings/GameSettings.asset");
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Default data is missing. Run Buzzfield > Create Default Data.", MessageType.Warning);
                return null;
            }
            SaveManager.DirectoryOverride = null;
            return new SaveManager(settings.SaveFileName);
        }
    }
}
