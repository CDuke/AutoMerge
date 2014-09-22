using System;

namespace AutoMerge
{
	public interface ILogger
	{
	    void LogDebug(string message);

	    void LogDebug(string message, params object[] args);

        void LogInfo(string message);

        void LogInfo(string message, params object[] args);

        void LogError(string message);

        void LogError(string message, Exception ex);
    }
}
