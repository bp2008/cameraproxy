using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy
{
	public class ComparisonComparer<T> : IComparer<T>
	{
		private readonly Comparison<T> comparison;

		public ComparisonComparer(Comparison<T> comparison)
		{
			this.comparison = comparison;
		}

		int IComparer<T>.Compare(T x, T y)
		{
			return comparison(x, y);
		}
	}
}
