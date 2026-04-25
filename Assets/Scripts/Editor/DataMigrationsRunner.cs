using UnityEditor;
using UnityEngine;
using Kindrith.Persistence.Migrations;

namespace Kindrith.Editor
{
    public static class DataMigrationsRunner
    {
        [MenuItem("Tools/Kindrith/Run Migrations")]
        public static void RunFromMenu()
        {
            int applied = Run();
            EditorUtility.DisplayDialog("Kindrith Migrations",
                applied == 0
                    ? "Already up to date."
                    : $"Applied {applied} migration(s).",
                "OK");
        }

        // Batchmode entrypoint: -executeMethod Kindrith.Editor.DataMigrationsRunner.RunForBatch
        public static void RunForBatch()
        {
            int applied = Run();
            Debug.Log($"DataMigrationsRunner: applied {applied} migration(s).");
        }

        public static int Run()
        {
            var migrator = new Migrator(Migrator.DefaultMigrations());
            return migrator.Run();
        }
    }
}
