using System;
using System.IO;
using UnityEngine;

public static class AgentDebugLogger
{
    private const string SessionId = "42a0a1";
    private const string LogFilePath = "debug-42a0a1.log";

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public static void Log(string runId, string hypothesisId, string location, string message, string dataJson)
    {
        try
        {
            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string safeData = string.IsNullOrWhiteSpace(dataJson) ? "{}" : dataJson;
            string line =
                "{\"sessionId\":\"" + SessionId + "\"," +
                "\"runId\":\"" + Escape(runId) + "\"," +
                "\"hypothesisId\":\"" + Escape(hypothesisId) + "\"," +
                "\"location\":\"" + Escape(location) + "\"," +
                "\"message\":\"" + Escape(message) + "\"," +
                "\"data\":" + safeData + "," +
                "\"timestamp\":" + ts + "}";
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
        catch
        {
            // Never break gameplay due to debug logging.
        }
    }
}
