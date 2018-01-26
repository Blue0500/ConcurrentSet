using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoshuaKearney.Collections {
    public class ConcurrentSet<T> : IEnumerable<T>, IEnumerable, ICollection<T>, ICollection, IReadOnlyCollection<T>, ISet<T> {

    private ConcurrentDictionary<T, bool> dict;
        private IEqualityComparer<T> comparer;

        public ConcurrentSet() : this(EqualityComparer<T>.Default) { }

        public ConcurrentSet(IEqualityComparer<T> comparer) {
            this.dict = new ConcurrentDictionary<T, bool>(comparer);
            this.comparer = comparer;
        }

        public ConcurrentSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) {
            this.dict = new ConcurrentDictionary<T, bool>(
                collection.ToDictionary(x => x, x => true),
                comparer
            );

            this.comparer = comparer;
        }

        public ConcurrentSet(IEnumerable<T> collection) : this(collection, EqualityComparer<T>.Default) { }

        public int Count => this.dict.Count;

        bool ICollection.IsSynchronized => ((ICollection)this.dict).IsSynchronized;

        object ICollection.SyncRoot => ((ICollection)this.dict).SyncRoot;

        bool ICollection<T>.IsReadOnly => false;

        public bool IsEmpty => this.dict.IsEmpty;

        public void Clear() => this.dict.Clear();

        public bool Contains(T item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            return this.dict.ContainsKey(item);
        }

        public void CopyTo(T[] array, int index) {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }

            foreach (T item in this) {
                array[index] = item;
                index++;
            }
        }

        public IEnumerator<T> GetEnumerator() => this.dict.Keys.GetEnumerator();

        void ICollection.CopyTo(Array array, int index) {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }

            foreach (T item in this) {
                array.SetValue(item, index);
                index++;
            }
        }

        void ICollection<T>.Add(T item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            this.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public bool Add(T item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            return this.dict.TryAdd(item, true);
        }

        public int AddAll(IEnumerable<T> other) {
            int count = 0;

            foreach (T item in other) {
                if (this.Add(item)) {
                    count++;
                }
            }

            return count;
        }

        public bool Remove(T item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            return this.dict.TryRemove(item, out _);
        }

        public int RemoveAll(IEnumerable<T> other) {
            int count = 0;

            foreach (T item in other) {
                if (this.Remove(item)) {
                    count++;
                }
            }

            return count;
        }

        public int RemoveWhere(Func<T, bool> predicate) {
            int removed = 0;
            
            foreach (T item in this.Where(predicate)) {
                if (this.Remove(item)) {
                    removed++;
                }
            }

            return removed;
        }

        public void ExceptWith(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException();
            }

            if (this.Count == 0) {
                return;
            }

            foreach (T item in other) {
                this.Remove(item);

                if (this.Count == 0) {
                    return;
                }
            }
        }

        public void IntersectWith(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            if (this.Count == 0) {
                return;
            }

            if (!other.Any()) {
                this.Clear();
                return;
            }

            if (other == this) {
                return;
            }

            if (other is ConcurrentSet<T> set && set.comparer.Equals(this.comparer)) {
                this.IntersectWithSetWithSameEC(set);
                return;
            }

            this.IntersectWithEnumerable(other);
        }

        private void IntersectWithSetWithSameEC(ISet<T> set) {
            foreach (var item in this) {
                if (!set.Contains(item)) {
                    this.Remove(item);
                }
            }
        }

        private void IntersectWithEnumerable(IEnumerable<T> other) {
            // The state of this function must be stored here and not in this object
            // because the set could change during this call
            HashSet<T> set = new HashSet<T>(this.comparer);

            // Mark items for keeping
            foreach (var item in other) {
                if (this.Contains(item)) {
                    set.Add(item);
                }
            }

            // If an item isn't marked, remove it
            foreach (var item in this) {
                if (!set.Contains(item)) {
                    this.Remove(item);
                }
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            // No set is a proper subset of itself
            if (other == this) {
                return false;
            }

            // If other is the empty set, it has no proper subsets
            if (!other.Any()) {
                return false;
            }

            // We know that other has some elements (the previous check), 
            // so this must be a proper subset
            if (this.Count == 0) {
                return true;
            }

            if (other is ConcurrentSet<T> set && this.comparer.Equals(set.comparer)) {
                // Elements are garunteed to be unique, so check count
                if (this.Count >= set.Count) {
                    return false;
                }

                // This has less elements than set, so a normal subset check will work
                return this.IsSubsetOfSetWithSameEC(set);
            }

            return IsProperSubsetOfEnumerable(other);
        }

        private bool IsSubsetOfSetWithSameEC(ISet<T> set) {
            foreach (var item in this) {
                if (!set.Contains(item)) {
                    return false;
                }
            }

            return true;
        }

        private bool IsProperSubsetOfEnumerable(IEnumerable<T> other) {
            HashSet<T> set = new HashSet<T>(other, this.comparer);

            if (this.Count >= set.Count) {
                return false;
            }

            foreach (var item in this) {
                if (!set.Contains(item)) {
                    return false;
                }
            }

            return true;
        }

        public bool IsSubsetOf(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            if (this.Count == 0) {
                return true;
            }

            // This must have some elements
            if (!other.Any()) {
                return false;
            }

            if (this == other) {
                return true;
            }

            ISet<T> set;
            if (other is ConcurrentSet<T> concSet && concSet.comparer.Equals(this.comparer)) {
                set = concSet;
            }
            else {
                set = new HashSet<T>(other, this.comparer);
            }

            if (this.Count > set.Count) {
                return false;
            }

            return this.IsSubsetOfSetWithSameEC(set);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            if (this == other) {
                return false;
            }

            if (this.Count == 0) {
                return false;
            }

            if (!other.Any()) {
                return true;
            }

            ISet<T> set;
            if (other is ConcurrentSet<T> concSet && concSet.comparer.Equals(this.comparer)) {              
                set = concSet;
            }
            else {
                set = new HashSet<T>(other, this.comparer);
            }

            if (this.Count <= set.Count) {
                return false;
            }

            return this.IsSupersetOf(set);
        }

        public bool IsSupersetOf(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException();
            }

            if (this == other) {
                return true;
            }

            if (this.Count == 0) {
                return !other.Any();
            }

            if (!other.Any()) {
                return true;
            }

            foreach (T item in other) {
                if (!this.Contains(item)) {
                    return false;
                }
            }

            return true;
        }

        public bool Overlaps(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            if (!other.Any() || this.Count == 0) {
                return false;
            }

            if (this == other) {
                return true;
            }

            foreach (T item in other) {
                if (this.Contains(item)) {
                    return true;
                }
            }

            return false;
        }

        public bool SetEquals(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            if (this.IsEmpty) {
                return !other.Any();
            }

            if (this == other) {
                return true;
            }

            if (other is ICollection<T> collection) {
                if (this.Count != collection.Count) {
                    return false;
                }
            }

            ISet<T> set;
            if (other is ConcurrentSet<T> concSet && concSet.comparer.Equals(this.comparer)) {
                set = concSet;
            }
            else {
                set = new HashSet<T>(other, this.comparer);
            }

            if (this.Count != set.Count) {
                return false;
            }

            return this.IsSubsetOf(set);
        }

        public void SymmetricExceptWith(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            if (!other.Any()) {
                return;
            }

            if (this.IsEmpty) {
                this.AddAll(other);
                return;
            }

            if (this == other) {
                this.Clear();
                return;
            }

            ISet<T> set;
            if (other is ConcurrentSet<T> concSet && concSet.comparer.Equals(this.comparer)) {
                set = concSet;
            }
            else {
                set = new HashSet<T>(other);
            }

            foreach (T item in set) {
                if (!this.Remove(item)) {
                    this.Add(item);
                }
            }
        }

        public void UnionWith(IEnumerable<T> other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            this.AddAll(other);
        }
    }
}