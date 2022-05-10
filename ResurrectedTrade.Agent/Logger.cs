using System;
using System.Collections.Generic;
using ResurrectedTrade.AgentBase;

namespace ResurrectedTrade.Agent
{
    public class Logger : ILogger
    {
        private readonly LinkedList<string> _buffer = new LinkedList<string>();
        private readonly int _bufferSize;
        private string _previousMessage;
        private int _previousCount;
        public event EventHandler Changed;

        public Logger(int bufferSize)
        {
            _bufferSize = bufferSize;
        }

        public void Info(string message)
        {
            if (message == _previousMessage)
            {
                _previousCount++;
                return;
            }

            if (_previousCount > 1)
            {
                Log($"Previous message repeated {_previousCount} times.");
            }

            _previousMessage = message;
            _previousCount = 1;

           Log(message);
        }

        private void Log(string msg)
        {
            msg = $"[{DateTime.Now:O}] {msg}";
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
