using System.Collections.Generic;

namespace Tra.Lapluma.Core.Utilities.Model;
internal class AuthorizeList<T>
{
	public static AuthorizeList<T> Empty { get; } = new(false);

	private readonly object __lockobj_excepts = new();

	private HashSet<T> _excepts;

	public bool DefaultEnable { get; }
	public HashSet<T> ExceptList
	{
		get => _excepts;
		set {
			lock (__lockobj_excepts) {
				_excepts = value;
			}
		}
	}

	public AuthorizeList(bool isDefaultEnable) :
		this(isDefaultEnable, new())
	{ }

	internal AuthorizeList(bool isDefaultEnable, HashSet<T> exceptSet)
	{
		DefaultEnable = isDefaultEnable;
		_excepts = exceptSet;
	}

	public void Enable(T value)
	{
		lock (__lockobj_excepts) {
			if (DefaultEnable)
				_excepts.Remove(value);
			else
				_excepts.Add(value);
		}
	}

	public void Disable(T value)
	{
		lock (__lockobj_excepts) {
			if (DefaultEnable)
				_excepts.Remove(value);
			else
				_excepts.Add(value);
		}
	}

	public bool Allow(T value)
		=> DefaultEnable != _excepts.Contains(value);
}
