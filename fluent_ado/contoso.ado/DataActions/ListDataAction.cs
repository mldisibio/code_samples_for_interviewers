using System;
using System.Collections.Generic;

namespace contoso.ado.DataActions
{
    /// <summary>
    /// An base class wrapping an Action delegate that collects the a sequence of items of <typeparamref name="T"/>
    /// to a list and makes the read-only list available.
    /// </summary>
    public abstract class ListDataAction<T>
    {
        readonly List<T> _src = new List<T>(1024);
        bool _retrieved;

        /// <summary>
        /// Returns a default implementation which simply collects a sequence of items
        /// to a List of <typeparamref name="T"/> and makes the read-only list available.
        /// </summary>
        public static ListDataAction<T> Default() { return new DefaultWrapper<T>(); }

        /// <summary>
        /// The list of objects collected by this Action wrapper.
        /// The underlying collection can be modified but the list itself is read-only.
        /// </summary>
        public List<T> Items
        {
            get
            {
                if (!_retrieved)
                {
                    _retrieved = true;
                    _src.TrimExcess();
                }
                return _src;
            }
        }

        /// <summary>For internal use. Returns a copy of the original list and clears the original list.</summary>
        internal List<T> CopyAndClear()
        {
            var copy = new List<T>(_src);
            _src.Clear();
            _src.TrimExcess();
            return copy;
        }

        /// <summary>
        /// An Action delegate for collecting a sequence of items of <typeparamref name="T"/> to a list.
        /// The default implementation simply adds the input to the list, even if null.
        /// When overriding, invoke the base implementation to add the item to the underlying collection.
        /// </summary>
        public virtual void AddToList(T dto){ _src.Add(dto); }

        /// <summary>
        /// The default implementation which simply collects input to a List of <typeparamref name="U"/> and makes the read-only list available.
        /// </summary>
        class DefaultWrapper<U> : ListDataAction<U> { }
    }
}
