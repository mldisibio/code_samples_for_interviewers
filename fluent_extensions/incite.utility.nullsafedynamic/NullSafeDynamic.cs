using System.Collections;
using System.Dynamic;

namespace contoso.utility.nullsafedynamic;

/// <summary>
/// Implementation of a <see cref="DynamicObject"/> which will not throw
/// the RuntimeBinderException which an <see cref="ExpandoObject"/> throws if you 
/// try to read a non-existent dynamic property at runtime.
/// </summary>
public class NullSafeDynamic : DynamicObject, IDictionary<string, object?>
{
    readonly IDictionary<string, object?> _members;

    /// <summary>Default ctor.</summary>
    public NullSafeDynamic()
    {
        _members = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Intitialize with the underlying <see cref="IDictionary{String, Object}"/>.</summary>
    public NullSafeDynamic(IDictionary<string, object?> src)
    {
        if (src == null)
            _members = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        else
            _members = new Dictionary<string, object?>(src, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Intitialize with an underlying <see cref="ExpandoObject"/>.</summary>
    public NullSafeDynamic(ExpandoObject expando)
    {
        if (expando == null)
            _members = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        else
            _members = new Dictionary<string, object?>(expando, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Called when a dynamic property is read at runtime.</summary>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (_members.Count > 0 && binder != null && !string.IsNullOrEmpty(binder.Name))
            if (_members.TryGetValue(binder.Name, out result))
                return true;

        // by returning an empty instance of this DynamicObject (instead of a null object) if the property doesn't exist
        // you can (as a convenience) also continue chaining non-existent nested properties (e.g. 'src.a.b.c')
        // without generating a NullReferenceException;
        // however, since the final result will not actually be null, you will need to cast it to your expected primitive
        // to see if the property truly exists (e.g. 'src.a.b.c as string');
        result = new NullSafeDynamic();
        // always return true so as not to throw RuntimeBinderException (as a convenience)
        return true;
    }

    /// <summary>Called when a dynamic property is set at runtime.</summary>
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (binder != null && !String.IsNullOrEmpty(binder.Name))
            _members[binder.Name] = value;
        // members can always be added or replaced, so return true
        return true;
    }

    /// <summary>Gets the number of elements contained in the underlying <see cref="IDictionary{String, Object}"/>.</summary>
    public int Count { get { return _members.Count; } }

    /// <summary>Determines whether the <see cref="IDictionary{String, Object}"/> contains an element with the specified key.</summary>
    public bool ContainsKey(string key) { return key == null ? false : _members.ContainsKey(key); }

    /// <summary>Gets the value associated with the specified key.</summary>
    public bool TryGetValue(string key, out object? value)
    {
        value = null;
        return key == null ? false : _members.TryGetValue(key, out value);
    }

    object? IDictionary<string, object?>.this[string key]
    {
        get
        {
            if (key != null && _members.ContainsKey(key))
                return _members[key];
            return null;
        }
        set
        {
            if (key != null)
                _members[key] = value;
        }
    }

    ICollection<string> IDictionary<string, object?>.Keys { get { return _members.Keys; } }

    ICollection<object?> IDictionary<string, object?>.Values { get { return _members.Values; } }

    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly { get { return _members.IsReadOnly; } }

    void IDictionary<string, object?>.Add(string key, object? value)
    {
        if (key != null)
            _members.Add(key, value);
    }

    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) { _members.Add(item); }

    void ICollection<KeyValuePair<string, object?>>.Clear() { _members.Clear(); }

    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item) { return _members.Contains(item); }

    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) { _members.CopyTo(array, arrayIndex); }

    bool IDictionary<string, object?>.Remove(string key) { return key == null ? false : _members.Remove(key); }

    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) { return _members.Remove(item); }

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() { return _members.GetEnumerator(); }

    IEnumerator IEnumerable.GetEnumerator() { return _members.GetEnumerator(); }
}
