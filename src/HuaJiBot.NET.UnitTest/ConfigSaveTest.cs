namespace HuaJiBot.NET.UnitTest;

[NonParallelizable]
internal class ConfigSaveTest
{
    [Test]
    public void Save_KeepsFilePermissions()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Unix file modes only.");
            return;
        }
        var dir = Directory.CreateTempSubdirectory().FullName;
        var cwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = dir;
            File.WriteAllText("config.json", "{}");
            File.SetUnixFileMode("config.json", UnixFileMode.UserRead | UnixFileMode.UserWrite);

            Config.Config.Load().Save();
            var mode = File.GetUnixFileMode("config.json");

            Assert.Multiple(() =>
            {
                Assert.That(mode, Is.EqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite));
                Assert.That(File.Exists("config.json.tmp"), Is.False);
            });
        }
        finally
        {
            Environment.CurrentDirectory = cwd;
            Directory.Delete(dir, true);
        }
    }
}
