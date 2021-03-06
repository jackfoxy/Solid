using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Solid.FingerTree.Iteration
{
	internal class DigitEnumerator<T> : IEnumerator<Measured>
		where T : Measured<T>
	{
		private readonly Digit<T> digit;
		private int childNumber;
		private IEnumerator<Measured> inner;

		public DigitEnumerator(Digit<T> digit)
		{
			digit.IsNotNull();
			this.digit = digit;
			Reset();
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (inner.MoveNext())
				return true;
			childNumber++;
			if (childNumber < digit.Size)
			{
				inner = digit.ChildbyIndex(childNumber).GetEnumerator();
				return inner.MoveNext();
			}
			return false;
		}

		public void Reset()
		{
			childNumber = 0;
			inner = digit.First.GetEnumerator();
		}

		public Measured Current
		{
			get
			{
				return inner.Current;
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return Current;
			}
		}
	}
}