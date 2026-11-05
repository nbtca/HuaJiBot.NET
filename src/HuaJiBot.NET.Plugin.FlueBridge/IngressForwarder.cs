namespace HuaJiBot.NET.Plugin.FlueBridge;

internal sealed class IngressForwarder(BridgeStore store, IngressClient client, int retrySeconds)
{
    internal async Task<bool> ForwardAsync(IngressEvent message, CancellationToken ct)
    {
        try
        {
            await client.SendAsync(message, ct);
            store.RemoveIngress(message.EventId);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (HttpRequestException ex)
        {
            var status = ex.StatusCode is { } code ? (int?)code : null;
            // Authentication can recover after credential rotation; timeout/rate-limit errors are transient.
            var permanent = status is >= 400 and < 500 and not (401 or 408 or 425 or 429);
            store.FailIngress(message.EventId, status, permanent, retrySeconds);
        }
        catch (InvalidDataException)
        {
            store.FailIngress(message.EventId, 413, true, retrySeconds);
        }
        catch (Exception)
        {
            store.FailIngress(message.EventId, null, false, retrySeconds);
        }
        return false;
    }
}
