using System.Threading.Channels;

namespace LabSyncBackbone.Services
{
    public class RetryTrigger
    {
        // Capacity = 1: if multiple successes happen while the worker is already
        // running, they collapse into one queued signal instead of building up.
        private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            }
        );

        public void Trigger()
        {
            _channel.Writer.TryWrite(true);
        }

        // Returns true if woken by a signal, false if the timeout expired
        public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            try
            {
                await _channel.Reader.WaitToReadAsync(cts.Token);
                _channel.Reader.TryRead(out _); // drain the signal
                return true;
            }
            catch (OperationCanceledException)
            {
                return false; // timed out normally — not an error
            }
        }
    }
}
