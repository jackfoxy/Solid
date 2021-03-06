using System;
using System.Collections.Generic;
using NUnit.Framework;
using Solid.Common;
using Solid.FingerTree.Iteration;

namespace Solid.FingerTree
{
	internal sealed class Compound<T> : FTree<T>
		where T : Measured<T>
	{
		public readonly FTree<Digit<T>> DeepTree;
		public readonly Digit<T> LeftDigit;
		public readonly Digit<T> RightDigit;


		public Compound(int measure, Digit<T> leftDigit, FTree<Digit<T>> deepTree, Digit<T> rightDigit)
			: base(measure, TreeType.Compound)
		{
			leftDigit.IsNotNull();
			deepTree.IsNotNull();
			rightDigit.IsNotNull();
			measure.IsStructuralEqual(leftDigit.Measure + rightDigit.Measure + deepTree.Measure);
			DeepTree = deepTree;
			RightDigit = rightDigit;
			LeftDigit = leftDigit;
		}

		public override T Left
		{
			get
			{
				return LeftDigit.Left;
			}
		}

		public override T Right
		{
			get
			{
				return RightDigit.Right;
			}
		}

		public override FTree<T> AddLeft(T item)
		{
			if (LeftDigit.Size < 4)
			{
				return new Compound<T>(Measure + item.Measure, LeftDigit.AddLeft(item), DeepTree, RightDigit);
			}
			Digit<T> leftmost;
			Digit<T> rightmost;
			LeftDigit.AddLeftSplit(item, out leftmost, out rightmost);
			FTree<Digit<T>> newDeep = DeepTree.AddLeft(rightmost);
			return new Compound<T>(Measure + item.Measure, leftmost, newDeep, RightDigit);
		}

		public override FTree<T> AddRight(T item)
		{
			if (RightDigit.Size < 4)
			{
				return new Compound<T>(Measure + item.Measure, LeftDigit, DeepTree, RightDigit.AddRight(item));
			}

			Digit<T> leftmost;
			Digit<T> rightmost;
			RightDigit.AddRightSplit(item, out leftmost, out rightmost);
			FTree<Digit<T>> newDeep = DeepTree.AddRight(leftmost);
			return new Compound<T>(Measure + item.Measure, LeftDigit, newDeep, rightmost);
		}

		public override FTree<T> DropLeft()
		{
			if (LeftDigit.Size > 1)
			{
				Digit<T> new_left = LeftDigit.PopLeft();
				int new_measure = Measure - LeftDigit.Left.Measure;
				return new Compound<T>(new_measure, new_left, DeepTree, RightDigit);
			}
			if (DeepTree.Measure > 0)
			{
				Digit<T> new_left = DeepTree.Left;
				FTree<Digit<T>> new_deep = DeepTree.DropLeft();
				int new_measure = Measure - LeftDigit.Measure;
				return new Compound<T>(new_measure, new_left, new_deep, RightDigit);
			}
			return new Single<T>(RightDigit.Measure, RightDigit);
		}

		public override FTree<T> DropRight()
		{
			if (RightDigit.Size > 1)
			{
				Digit<T> new_right = RightDigit.PopRight();
				int new_measure = Measure - RightDigit.Right.Measure;
				return new Compound<T>(new_measure, LeftDigit, DeepTree, new_right);
			}
			if (DeepTree.Measure > 0)
			{
				Digit<T> new_right = DeepTree.Right;
				FTree<Digit<T>> new_deep = DeepTree.DropRight();
				int new_measure = Measure - RightDigit.Measure;
				return new Compound<T>(new_measure, LeftDigit, new_deep, new_right);
			}
			return new Single<T>(LeftDigit.Measure, LeftDigit);
		}

		public override bool IterBackWhile(Func<Measured, bool> action)
		{
			if (!RightDigit.IterBackWhile(action)) return false;
			if (!DeepTree.IterBackWhile(action)) return false;
			if (!LeftDigit.IterBackWhile(action)) return false;
			return true;
		}

		public override bool IterWhile(Func<Measured, bool> action)
		{
			if (!LeftDigit.IterWhile(action)) return false;
			if (!DeepTree.IterWhile(action)) return false;
			if (!RightDigit.IterWhile(action)) return false;
			return true;
		}

		public override Measured Get(int index)
		{
			index.Is(i => i < Measure);
			int m1 = LeftDigit.Measure;
			int m2 = DeepTree.Measure + m1;

			if (index < m1)
				return LeftDigit[index];
			if (index < m2)
				return DeepTree.Get(index - m1);
			if (index < Measure)
				return RightDigit[index - m2];

			throw Errors.Index_out_of_range;
		}

		public override IEnumerator<Measured> GetEnumerator()
		{
			return new CompoundEnumerator<T>(this);
		}

		public override FTree<T> Insert(int index, Measured value)
		{
			int splitCode = WhereIsThisIndex(index);

			switch (splitCode)
			{
				case 0:
				case 1:
					Digit<T> left_l, left_r;
					LeftDigit.Insert(index, value, out left_l, out left_r);
					if (left_r != null)
					{
						return new Compound<T>(Measure + 1, left_l, DeepTree.AddLeft(left_r), RightDigit);
					}
					return new Compound<T>(Measure + 1, left_l, DeepTree, RightDigit);
				case 2:
				case 3:
					return new Compound<T>(Measure + 1, LeftDigit, DeepTree.Insert(index - LeftDigit.Measure, value), RightDigit);
				case 4:
				case 5:
					Digit<T> right_l, right_r;
					RightDigit.Insert(index - LeftDigit.Measure - DeepTree.Measure, value, out right_l, out right_r);
					if (right_r != null)
					{
						return new Compound<T>(Measure + 1, LeftDigit, DeepTree.AddRight(right_l), right_r);
					}
					return new Compound<T>(Measure + 1, LeftDigit, DeepTree, right_l);
				default:
					throw Errors.Invalid_execution_path;
			}
		}

		public override void Iter(Action<Measured> action)
		{
			action.IsNotNull();
			LeftDigit.Iter(action);
			DeepTree.Iter(action);
			RightDigit.Iter(action);
		}

		public override void IterBack(Action<Measured> action)
		{
			action.IsNotNull();
			RightDigit.IterBack(action);
			DeepTree.IterBack(action);
			LeftDigit.IterBack(action);
		}

		public override FTree<T> Reverse()
		{
			Digit<T> first = LeftDigit.Reverse();
			FTree<Digit<T>> deep = DeepTree.Reverse();
			Digit<T> last = RightDigit.Reverse();
			return new Compound<T>(Measure, last, deep, first);
		}

		public override FTree<T> Set(int index, Measured value)
		{
			int splitCode = WhereIsThisIndex(index);
			switch (splitCode)
			{
				case 0:
				case 1:
					Digit<T> new_left = LeftDigit.Set(index, value);
					return new Compound<T>(Measure, new_left, DeepTree, RightDigit);
				case 2:
				case 3:
					if (DeepTree.Measure == 0) goto case 4;
					FTree<Digit<T>> new_deep = DeepTree.Set(index - LeftDigit.Measure, value);
					return new Compound<T>(Measure, LeftDigit, new_deep, RightDigit);
				case 4:
				case 5:
					Digit<T> new_right = RightDigit.Set(index - LeftDigit.Measure - DeepTree.Measure, value);
					return new Compound<T>(Measure, LeftDigit, DeepTree, new_right);
				default:
					throw Errors.Index_out_of_range;
			}
		}

		public override void Split(int count, out FTree<T> leftmost, out FTree<T> rightmost)
		{
			int splitCode = WhereIsThisIndex(count);
			count.Is(i => i < Measure && i >= 0);

			switch (splitCode)
			{
				case 0:
					leftmost = Empty<T>.Instance;
					rightmost = this;
					return;
				case 1:
					Digit<T> left_1, left_2;
					LeftDigit.Split(count, out left_1, out left_2);
					leftmost = left_1 != null ? (FTree<T>) new Single<T>(left_1.Measure, left_1) : Empty<T>.Instance;
					rightmost = CreateCheckNull(Measure - count, left_2, DeepTree, RightDigit);
					return;
				case 2:
					leftmost = new Single<T>(LeftDigit.Measure, LeftDigit);
					rightmost = CreateCheckNull(Measure - leftmost.Measure, null, DeepTree, RightDigit);
					return;
				case 3:
					FTree<Digit<T>> tree_1, tree_2;
					DeepTree.Split(count - LeftDigit.Measure, out tree_1, out tree_2);
					leftmost = CreateCheckNull(count, LeftDigit, tree_1);
					rightmost = CreateCheckNull(Measure - count, null, tree_2, RightDigit);
					return;
				case 4:
					leftmost = CreateCheckNull(count, LeftDigit, DeepTree);
					rightmost = new Single<T>(Measure - count, RightDigit);
					return;
				case 5:
					Digit<T> right_1, right_2;
					RightDigit.Split(count - LeftDigit.Measure - DeepTree.Measure, out right_1, out right_2);
					leftmost = CreateCheckNull(count, LeftDigit, DeepTree, right_1);
					rightmost = CreateCheckNull(Measure - count, right_2);
					return;
				case 6:
					leftmost = this;
					rightmost = Empty<T>.Instance;
					return;
				default:
					throw Errors.Invalid_execution_path;
			}
		}

		/// <summary>
		/// Returns 0 if count is 0 (meaning, empty).
		/// Returns 1 if count is in left digit
		/// Returns 2 if count encompasses the left digit.
		/// Returns 3 if he count is in the deep tree.
		/// Returns 4 if the count encompasses the left digit + deep tree
		/// Returns 5 if the count is in the right digit.
		/// Returns 6 if the count encompasses the entire tree.
		/// Returns 7 if the count is outside the tree.
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		private int WhereIsThisIndex(int count)
		{
			count.Is(i => i < Measure);
			if (count == 0) return 0;
			if (count < LeftDigit.Measure) return 1;
			if (count == LeftDigit.Measure) return 2;
			if (count < LeftDigit.Measure + DeepTree.Measure) return 3;
			if (count == LeftDigit.Measure + DeepTree.Measure) return 4;
			if (count < Measure) return 5;
			if (count == Measure) return 6;
			throw new Exception();
		}

		//+ Implementation
		//  This function creates an FTree of the right type, depending on which digits are null.
		//  The tree cannot be null, but can be empty.

		private static FTree<T> CreateCheckNull(int measure, Digit<T> left = null, FTree<Digit<T>> deep = null,
		                                        Digit<T> right = null)
		{
			int caseCode = left != null ? 1 << 0 : 0;
			caseCode |= (deep != null && deep.Measure != 0) ? 1 << 1 : 0;
			caseCode |= right != null ? 1 << 2 : 0;

			switch (caseCode)
			{
				case 0:
					return Empty<T>.Instance;
				case 1 << 0:
					return new Single<T>(measure, left);
				case 1 << 0 | 1 << 1:
					FTree<Digit<T>> deep_1 = deep.DropRight();
					Digit<T> r_2 = deep.Right;
					return new Compound<T>(measure, left, deep.DropRight(), deep.Right);
				case 1 << 0 | 1 << 1 | 1 << 2:
					return new Compound<T>(measure, left, deep, right);
				case 1 << 1 | 1 << 2:
					return new Compound<T>(measure, deep.Left, deep.DropLeft(), right);
				case 1 << 0 | 1 << 2:
					return new Compound<T>(measure, left, deep, right);
				case 1 << 1:
					left = deep.Left;
					deep = deep.DropLeft();
					if (deep.Measure != 0)
					{
						right = deep.Right;
						deep = deep.DropRight();
						return new Compound<T>(measure, left, deep, right);
					}
					return new Single<T>(measure, left);
				case 1 << 2:
					return new Single<T>(measure, right);
				default:
					throw Errors.Invalid_execution_path;
			}
		}
	}
}