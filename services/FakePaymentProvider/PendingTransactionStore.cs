using System.Collections.Concurrent;

namespace FakePaymentProvider;

public record PendingTransaction(
    string TransactionId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Method,
    string WebhookUrl,
    string ReturnUrl,
    string ProviderSecret
);

/// <summary>In-memory store for pending fake transactions.</summary>
public class PendingTransactionStore
{
    private readonly ConcurrentDictionary<string, PendingTransaction> _store = new();

    public void Add(PendingTransaction tx) => _store[tx.TransactionId] = tx;

    public PendingTransaction? Get(string transactionId) =>
        _store.TryGetValue(transactionId, out var tx) ? tx : null;

    public void Remove(string transactionId) => _store.TryRemove(transactionId, out _);
}
