﻿using System;
using System.Collections.Generic;

namespace g3
{
	/// <summary>
	/// Minimalist implicit function interface
	/// </summary>
    public interface ImplicitFunction3d
    {
        double Value(ref Vector3d pt);
    }


	/// <summary>
	/// Bounded implicit function has a bounding box within which
	/// the "interesting" part of the function is contained 
	/// (eg the surface)
	/// </summary>
	public interface BoundedImplicitFunction3d : ImplicitFunction3d
	{
		AxisAlignedBox3d Bounds();
	}


	/// <summary>
	/// Implicit sphere, where zero isocontour is at Radius
	/// </summary>
	public class ImplicitSphere3d : BoundedImplicitFunction3d
    {
		public Vector3d Origin;
		public double Radius;

        public double Value(ref Vector3d pt)
        {
			return pt.Distance(ref Origin) - Radius;
        }

		public AxisAlignedBox3d Bounds()
		{
			return new AxisAlignedBox3d(Origin, Radius);
		}
    }


	/// <summary>
	/// Implicit half-space. "Inside" is opposite of Normal direction.
	/// </summary>
	public class ImplicitHalfSpace3d : BoundedImplicitFunction3d
	{
		public Vector3d Origin;
		public Vector3d Normal;

		public double Value(ref Vector3d pt)
		{
			return (pt - Origin).Dot(Normal);
		}

		public AxisAlignedBox3d Bounds()
		{
			return new AxisAlignedBox3d(Origin, MathUtil.Epsilon);
		}
	}



	/// <summary>
	/// Implicit axis-aligned box
	/// </summary>
	public class ImplicitAxisAlignedBox3d : BoundedImplicitFunction3d
	{
		public AxisAlignedBox3d AABox;

		public double Value(ref Vector3d pt)
		{
			return AABox.SignedDistance(pt);
		}

		public AxisAlignedBox3d Bounds()
		{
			return AABox;
		}
	}



	/// <summary>
	/// Implicit oriented box
	/// </summary>
	public class ImplicitBox3d : BoundedImplicitFunction3d
	{
		Box3d box;
		AxisAlignedBox3d local_aabb;
		AxisAlignedBox3d bounds_aabb;
		public Box3d Box {
			get { return box; }
			set {
				box = value;
				local_aabb = new AxisAlignedBox3d(
					-Box.Extent.x, -Box.Extent.y, -Box.Extent.z,
					Box.Extent.x, Box.Extent.y, Box.Extent.z);
				bounds_aabb = box.ToAABB();
			}
		}


		public double Value(ref Vector3d pt)
		{
			double dx = (pt - Box.Center).Dot(Box.AxisX);
			double dy = (pt - Box.Center).Dot(Box.AxisY);
			double dz = (pt - Box.Center).Dot(Box.AxisZ);
			return local_aabb.SignedDistance(new Vector3d(dx, dy, dz));
		}

		public AxisAlignedBox3d Bounds()
		{
			return bounds_aabb;
		}
	}



	/// <summary>
	/// Implicit tube around line segment
	/// </summary>
	public class ImplicitLine3d : BoundedImplicitFunction3d
	{
		public Segment3d Segment;
		public double Radius;

		public double Value(ref Vector3d pt)
		{
			double d = Math.Sqrt(Segment.DistanceSquared(pt));
			return d - Radius;
		}

		public AxisAlignedBox3d Bounds()
		{
			Vector3d o = Radius * Vector3d.One, p0 = Segment.P0, p1 = Segment.P1;
			AxisAlignedBox3d box = new AxisAlignedBox3d(p0 - o, p0 + o);
			box.Contain(p1 - o);
			box.Contain(p1 + o);
			return box;
		}
	}




	/// <summary>
	/// Offset the zero-isocontour of an implicit function.
	/// Assumes that negative is inside, if not, reverse offset.
	/// </summary>
	public class ImplicitOffset3d : BoundedImplicitFunction3d
	{
		public BoundedImplicitFunction3d A;
		public double Offset;

