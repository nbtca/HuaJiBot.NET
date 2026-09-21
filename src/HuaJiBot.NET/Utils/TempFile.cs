namespace HuaJiBot.NET.Utils;

public static class TempFile
{
    /// <summary>
    /// 用于出作用域自动删除文件
    /// </summary>
    public class AutoDeleteFile : IDisposable
    {
        private readonly string _file;

        internal AutoDeleteFile(string file)
        {
            _file = file;
        }

        public void Dispose() //销毁
        { //delete after 30s
            Task.Delay(30_000)
                .ContinueWith(_ =>
                {
                    File.Delete(_file); //删除文件
                });
        }

        public static implicit operator string(AutoDeleteFile file) => file.FileName;

        public string FileName => _file;
    }
}
