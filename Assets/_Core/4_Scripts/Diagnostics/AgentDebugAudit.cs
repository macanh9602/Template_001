using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Reusable, file-only diagnostics for agent-assisted runtime debugging.
/// The debug-audit skill owns where/when hooks are added; this class only provides the sink.
/// </summary>
public static class AgentDebugAudit
{
    private const int DefaultMaxRecords = 500;
    private static readonly Dictionary<string, ChannelState> Channels = new Dictionary<string, ChannelState>(StringComparer.Ordinal);
    private static readonly StringBuilder Builder = new StringBuilder(512);
    private static readonly object Sync = new object();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        Channels.Clear();
        Builder.Clear();
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Begin(string channel, string scope, bool enabled = true, int maxRecords = DefaultMaxRecords)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (string.IsNullOrWhiteSpace(channel)) return;

        var state = new ChannelState
        {
            Enabled = enabled,
            RecordCount = 0,
            MaxRecords = Math.Max(1, maxRecords),
            TruncatedWritten = false,
            Path = BuildPath(channel)
        };
        Channels[channel] = state;
        if (!enabled) return;

        Directory.CreateDirectory(Path.GetDirectoryName(state.Path));
        Builder.Clear();
        Builder.Append("# Agent Audit: ").AppendLine(channel)
            .Append("Generated: ").AppendLine(DateTime.Now.ToString("O"))
            .Append("Scope: ").AppendLine(string.IsNullOrWhiteSpace(scope) ? "<none>" : scope)
            .AppendLine();
        lock (Sync)
        {
            File.WriteAllText(state.Path, Builder.ToString(), Encoding.UTF8);
        }
#endif
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void SetEnabled(string channel, bool enabled)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!Channels.TryGetValue(channel, out ChannelState state)) return;
        state.Enabled = enabled;
        Channels[channel] = state;
#endif
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Event(string channel, string tag, string details)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!Channels.TryGetValue(channel, out ChannelState state) || !state.Enabled) return;

        if (state.RecordCount >= state.MaxRecords)
        {
            if (!state.TruncatedWritten)
            {
                state.TruncatedWritten = true;
                Append(state.Path, "## AUDIT_TRUNCATED\n- maxRecords: " + state.MaxRecords + "\n\n");
                Channels[channel] = state;
            }
            return;
        }

        state.RecordCount++;
        Channels[channel] = state;

        Builder.Clear();
        Builder.Append("## ").Append(state.RecordCount).Append(" · ").AppendLine(string.IsNullOrWhiteSpace(tag) ? "Event" : tag)
            .Append("- frame: ").AppendLine(Time.frameCount.ToString())
            .Append("- time: ").AppendLine(Time.realtimeSinceStartup.ToString("F4"))
            .Append("- details: ").AppendLine(string.IsNullOrWhiteSpace(details) ? "<none>" : details)
            .AppendLine();
        Append(state.Path, Builder.ToString());
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static string BuildPath(string channel)
    {
        string safe = Sanitize(channel) + ".md";
#if UNITY_EDITOR
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(projectRoot, "AgentAudit", safe);
#else
        return Path.Combine(Application.persistentDataPath, "AgentAudit", safe);
#endif
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '-');
        return value.Trim().Replace(' ', '-');
    }

    private static void Append(string path, string text)
    {
        lock (Sync)
        {
            File.AppendAllText(path, text, Encoding.UTF8);
        }
    }

    private struct ChannelState
    {
        public bool Enabled;
        public int RecordCount;
        public int MaxRecords;
        public bool TruncatedWritten;
        public string Path;
    }
#endif
}
