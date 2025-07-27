using System;
using System.Collections.Generic;

public class PriorityQueue<T> where T : IComparable<T>
{
    private List<T> heap;

    public PriorityQueue()
    {
        heap = new List<T>();
    }

    public int Count => heap.Count;

    public void Enqueue(T item)
    {
        heap.Add(item);
        BubbleUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Priority queue is empty");

        T result = heap[0];
        
        // Move last element to root and remove last
        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        
        if (heap.Count > 0)
            BubbleDown(0);
        
        return result;
    }

    private void BubbleUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            
            if (heap[index].CompareTo(heap[parentIndex]) >= 0)
                break;
            
            // Swap with parent
            (heap[index], heap[parentIndex]) = (heap[parentIndex], heap[index]);
            index = parentIndex;
        }
    }

    private void BubbleDown(int index)
    {
        while (true)
        {
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;
            int smallest = index;

            if (leftChild < heap.Count && heap[leftChild].CompareTo(heap[smallest]) < 0)
                smallest = leftChild;

            if (rightChild < heap.Count && heap[rightChild].CompareTo(heap[smallest]) < 0)
                smallest = rightChild;

            if (smallest == index)
                break;

            // Swap with smallest child
            (heap[index], heap[smallest]) = (heap[smallest], heap[index]);
            index = smallest;
        }
    }
}

// Node for Dijkstra's algorithm with priority queue support
public class DijkstraNode : IComparable<DijkstraNode>
{
    public int Cost { get; }
    public Godot.Vector2I Position { get; }

    public DijkstraNode(int cost, Godot.Vector2I position)
    {
        Cost = cost;
        Position = position;
    }

    public int CompareTo(DijkstraNode other)
    {
        return Cost.CompareTo(other.Cost);
    }
} 