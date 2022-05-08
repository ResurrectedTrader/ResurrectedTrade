using System;
using System.Collections.Generic;
using ResurrectedTrade.AgentBase;

namespace ResurrectedTrade.Agent
{
    public class Logger : ILogger
    {
        private readonly LinkedList<string> _buffer = new LinkedList<string>();
        private readonly int _bufferSize;
        public event EventHandler Changed;

        public Logger(int bufferSize)
        {
            _bufferSize = bufferSize;
        }

        public void Info(string message)
        {
            var msg = $"[{DateTime.Now:O}] {message}";
            lock (this)
            {
                _buffer.AddLast(msg);
                while (_buffer.Count > _bufferSize)
                {
                    _buffer.RemoveFirst();
                }
            }

            Changed?.Invoke(this, EventArgs.Empty);

            Console.WriteLine(msg);
        }

        public void Debug(string message)
        {
            Info(message);
        }

        public string GetBufferContent()
        {
            lock (this)
            {
                return string.Join("\r\n", _buffer);
            }
        }
    }
}
