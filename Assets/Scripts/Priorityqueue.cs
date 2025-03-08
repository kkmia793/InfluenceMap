using System.Collections.Generic;
using System.Linq;

public class PriorityQueue<T>
{
    private readonly SortedDictionary<float, Queue<T>> _dictionary = new SortedDictionary<float, Queue<T>>();

    public int Count { get; private set; } = 0;

    public void Enqueue(T item, float priority)
    {
        if (!_dictionary.TryGetValue(priority, out var queue))
        {
            queue = new Queue<T>();
            _dictionary[priority] = queue;
        }
        queue.Enqueue(item);
        Count++;
    }

    public T Dequeue()
    {
        if (_dictionary.Count == 0)
            return default;

        var firstPair = _dictionary.First();
        var item = firstPair.Value.Dequeue();
        if (firstPair.Value.Count == 0)
            _dictionary.Remove(firstPair.Key);

        Count--;
        return item;
    }
}