		public double Value(ref Vector3d pt)
		{
			return A.Value(ref pt) - Offset;
		}

		public AxisAlignedBox3d Bounds()
		{
			AxisAlignedBox3d box = A.Bounds();
			box.Expand(Offset);
			return box;
		}
	}



	/// <summary>
	/// Boolean Union of two implicit functions, A OR B.
	/// Assumption is that both have surface at zero isocontour and 
	/// negative is inside.
	/// </summary>
	public class ImplicitUnion3d : BoundedImplicitFunction3d
	{
		public BoundedImplicitFunction3d A;
		public BoundedImplicitFunction3d B;

		public double Value(ref Vector3d pt)
		{
			return Math.Min(A.Value(ref pt), B.Value(ref pt));
		}

		public AxisAlignedBox3d Bounds()
		{
			var box = A.Bounds();
			box.Contain(B.Bounds());
			return box;
		}
	}



	/// <summary>
	/// Boolean Intersection of two implicit functions, A AND B
	/// Assumption is that both have surface at zero isocontour and 
	/// negative is inside.
	/// </summary>
	public class ImplicitIntersection3d : BoundedImplicitFunction3d
	{
		public BoundedImplicitFunction3d A;
		public BoundedImplicitFunction3d B;

		public double Value(ref Vector3d pt)
		{
			return Math.Max(A.Value(ref pt), B.Value(ref pt));
		}

		public AxisAlignedBox3d Bounds()
		{
            // [TODO] intersect boxes
			var box = A.Bounds();
			box.Contain(B.Bounds());
			return box;
		}
	}



	/// <summary>
	/// Boolean Difference/Subtraction of two implicit functions A-B = A AND (NOT B)
	/// Assumption is that both have surface at zero isocontour and 
	/// negative is inside.
	/// </summary>
	public class ImplicitDifference3d : BoundedImplicitFunction3d
	{
		public BoundedImplicitFunction3d A;
		public BoundedImplicitFunction3d B;

		public double Value(ref Vector3d pt)
		{
			return Math.Max(A.Value(ref pt), -B.Value(ref pt));
		}

		public AxisAlignedBox3d Bounds()
		{
			// [TODO] can actually subtract B.Bounds() here...
			return A.Bounds();
		}
	}




	/// <summary>
	/// Boolean Union of N implicit functions, A OR B.
	/// Assumption is that both have surface at zero isocontour and 
	/// negative is inside.
	/// </summary>
	public class ImplicitNaryUnion3d : BoundedImplicitFunction3d
	{
		public List<BoundedImplicitFunction3d> Children;

		public double Value(ref Vector3d pt)
		{
			double f = Children[0].Value(ref pt);
			int N = Children.Count;
			for (int k = 1; k < N; ++k)
				f = Math.Min(f, Children[k].Value(ref pt));
			return f;
		}

		public AxisAlignedBox3d Bounds()
		{
			var box = Children[0].Bounds();
			int N = Children.Count;
			for (int k = 1; k < N; ++k)
				box.Contain(Children[k].Bounds());
			return box;
		}
	}





	/// <summary>
	/// Boolean Difference of N implicit functions, A - Union(B1..BN)
	/// Assumption is that both have surface at zero isocontour and 
	/// negative is inside.
	/// </summary>
	public class ImplicitNaryDifference3d : BoundedImplicitFunction3d
	{
		public BoundedImplicitFunction3d A;
		public List<BoundedImplicitFunction3d> BSet;

		public double Value(ref Vector3d pt)
		{
			double fA = A.Value(ref pt);
			int N = BSet.Count;
			if (N == 0)
				return fA;
			double fB = BSet[0].Value(ref pt);
			for (int k = 1; k < N; ++k)
				fB = Math.Min(fB, BSet[k].Value(ref pt));
			return Math.Max(fA, -fB);
		}

		public AxisAlignedBox3d Bounds()
		{
			// [TODO] could actually subtract other bounds here...
			return A.Bounds();
		}
	}




