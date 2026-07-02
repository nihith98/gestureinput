using System;
using System.IO;

namespace GestureInput.Tests
{
    /// <summary>
    /// Locates checked-in fixture files from both execution environments:
    /// Unity EditMode tests (cwd = project root, package under Packages/) and
    /// the DevTests~ dotnet harness (base dir somewhere under DevTests~/bin/).
    /// </summary>
    public static class FixturePaths
    {
        private const string FixtureDirName = "Fixtures";

        public static string Resolve(string fileName)
        {
            // 1. Unity: package resolved into the project
            var unityPath = Path.Combine("Packages", "com.nihith.gestureinput", "Tests", "EditMode", FixtureDirName, fileName);
            if (File.Exists(unityPath)) return Path.GetFullPath(unityPath);

            // 2. dotnet harness / anything else: walk up from the app base looking
            //    for the package root (identified by Tests/EditMode/Fixtures).
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Tests", "EditMode", FixtureDirName, fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException(
                $"Fixture '{fileName}' not found from '{AppContext.BaseDirectory}' or '{unityPath}'.");
        }

        public static string FixtureDirectory()
        {
            var probe = Resolve("smoke.gframes");
            return Path.GetDirectoryName(probe);
        }
    }
}
