using System;
using System.Collections.Generic;
using System.Linq;

namespace BPlusTree;

public sealed class Key : IComparable<Key>, IComparable {
  public int Value { get; }

  public Key(int value) {
    this.Value = value;
  }

  public int CompareTo(Key other) {
    return this.Value.CompareTo(other.Value);
  }

  public int CompareTo(object obj) {
    if (obj is null) return 1;
    if (obj is not Key other) {
      throw new ArgumentException("Object is not a Key instance.", nameof(obj));
    }
    return this.CompareTo(other);
  }

  public override string ToString() => this.Value.ToString();
}

public interface IRecord<KType> where KType : IComparable<KType> {
  KType Key { get; }
}

public class Record<KType> : IRecord<KType> where KType : IComparable<KType> {
  public KType Key { get; set; }
  public List<string> Values { get; set; }

  public Record(KType key, IEnumerable<string> values = null) {
    this.Key = key;
    this.Values = values?.ToList() ?? new List<string>();
  }
}