    /// <summary>
    /// Continuous R-Function Boolean Union of two implicit functions, A OR B.
    /// Assumption is that both have surface at zero isocontour and 
    /// negative is inside.
    /// </summary>
    public class ImplicitSmoothUnion3d : BoundedImplicitFunction3d
    {
        public BoundedImplicitFunction3d A;
        public BoundedImplicitFunction3d B;

        const double mul = 1.0 / 1.5;

        public double Value(ref Vector3d pt) {
			double fA = A.Value(ref pt);
			double fB = B.Value(ref pt);
			return mul * (fA + fB - Math.Sqrt(fA*fA + fB*fB - fA*fB));
        }

        public AxisAlignedBox3d Bounds() {
            var box = A.Bounds();
            box.Contain(B.Bounds());
            return box;
        }
    }



    /// <summary>
    /// Continuous R-Function Boolean Intersection of two implicit functions, A-B = A AND (NOT B)
    /// Assumption is that both have surface at zero isocontour and 
    /// negative is inside.
    /// </summary>
    public class ImplicitSmoothIntersection3d : BoundedImplicitFunction3d
    {
        public BoundedImplicitFunction3d A;
        public BoundedImplicitFunction3d B;

        const double mul = 1.0 / 1.5;

        public double Value(ref Vector3d pt) {
            double fA = A.Value(ref pt);
            double fB = B.Value(ref pt);
            return mul * (fA + fB + Math.Sqrt(fA*fA + fB*fB - fA*fB));
        }

        public AxisAlignedBox3d Bounds() {
            var box = A.Bounds();
            box.Contain(B.Bounds());
            return box;
        }
    }




    /// <summary>
    /// Continuous R-Function Boolean Difference of two implicit functions, A AND B
    /// Assumption is that both have surface at zero isocontour and 
    /// negative is inside.
    /// </summary>
    public class ImplicitSmoothDifference3d : BoundedImplicitFunction3d
    {
        public BoundedImplicitFunction3d A;
        public BoundedImplicitFunction3d B;

        const double mul = 1.0 / 1.5;

        public double Value(ref Vector3d pt) {
            double fA = A.Value(ref pt);
            double fB = -B.Value(ref pt);
            return mul * (fA + fB + Math.Sqrt(fA*fA + fB*fB - fA*fB));
        }

        public AxisAlignedBox3d Bounds() {
            var box = A.Bounds();
            box.Contain(B.Bounds());
            return box;
        }
    }




    /// <summary>
    /// Blend of two implicit surfaces. Assumes surface is at zero iscontour.
    /// Uses Pasko blend from http://www.hyperfun.org/F-rep.pdf
    /// </summary>
    public class ImplicitBlend3d : BoundedImplicitFunction3d
	{
		public BoundedImplicitFunction3d A;
		public BoundedImplicitFunction3d B;

		/// <summary>Blending power. 0 is Union, 2 is nice blend.</summary>
		public double Blend {
			get { return blend; }
			set { blend = MathUtil.Clamp(value, 0.0, 100000); }
		}
		double blend = 2.0;

		public double Value(ref Vector3d pt)
		{
			double fA = A.Value(ref pt);
			double fB = B.Value(ref pt);
			double sqr_sum = fA*fA + fB*fB;
            Util.gDevAssert(sqr_sum != double.PositiveInfinity);
			double b = blend / (1.0 + sqr_sum);
			//double a = 0.5;
			//return (1.0/(1.0+a)) * (fA + fB - Math.Sqrt(fA*fA + fB*fB - 2*a*fA*fB)) - b;
            return 0.666666 * (fA + fB - Math.Sqrt(sqr_sum - fA*fB)) - b;
		}

		public AxisAlignedBox3d Bounds()
		{
			var box = A.Bounds();
			box.Contain(B.Bounds());
            box.Expand( 0.5 * blend * box.MaxDim );
			return box;
		}
	}



}
