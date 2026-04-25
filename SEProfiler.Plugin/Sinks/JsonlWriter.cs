using System.IO;
using System.Text;

namespace SEProfiler.Sinks
{
    public sealed class JsonlWriter
    {
        private readonly AggregateSink _sink;
        private StreamWriter _writer;
        private string _currentPath;

        public JsonlWriter(AggregateSink sink)
        {
            _sink = sink;
        }

        public void Open(string outputPath)
        {
            Close();
            _currentPath = outputPath + ".jsonl";

            string dir = Path.GetDirectoryName(_currentPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            _writer = new StreamWriter(
                new FileStream(_currentPath, FileMode.Create, FileAccess.Write, FileShare.Read),
                Encoding.UTF8,
                bufferSize: 65536);

            _sink.JsonlEnabled = true;
        }

        public void Flush()
        {
            if (_writer == null)
                return;

            string line;
            while (_sink.TryDequeueEvent(out line))
                _writer.WriteLine(line);

            _writer.Flush();
        }

        public void Close()
        {
            _sink.JsonlEnabled = false;

            if (_writer == null)
                return;

            Flush();
            _writer.Close();
            _writer = null;
            _currentPath = null;
        }
    }
}
