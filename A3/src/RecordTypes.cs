using System;

namespace BPlusTree;

// TODO: Document
public sealed class Key(int Value) : IComparable<Key>, IComparable {
  public int Value { get; } = Value;
  public int CompareTo(Key other) => this.Value.CompareTo(other.Value);
  public int CompareTo(object obj) {
    if (obj is null) return 1;
    if (obj is not Key other) throw new ArgumentException("Object is not a Key instance.", nameof(obj));
    return this.CompareTo(other);
  }

  public override string ToString() => this.Value.ToString();
}
// TODO: Document
public class Record<KType, TValue>(KType Key, TValue Value) where KType : IComparable<KType> {
  public KType Key { get; } = Key;
  public TValue Value { get; } = Value;
}
