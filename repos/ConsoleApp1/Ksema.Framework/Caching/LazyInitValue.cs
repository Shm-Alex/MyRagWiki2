using System;
using Framework.Utils;

namespace Ksema.Framework.Caching
{
	public interface ILazyInitValue
	{
		object Value { get; }
	}

	public interface ILazyInitValue<T>
	{
		T Value { get; }
	}

	public class LazyInitValue<T> : ILazyInitValue<T>, ILazyInitValue
	{
		T _value;
		readonly Func<T> _initFunc;
		readonly object _lock = new object();
		volatile bool _initialized;

		public LazyInitValue(Func<T> initFunc)
		{
			AssertUtils.IsNotNull(initFunc, "func");
			_initFunc = initFunc;
		}

		public T Value
		{
			get
			{
				if (!_initialized)
				{
					lock (_lock)
					{
						if (!_initialized)
						{
							_value = _initFunc();
							_initialized = true;
						}
					}
				}
				return _value;
			}
		}

		public void Clear()
		{
			if (_initialized)
			{
				lock (_lock)
				{
					if (_initialized)
					{
						_initialized = false;
						_value = default(T);
					}
				}
			}
		}

		object ILazyInitValue.Value
		{
			get
			{
				return Value;
			}
		}
	}
}
