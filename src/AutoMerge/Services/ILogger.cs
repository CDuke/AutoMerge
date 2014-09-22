using System;

namespace AutoMerge
{
	public interface ILogger
	{
	    void Debug(string message);

	    void Debug(string message, params object[] args);

        void Info(string message);

        void Info(string message, params object[] args);

        void Error(string message);

        void Error(string message, Exception ex);
    }
}
