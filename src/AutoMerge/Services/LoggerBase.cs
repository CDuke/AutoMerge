using System;

namespace AutoMerge
{
    public abstract class LoggerBase : ILogger
    {
        protected LoggerBase()
        {
            
        }

        public void Log(string message)
        {
            var fullMessage = DateTime.Now + ": " + message + Environment.NewLine;
            WriteMessage(fullMessage);
        }

        public void Log(string message, Exception ex)
        {
            Log(message + Environment.NewLine + ex);
        }

        public void LogDebug(string message)
        {
            if (IsDebugEnabled())
            {
                var fullMessage = string.Format("{0} (DEBUG): {1} \r\n", DateTime.Now, message);
                WriteMessage(fullMessage);
            }
        }

        public void LogDebug(string message, params object[] args)
        {
            if (IsDebugEnabled())
            {
                var formatedMessage = string.Format(message, args);
                var fullMessage = string.Format("{0} (DEBUG): {1} \r\n", DateTime.Now, formatedMessage);
                WriteMessage(fullMessage);
            }
        }

        public void LogInfo(string message)
        {
            if (IsInfoEnabled())
            {
                var fullMessage = string.Format("{0} (INFO): {1} \r\n", DateTime.Now, message);
                WriteMessage(fullMessage);
            }
        }

        public void LogInfo(string message, params object[] args)
        {
            if (IsDebugEnabled())
            {
                var formatedMessage = string.Format(message, args);
                var fullMessage = string.Format("{0} (INFO): {1} \r\n", DateTime.Now, formatedMessage);
                WriteMessage(fullMessage);
            }
        }

        public void LogError(string message)
        {
            if (IsErrorEnabled())
            {
                var fullMessage = string.Format("{0} (ERROR): {1} \r\n", DateTime.Now, message);
                WriteMessage(fullMessage);
            }
        }

        public void LogError(string message, Exception ex)
        {
            if (IsDebugEnabled())
            {
                var formatedMessage = message + Environment.NewLine + ex.ToString();
                var fullMessage = string.Format("{0} (ERROR): {1} \r\n", DateTime.Now, formatedMessage);
                WriteMessage(fullMessage);
            }
        }

        private bool IsDebugEnabled()
        {
            return true;
        }

        private bool IsInfoEnabled()
        {
            return true;
        }

        private bool IsErrorEnabled()
        {
            return true;
        }

        protected abstract void WriteMessage(string message);
    }
}
