using System.Collections.Concurrent;

namespace VSifier;

// ════════════════════════════════════════════════════════════════
//  日志缓冲管理 - 避免频繁的 UI 更新导致卡顿
// ════════════════════════════════════════════════════════════════

internal sealed class LogBuffer
{
	private readonly ConcurrentQueue<string> _queue = new();
	private readonly System.Timers.Timer _flushTimer;
	private readonly Action<List<string>> _onFlush;
	private const int BATCH_SIZE = 50;
	private const int FLUSH_INTERVAL_MS = 500;

	public LogBuffer(Action<List<string>> onFlush)
	{
		_onFlush = onFlush;
		_flushTimer = new System.Timers.Timer(FLUSH_INTERVAL_MS);
		_flushTimer.Elapsed += (s, e) => Flush();
		_flushTimer.AutoReset = true;
		_flushTimer.Start();
	}

	/// <summary>
	/// 添加日志到缓冲区
	/// </summary>
	public void Enqueue(string message)
	{
		_queue.Enqueue(message);

		// 缓冲区满时立即刷新
		if (_queue.Count >= BATCH_SIZE)
			Flush();
	}

	/// <summary>
	/// 手动刷新缓冲区到 UI
	/// </summary>
	public void Flush()
	{
		var batch = new List<string>(BATCH_SIZE);
		while (_queue.TryDequeue(out var msg) && batch.Count < BATCH_SIZE)
			batch.Add(msg);

		if (batch.Count > 0)
			_onFlush(batch);
	}

	/// <summary>
	/// 停止缓冲并清空
	/// </summary>
	public void Dispose()
	{
		_flushTimer?.Stop();
		_flushTimer?.Dispose();
		Flush();
		_queue.Clear();
	}
}
