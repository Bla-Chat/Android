using System;

namespace BlaChat
{
	public interface IEqualsExpression<T>
	{
		System.Linq.Expressions.Expression<Func<T, bool>> EqualsExpression ();
	}
}

