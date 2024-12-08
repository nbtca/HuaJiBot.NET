using System.Reflection;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.UnitTest;

internal class TestAdapter : BotServiceBase
{
    public override ILogger Logger { get; init; } = new ConsoleLogger();

    public override void Reconnect()
    {
        throw new NotImplementedException();
    }

    public override Task SetupServiceAsync()
    {
        return Task.CompletedTask;
    }

    public override string[] AllRobots => throw new NotImplementedException();

    public override Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        throw new NotImplementedException();
    }

    public override void RecallMessage(string? robotId, string targetGroup, string msgId)
    {
        throw new NotImplementedException();
    }

    public override void SetGroupName(string? robotId, string targetGroup, string groupName)
    {
        throw new NotImplementedException();
    }

    public override void FeedbackAt(string? robotId, string targetGroup, string msgId, string text)
    {
        throw new NotImplementedException();
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    public override string GetNick(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 取插件数据目录
    /// </summary>
    /// <returns></returns>
    public override string GetPluginDataPath()
    {
        var path = Path.GetFullPath(Path.Combine("plugins", "data")); //插件数据目录，当前目录下的plugins/data
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path); //自动创建目录
        return path;
    }
}

public class Tests
{
    [SetUp]
    public void Setup() { }

    private readonly TestAdapter _api = new();

    private enum TestE
    {
        [CommandEnumItem("XA", "testA")]
        A,
        B,
        C,
    }

    [Test]
    public void Test3()
    {
        string EnumToAttributeName<T>(T value)
            where T : Enum
        {
            Type enumType = typeof(T);
            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetValue(null)?.Equals(value) ?? false)
                {
                    return field.GetCustomAttribute<CommandEnumItemAttribute>()?.Alias
                        ?? field.Name;
                }
            }
            return value.ToString();
        }
        Console.WriteLine(EnumToAttributeName(TestE.A));
        Console.WriteLine(EnumToAttributeName(TestE.B));
    }

    [Test]
    public void Test2()
    {
        static IEnumerable<CommonCommandReader.ReaderEntity> Read()
        {
            yield return "枚举";
            yield return "BA";
        }
        var reader = new DefaultCommandReader(Read());
        {
            var result = reader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = reader.Match<TestE?>(
                [TestE.A, TestE.B, TestE.C],
                x => [x?.ToString() ?? "?"],
                out var test,
                true
            );
            Console.WriteLine(result);
            Console.WriteLine(test.ToString());
        }
    }

    [Test]
    public void Test1()
    {
        static IEnumerable<CommonCommandReader.ReaderEntity> Read()
        {
            yield return "a";
            yield return "\"测 试 ``` code ```\"";
            yield return new CommonCommandReader.ReaderAt("0001");
            yield return "test a  '测试  内容'";
            yield return " aaaa '测试  内容' testa";
        }
        var reader = new DefaultCommandReader(Read());
        {
            var result = reader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = reader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = reader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = reader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = reader.Match(new[] { "test" }, x => x, out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = reader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
    }
}
