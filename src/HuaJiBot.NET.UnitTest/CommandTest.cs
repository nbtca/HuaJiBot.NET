using System.Reflection;
using HuaJiBot.NET.Commands;

namespace HuaJiBot.NET.UnitTest;

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
            var result = reader.Match(["test"], x => x, out var test);
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
