using System;

namespace AlmediaSDK
{
    /// <summary>
    /// A task the server reports as completed. <see cref="Almedia.OnTaskCompleted"/> delivers it.
    /// The event is historical and best-effort. The task is not always in <see cref="Almedia.Progress"/>,
    /// and a replay can deliver the same completion twice. Deduplicate on <see cref="Id"/>.
    /// </summary>
    public sealed class AlmediaTaskCompletion : IEquatable<AlmediaTaskCompletion>
    {
        /// <summary>The server-generated message identifier. A replay carries the same identifier.</summary>
        public string Id { get; }

        /// <summary>The completion, with the reward it paid.</summary>
        public AlmediaCompletedTask Task { get; }

        public AlmediaTaskCompletion(string id, AlmediaCompletedTask task)
        {
            Id = id;
            Task = task;
        }

        internal static AlmediaTaskCompletion FromResponse(TaskCompletedResponse response)
            => new AlmediaTaskCompletion(response.id, AlmediaCompletedTask.FromItem(response.task));

        public bool Equals(AlmediaTaskCompletion other)
            => other != null
               && string.Equals(Id, other.Id, StringComparison.Ordinal)
               && Equals(Task, other.Task);

        public override bool Equals(object obj) => Equals(obj as AlmediaTaskCompletion);

        public override int GetHashCode() => ValueEquality.Hash(Id, Task);

        public static bool operator ==(AlmediaTaskCompletion a, AlmediaTaskCompletion b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaTaskCompletion a, AlmediaTaskCompletion b) => !(a == b);
    }
}
