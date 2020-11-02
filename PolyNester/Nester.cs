using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ClipperLib;

namespace PolyNester
{
    using Ngon = List<IntPoint>;
    using Ngons = List<List<IntPoint>>;

    public enum NFPQUALITY { Simple, Convex, ConcaveLight, ConcaveMedium, ConcaveHigh, ConcaveFull, Full }

    public struct Vector64
    {
        public double X;
        public double Y;
        public Vector64(double X, double Y)
        {
            this.X = X; this.Y = Y;
        }

        public Vector64(Vector64 pt)
        {
            this.X = pt.X; this.Y = pt.Y;
        }

        public static Vector64 operator +(Vector64 a, Vector64 b)
        {
            return new Vector64(a.X + b.X, a.Y + b.Y);
        }

        public static Vector64 operator -(Vector64 a, Vector64 b)
        {
            return new Vector64(a.X - b.X, a.Y - b.Y);
        }

        public static Vector64 operator *(Vector64 a, Vector64 b)
        {
            return new Vector64(a.X * b.X, a.Y * b.Y);
        }

        public static Vector64 operator /(Vector64 a, Vector64 b)
        {
            return new Vector64(a.X / b.X, a.Y / b.Y);
        }

        public static Vector64 operator *(Vector64 a, double b)
        {
            return new Vector64(a.X * b, a.Y * b);
        }

        public static Vector64 operator *(double b, Vector64 a)
        {
            return new Vector64(a.X * b, a.Y * b);
        }

        public static bool operator ==(Vector64 a, Vector64 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(Vector64 a, Vector64 b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is Vector64)
            {
                Vector64 a = (Vector64)obj;
                return (X == a.X) && (Y == a.Y);
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return (X.GetHashCode() ^ Y.GetHashCode());
        }

    }

    public struct Rect64
    {
        public double left;
        public double right;
        public double top;
        public double bottom;

        public Rect64(double l, double t, double r, double b)
        {
            left = l;
            right = r;
            top = t;
            bottom = b;
        }

        public double Width()
        {
            return Math.Abs(left - right);
        }

        public double Height()
        {
            return Math.Abs(top - bottom);
        }

        public double Area()
        {
            return Width() * Height();
        }

        public double Aspect()
        {
            return Width() / Height();
        }
    }

    public struct Mat3x3
    {
        double x11, x12, x13, x21, x22, x23, x31, x32, x33;

        public static Mat3x3 Eye()
        {
            return Scale(1, 1, 1);
        }

        public static Mat3x3 RotateCounterClockwise(double t)
        {
            Mat3x3 T = new Mat3x3();

            double c = Math.Cos(t);
            double s = Math.Sin(t);

            T.x11 = c;
            T.x12 = -s;
            T.x13 = 0;
            T.x21 = s;
            T.x22 = c;
            T.x23 = 0;
            T.x31 = 0;
            T.x32 = 0;
            T.x33 = 1;

            return T;
        }

        public static Mat3x3 Scale(double scale_x, double scale_y, double scale_z = 1.0)
        {
            Mat3x3 I = new Mat3x3();
            I.x11 = scale_x;
            I.x12 = 0;
            I.x13 = 0;
            I.x21 = 0;
            I.x22 = scale_y;
            I.x23 = 0;
            I.x31 = 0;
            I.x32 = 0;
            I.x33 = scale_z;

            return I;
        }

        public static Mat3x3 Translate(double t_x, double t_y)
        {
            Mat3x3 I = new Mat3x3();
            I.x11 = 1;
            I.x12 = 0;
            I.x13 = t_x;

            I.x21 = 0;
            I.x22 = 1;
            I.x23 = t_y;
            I.x31 = 0;
            I.x32 = 0;
            I.x33 = 1;

            return I;
        }

        public static Mat3x3 operator *(Mat3x3 A, Mat3x3 B)
        {
            Mat3x3 I = new Mat3x3();
            I.x11 = A.x11 * B.x11 + A.x12 * B.x21 + A.x13 * B.x31;
            I.x12 = A.x11 * B.x12 + A.x12 * B.x22 + A.x13 * B.x32;
            I.x13 = A.x11 * B.x13 + A.x12 * B.x23 + A.x13 * B.x33;
            I.x21 = A.x21 * B.x11 + A.x22 * B.x21 + A.x23 * B.x31;
            I.x22 = A.x21 * B.x12 + A.x22 * B.x22 + A.x23 * B.x32;
            I.x23 = A.x21 * B.x13 + A.x22 * B.x23 + A.x23 * B.x33;
            I.x31 = A.x31 * B.x11 + A.x32 * B.x21 + A.x33 * B.x31;
            I.x32 = A.x31 * B.x12 + A.x32 * B.x22 + A.x33 * B.x32;
            I.x33 = A.x31 * B.x13 + A.x32 * B.x23 + A.x33 * B.x33;

            return I;
        }

        public static Vector64 operator *(Mat3x3 A, Vector64 B)
        {
            Vector64 v = new Vector64();
            v.X = A.x11 * B.X + A.x12 * B.Y + A.x13;
            v.Y = A.x21 * B.X + A.x22 * B.Y + A.x23;

            return v;
        }

        public static IntPoint operator *(Mat3x3 A, IntPoint B)
        {
            double x = A.x11 * B.X + A.x12 * B.Y + A.x13;
            double y = A.x21 * B.X + A.x22 * B.Y + A.x23;

            return new IntPoint(x, y);
        }

        private double Det2x2(double a11, double a12, double a21, double a22)
        {
            return a11 * a22 - a12 * a21;
        }

        public double Determinant()
        {
            return x11 * Det2x2(x22, x23, x32, x33) - x12 * Det2x2(x21, x23, x31, x33) + x13 * Det2x2(x21, x22, x31, x32);
        }

        public Mat3x3 Inverse()
        {
            double D = Determinant();

            Mat3x3 I = new Mat3x3();
            I.x11 = Det2x2(x22, x23, x32, x33) / D;
            I.x12 = Det2x2(x13, x12, x33, x32) / D;
            I.x13 = Det2x2(x12, x13, x22, x23) / D;
            I.x21 = Det2x2(x23, x21, x33, x31) / D;
            I.x22 = Det2x2(x11, x13, x31, x33) / D;
            I.x23 = Det2x2(x13, x11, x23, x21) / D;
            I.x31 = Det2x2(x21, x22, x31, x32) / D;
            I.x32 = Det2x2(x12, x11, x32, x31) / D;
            I.x33 = Det2x2(x11, x12, x21, x22) / D;

            return I;
        }
    }

    public static class ConverterUtility
    {
        public static IntPoint ToIntPoint(this Vector64 vec) { return new IntPoint(vec.X, vec.Y); }
        public static Vector64 ToVector64(this IntPoint vec) { return new Vector64(vec.X, vec.Y); }
        public static IntRect ToIntRect(this Rect64 rec) { return new IntRect((long)rec.left, (long)rec.top, (long)rec.right, (long)rec.bottom); }
        public static Rect64 ToRect64(this IntRect rec) { return new Rect64(rec.left, rec.top, rec.right, rec.bottom); }
    }

    public static class GeomUtility
    {
        private class PolarComparer : IComparer
        {
            public static int CompareIntPoint(IntPoint A, IntPoint B)
            {
                long det = A.Y * B.X - A.X * B.Y;

                if (det == 0)
                {
                    long dot = A.X * B.X + A.Y * B.Y;
                    if (dot >= 0)
                        return 0;
                }

                if (A.Y == 0 && A.X > 0)
                    return -1;
                if (B.Y == 0 && B.X > 0)
                    return 1;
                if (A.Y > 0 && B.Y < 0)
                    return -1;
                if (A.Y < 0 && B.Y > 0)
                    return 1;
                return det > 0 ? 1 : -1;
            }

            int IComparer.Compare(object a, object b)
            {
                IntPoint A = (IntPoint)a;
                IntPoint B = (IntPoint)b;

                return CompareIntPoint(A, B);
            }
        }

        public static long Width(this IntRect rect)
        {
            return Math.Abs(rect.left - rect.right);
        }

        public static long Height(this IntRect rect)
        {
            return Math.Abs(rect.top - rect.bottom);
        }

        public static long Area(this IntRect rect)
        {
            return rect.Width() * rect.Height();
        }

        public static double Aspect(this IntRect rect)
        {
            return ((double)rect.Width()) / rect.Height();
        }

        public static Ngon Clone(this Ngon poly)
        {
            return new Ngon(poly);
        }

        public static Ngon Clone(this Ngon poly, long shift_x, long shift_y, bool flip_first = false)
        {
            long scale = flip_first ? -1 : 1;

            Ngon clone = new Ngon(poly.Count);
            for (int i = 0; i < poly.Count; i++)
                clone.Add(new IntPoint(scale * poly[i].X + shift_x, scale * poly[i].Y + shift_y));
            return clone;
        }

        public static Ngon Clone(this Ngon poly, Mat3x3 T)
        {
            Ngon clone = new Ngon(poly.Count);
            for (int i = 0; i < poly.Count; i++)
                clone.Add(T * poly[i]);
            return clone;
        }

        public static Ngons Clone(this Ngons polys)
        {
            Ngons clone = new Ngons(polys.Count);
            for (int i = 0; i < polys.Count; i++)
                clone.Add(polys[i].Clone());
            return clone;
        }

        public static Ngons Clone(this Ngons polys, long shift_x, long shift_y, bool flip_first = false)
        {
            Ngons clone = new Ngons(polys.Count);
            for (int i = 0; i < polys.Count; i++)
                clone.Add(polys[i].Clone(shift_x, shift_y, flip_first));
            return clone;
        }

        public static Ngons Clone(this Ngons polys, Mat3x3 T)
        {
            Ngons clone = new Ngons(polys.Count);
            for (int i = 0; i < polys.Count; i++)
                clone.Add(polys[i].Clone(T));
            return clone;
        }

        public static IntRect GetBounds(IEnumerable<IntPoint> points)
        {
            long width_min = long.MaxValue;
            long width_max = long.MinValue;
            long height_min = long.MaxValue;
            long height_max = long.MinValue;

            foreach (IntPoint p in points)
            {
                width_min = Math.Min(width_min, p.X);
                height_min = Math.Min(height_min, p.Y);
                width_max = Math.Max(width_max, p.X);
                height_max = Math.Max(height_max, p.Y);
            }

            return new IntRect(width_min, height_max, width_max, height_min);
        }

        public static Rect64 GetBounds(IEnumerable<Vector64> points)
        {
            double width_min = double.MaxValue;
            double width_max = double.MinValue;
            double height_min = double.MaxValue;
            double height_max = double.MinValue;

            foreach (Vector64 p in points)
            {
                width_min = Math.Min(width_min, p.X);
                height_min = Math.Min(height_min, p.Y);
                width_max = Math.Max(width_max, p.X);
                height_max = Math.Max(height_max, p.Y);
            }

            return new Rect64(width_min, height_max, width_max, height_min);
        }

        public static void GetRefitTransform(IEnumerable<Vector64> points, Rect64 target, bool stretch, out Vector64 scale, out Vector64 shift)
        {
            Rect64 bds = GetBounds(points);

            scale = new Vector64(target.Width() / bds.Width(), target.Height() / bds.Height());

            if (!stretch)
            {
                double s = Math.Min(scale.X, scale.Y);
                scale = new Vector64(s, s);
            }

            shift = new Vector64(-bds.left, -bds.bottom) * scale
                + new Vector64(Math.Min(target.left, target.right), Math.Min(target.bottom, target.top));
        }

        public static Ngon ConvexHull(Ngon subject, double rigidness = 0)
        {
            if (subject.Count == 0)
                return new Ngon();

            if (rigidness >= 1)
                return subject.Clone();

            subject = subject.Clone();
            if (Clipper.Area(subject) < 0)
                Clipper.ReversePaths(new Ngons() { subject });

            Ngon last_hull = new Ngon();
            Ngon hull = subject;

            double subj_area = Clipper.Area(hull);

            int last_vert = 0;
            for (int i = 1; i < subject.Count; i++)
                if (hull[last_vert].Y > hull[i].Y)
                    last_vert = i;

            while (last_hull.Count != hull.Count)
            {
                last_hull = hull;
                hull = new Ngon();
                hull.Add(last_hull[last_vert]);

                int steps_since_insert = 0;
                int max_steps = rigidness <= 0 ? int.MaxValue : (int)Math.Round(10 - (10 * rigidness));

                int n = last_hull.Count;

                int start = last_vert;
                for (int i = 1; i < n; i++)
                {
                    IntPoint a = last_hull[last_vert];
                    IntPoint b = last_hull[(start + i) % n];
                    IntPoint c = last_hull[(start + i + 1) % n];

                    IntPoint ab = new IntPoint(b.X - a.X, b.Y - a.Y);
                    IntPoint ac = new IntPoint(c.X - a.X, c.Y - a.Y);

                    if (ab.Y * ac.X < ab.X * ac.Y || steps_since_insert >= max_steps)
                    {
                        hull.Add(b);
                        last_vert = (start + i) % n;
                        steps_since_insert = -1;
                    }
                    steps_since_insert++;
                }

                last_vert = 0;

                double hull_area = Clipper.Area(hull);

                if (subj_area / hull_area < Math.Sqrt(rigidness))
                {
                    hull = Clipper.SimplifyPolygon(hull, PolyFillType.pftNonZero)[0];
                    break;
                }
            }

            return hull;
        }

        public static Ngons MinkowskiSumSegment(Ngon pattern, IntPoint p1, IntPoint p2, bool flip_pattern)
        {
            Clipper clipper = new Clipper();

            Ngon p1_c = pattern.Clone(p1.X, p1.Y, flip_pattern);

            if (p1 == p2)
                return new Ngons() { p1_c };

            Ngon p2_c = pattern.Clone(p2.X, p2.Y, flip_pattern);

            Ngons full = new Ngons();
            clipper.AddPath(p1_c, PolyType.ptSubject, true);
            clipper.AddPath(p2_c, PolyType.ptSubject, true);
            clipper.AddPaths(Clipper.MinkowskiSum(pattern.Clone(0, 0, flip_pattern), new Ngon() { p1, p2 }, false), PolyType.ptSubject, true);
            clipper.Execute(ClipType.ctUnion, full, PolyFillType.pftNonZero);

            return full;
        }

        public static Ngons MinkowskiSumBoundary(Ngon pattern, Ngon path, bool flip_pattern)
        {
            Clipper clipper = new Clipper();

            Ngons full = new Ngons();

            for (int i = 0; i < path.Count; i++)
            {
                IntPoint p1 = path[i];
                IntPoint p2 = path[(i + 1) % path.Count];

                Ngons seg = MinkowskiSumSegment(pattern, p1, p2, flip_pattern);
                clipper.AddPaths(full, PolyType.ptSubject, true);
                clipper.AddPaths(seg, PolyType.ptSubject, true);

                Ngons res = new Ngons();
                clipper.Execute(ClipType.ctUnion, res, PolyFillType.pftNonZero);
                full = res;
                clipper.Clear();
            }

            return full;
        }

        public static Ngons MinkowskiSumBoundary(Ngon pattern, Ngons path, bool flip_pattern)
        {
            Clipper clipper = new Clipper();

            Ngons full = new Ngons();

            for (int i = 0; i < path.Count; i++)
            {
                Ngons seg = MinkowskiSumBoundary(pattern, path[i], flip_pattern);
                clipper.AddPaths(full, PolyType.ptSubject, true);
                clipper.AddPaths(seg, PolyType.ptSubject, true);

                Ngons res = new Ngons();
                clipper.Execute(ClipType.ctUnion, res, PolyFillType.pftNonZero);
                full = res;
                clipper.Clear();
            }

            return full;
        }

        private static Ngons MSumSimple(Ngon pattern, Ngons subject, bool flip_pattern)
        {
            IntRect pB = GetBounds(pattern);
            IntRect sB = GetBounds(subject[0]);

            if (flip_pattern)
            {
                pB = new IntRect(-pB.right, -pB.bottom, -pB.left, -pB.top);
            }

            long l = pB.left + sB.left;
            long r = pB.right + sB.right;
            long t = pB.top + sB.top;
            long b = pB.bottom + sB.bottom;

            Ngon p = new Ngon() { new IntPoint(l, b), new IntPoint(r, b), new IntPoint(r, t), new IntPoint(l, t) };
            return new Ngons() { p };
        }

        private static Ngons MSumConvex(Ngon pattern, Ngons subject, bool flip_pattern)
        {
            Ngon h_p = ConvexHull(pattern.Clone(0, 0, flip_pattern));
            Ngon h_s = ConvexHull(subject[0].Clone());

            int n_p = h_p.Count;
            int n_s = h_s.Count;

            int sp = 0;
            for (int k = 0; k < n_p; k++)
                if (h_p[k].Y < h_p[sp].Y)
                    sp = k;

            int ss = 0;
            for (int k = 0; k < n_s; k++)
                if (h_s[k].Y < h_s[ss].Y)
                    ss = k;

            Ngon poly = new Ngon(n_p + n_s);

            int i = 0;
            int j = 0;
            while (i < n_p || j < n_s)
            {
                int ip = (sp + i + 1) % n_p;
                int jp = (ss + j + 1) % n_s;
                int ii = (sp + i) % n_p;
                int jj = (ss + j) % n_s;

                IntPoint sum = new IntPoint(h_p[ii].X + h_s[jj].X, h_p[ii].Y + h_s[jj].Y);
                IntPoint v = new IntPoint(h_p[ip].X - h_p[ii].X, h_p[ip].Y - h_p[ii].Y);
                IntPoint w = new IntPoint(h_s[jp].X - h_s[jj].X, h_s[jp].Y - h_s[jj].Y);

                poly.Add(sum);

                if (i == n_p)
                {
                    j++;
                    continue;
                }

                if (j == n_s)
                {
                    i++;
                    continue;
                }

                long cross = v.Y * w.X - v.X * w.Y;

                if (cross < 0) i++;
                else if (cross > 0) j++;
                else
                {
                    long dot = v.X * w.X + v.Y * w.Y;
                    if (dot > 0)
                    {
                        i++;
                        j++;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }

            return Clipper.SimplifyPolygon(poly);
        }

        private static Ngons MSumConcave(Ngon pattern, Ngons subject, bool flip_pattern, double rigidness = 1.0)
        {
            Ngon subj = subject[0];
            Ngon patt = pattern.Clone(0, 0, flip_pattern);

            if (rigidness < 1.0)
            {
                subj = ConvexHull(subj, rigidness);
                patt = ConvexHull(patt, rigidness);
            }

            Ngons sres = MinkowskiSumBoundary(patt, subj, false);
            return sres.Count == 0 ? sres : new Ngons() { sres[0] };
        }

        private static Ngons MSumFull(Ngon pattern, Ngons subject, bool flip_pattern)
        {
            Clipper clipper = new Clipper();

            Ngons full = new Ngons();

            long scale = flip_pattern ? -1 : 1;

            for (int i = 0; i < pattern.Count; i++)
                clipper.AddPaths(subject.Clone(scale * pattern[i].X, scale * pattern[i].Y), PolyType.ptSubject, true);

            clipper.Execute(ClipType.ctUnion, full, PolyFillType.pftNonZero);
            clipper.Clear();

            clipper.AddPaths(full, PolyType.ptSubject, true);
            clipper.AddPaths(MinkowskiSumBoundary(pattern, subject, flip_pattern), PolyType.ptSubject, true);

            Ngons res = new Ngons();

            clipper.Execute(ClipType.ctUnion, res, PolyFillType.pftNonZero);

            return res;
        }

        public static Ngons MinkowskiSum(Ngon pattern, Ngons subject, NFPQUALITY quality, bool flip_pattern)
        {
            switch (quality)
            {
                case NFPQUALITY.Simple:
                    return MSumSimple(pattern, subject, flip_pattern);
                case NFPQUALITY.Convex:
                    return MSumConvex(pattern, subject, flip_pattern);
                case NFPQUALITY.ConcaveLight:
                    return MSumConcave(pattern, subject, flip_pattern, 0.25);
                case NFPQUALITY.ConcaveMedium:
                    return MSumConcave(pattern, subject, flip_pattern, 0.55);
                case NFPQUALITY.ConcaveHigh:
                    return MSumConcave(pattern, subject, flip_pattern, 0.85);
                case NFPQUALITY.ConcaveFull:
                    return MSumConcave(pattern, subject, flip_pattern, 1.0);
                case NFPQUALITY.Full:
                    return MSumFull(pattern, subject, flip_pattern);
                default:
                    return null;
            }
        }

        public static Ngon CanFitInsidePolygon(IntRect canvas, Ngon pattern)
        {
            IntRect bds = GetBounds(pattern);

            long l = canvas.left - bds.left;
            long r = canvas.right - bds.right;
            long t = canvas.top - bds.top;
            long b = canvas.bottom - bds.bottom;

            if (l > r || b > t)
                return null;

            return new Ngon() { new IntPoint(l, b), new IntPoint(r, b), new IntPoint(r, t), new IntPoint(l, t) };
        }

        public static double AlignToEdgeRotation(Ngon target, int edge_start)
        {
            edge_start %= target.Count;
            int next_pt = (edge_start + 1) % target.Count;
            IntPoint best_edge = new IntPoint(target[next_pt].X - target[edge_start].X, target[next_pt].Y - target[edge_start].Y);
            return -Math.Atan2(best_edge.Y, best_edge.X);
        }

        public static bool AlmostRectangle(Ngon target, double percent_diff = 0.05)
        {
            IntRect bounds = GetBounds(target);
            double area = Math.Abs(Clipper.Area(target));

            return 1.0 - area / bounds.Area() < percent_diff;
        }
    }

    public class Nester
    {
        private class PolyRef
        {
            public Ngons poly;
            public Mat3x3 trans;

            public IntPoint GetTransformedPoint(int poly_id, int index)
            {
                return trans * poly[poly_id][index];
            }

            public Ngons GetTransformedPoly()
            {
                Ngons n = new Ngons(poly.Count);
                for (int i = 0; i < poly.Count; i++)
                {
                    Ngon nn = new Ngon(poly[i].Count);
                    for (int j = 0; j < poly[i].Count; j++)
                        nn.Add(GetTransformedPoint(i, j));
                    n.Add(nn);
                }
                return n;
            }
        }

        private class Command
        {
            public Action<object[]> Call;
            public object[] param;
        }

        private const long unit_scale = 10000000;

        private List<PolyRef> polygon_lib;  // list of saved polygons for reference by handle, stores raw poly positions and transforms

        private Queue<Command> command_buffer;  // buffers list of commands which will append transforms to elements of poly_lib on execute

        private BackgroundWorker background_worker;     // used to execute command buffer in background

        public int LibSize { get { return polygon_lib.Count; } }

        public Nester()
        {
            polygon_lib = new List<PolyRef>();
            command_buffer = new Queue<Command>();
        }

        public void ExecuteCommandBuffer(Action<ProgressChangedEventArgs> callback_progress, Action<AsyncCompletedEventArgs> callback_completed)
        {
            background_worker = new BackgroundWorker();
            background_worker.WorkerSupportsCancellation = true;
            background_worker.WorkerReportsProgress = true;

            if (callback_progress != null)
                background_worker.ProgressChanged += (sender, e) => callback_progress.Invoke(e);
            if (callback_completed != null)
                background_worker.RunWorkerCompleted += (sender, e) => callback_completed.Invoke(e);

            background_worker.DoWork += Background_worker_DoWork;
            background_worker.RunWorkerCompleted += Background_worker_RunWorkerCompleted;

            background_worker.RunWorkerAsync();
        }

        public void CancelExecute()
        {
            if (!IsBusy())
                return;

            background_worker.CancelAsync();
        }

        private void Background_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                ResetTransformLib();
                command_buffer.Clear();
            }

            background_worker.Dispose();
        }

        private void Background_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (command_buffer.Count > 0)
            {
                Command cmd = command_buffer.Dequeue();
                cmd.Call(cmd.param);

                if (background_worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
            }
        }

        public void ClearCommandBuffer()
        {
            command_buffer.Clear();
        }

        public bool IsBusy()
        {
            return background_worker != null && background_worker.IsBusy;
        }

        private HashSet<int> PreprocessHandles(IEnumerable<int> handles)
        {
            if (handles == null)
                handles = Enumerable.Range(0, polygon_lib.Count);

            HashSet<int> unique = new HashSet<int>();
            foreach (int i in handles)
                unique.Add(i);

            return unique;
        }

        public void CMD_Scale(int handle, double scale_x, double scale_y)
        {
            command_buffer.Enqueue(new Command() { Call = cmd_scale, param = new object[] { handle, scale_x, scale_y } });
        }

        private void cmd_scale(params object[] param)
        {
            int handle = (int)param[0];
            double scale_x = (double)param[1];
            double scale_y = (double)param[2];
            polygon_lib[handle].trans = Mat3x3.Scale(scale_x, scale_y) * polygon_lib[handle].trans;
        }

        public void CMD_Rotate(int handle, double theta)
        {
            command_buffer.Enqueue(new Command() { Call = cmd_rotate, param = new object[] { handle, theta } });
        }

        private void cmd_rotate(params object[] param)
        {
            int handle = (int)param[0];
            double theta = (double)param[1];
            polygon_lib[handle].trans = Mat3x3.RotateCounterClockwise(theta) * polygon_lib[handle].trans;
        }

        public void CMD_Translate(int handle, double translate_x, double translate_y)
        {
            command_buffer.Enqueue(new Command() { Call = cmd_translate, param = new object[] { handle, translate_x, translate_y } });
        }

        private void cmd_translate(params object[] param)
        {
            int handle = (int)param[0];
            double translate_x = (double)param[1];
            double translate_y = (double)param[2];
            polygon_lib[handle].trans = Mat3x3.Translate(translate_x, translate_y) * polygon_lib[handle].trans;
        }

        public void CMD_TranslateOriginToZero(IEnumerable<int> handles)
        {
            command_buffer.Enqueue(new Command() { Call = cmd_translate_origin_to_zero, param = new object[] { handles } });
        }

        private void cmd_translate_origin_to_zero(params object[] param)
        {
            HashSet<int> unique = PreprocessHandles(param[0] as IEnumerable<int>);

            foreach (int i in unique)
            {
                IntPoint o = polygon_lib[i].GetTransformedPoint(0, 0);
                cmd_translate(i, (double)-o.X, (double)-o.Y);
            }
        }

        public void CMD_Refit(Rect64 target, bool stretch, IEnumerable<int> handles)
        {
            command_buffer.Enqueue(new Command() { Call = cmd_refit, param = new object[] { target, stretch, handles } });
        }

        private void cmd_refit(params object[] param)
        {
            Rect64 target = (Rect64)param[0];
            bool stretch = (bool)param[1];
            HashSet<int> unique = PreprocessHandles(param[2] as IEnumerable<int>);

            HashSet<Vector64> points = new HashSet<Vector64>();
            foreach (int i in unique)
                points.UnionWith(polygon_lib[i].poly[0].Select(p => polygon_lib[i].trans * new Vector64(p.X, p.Y)));

            Vector64 scale, trans;
            GeomUtility.GetRefitTransform(points, target, stretch, out scale, out trans);

            foreach (int i in unique)
            {
                cmd_scale(i, scale.X, scale.Y);
                cmd_translate(i, trans.X, trans.Y);
            }
        }

        /// <summary>
        /// Get the optimal quality for tradeoff between speed and precision of NFP
        /// </summary>
        /// <param name="subj_handle"></param>
        /// <param name="pattern_handle"></param>
        /// <returns></returns>
        private NFPQUALITY GetNFPQuality(int subj_handle, int pattern_handle, double max_area_bounds)
        {
            Ngon S = polygon_lib[subj_handle].GetTransformedPoly()[0];
            Ngon P = polygon_lib[pattern_handle].GetTransformedPoly()[0];

            if (GeomUtility.AlmostRectangle(S) && GeomUtility.AlmostRectangle(P))
                return NFPQUALITY.Simple;

            double s_A = GeomUtility.GetBounds(S).Area();
            double p_A = GeomUtility.GetBounds(P).Area();

            if (p_A / s_A > 1000)
                return NFPQUALITY.Simple;

            if (s_A / max_area_bounds < 0.05)
                return NFPQUALITY.Simple;

            if (p_A / s_A > 100)
                return NFPQUALITY.Convex;

            if (p_A / s_A > 50)
                return NFPQUALITY.ConcaveLight;

            if (p_A / s_A > 10)
                return NFPQUALITY.ConcaveMedium;

            if (p_A / s_A > 2)
                return NFPQUALITY.ConcaveHigh;

            if (p_A / s_A > 0.25)
                return NFPQUALITY.ConcaveFull;

            return NFPQUALITY.Full;
        }

        /// <summary>
        /// Parallel kernel for generating NFP of pattern on handle, return the index in the library of this NFP
        /// Decides the optimal quality for this NFP
        /// </summary>
        /// <param name="subj_handle"></param>
        /// <param name="pattern_handle"></param>
        /// <param name="lib_set_at"></param>
        /// <returns></returns>
        private int NFPKernel(int subj_handle, int pattern_handle, double max_area_bounds, int lib_set_at, NFPQUALITY max_quality = NFPQUALITY.Full)
        {
            NFPQUALITY quality = GetNFPQuality(subj_handle, pattern_handle, max_area_bounds);
            quality = (NFPQUALITY)Math.Min((int)quality, (int)max_quality);
            return AddMinkowskiSum(subj_handle, pattern_handle, quality, true, lib_set_at);
        }

        /// <summary>
        /// Regular for loop in the syntax of a parallel for used for debugging
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="body"></param>
        private void For(int i, int j, Action<int> body)
        {
            for (int k = i; k < j; k++)
                body(k);
        }

        public void CMD_Nest(IEnumerable<int> handles, NFPQUALITY max_quality = NFPQUALITY.Full)
        {
            command_buffer.Enqueue(new Command() { Call = cmd_nest, param = new object[] { handles, max_quality } });
        }

        /// <summary>
        /// Nest the collection of handles with minimal enclosing square from origin
        /// </summary>
        /// <param name="handles"></param>
        private void cmd_nest(params object[] param)
        {
            HashSet<int> unique = PreprocessHandles(param[0] as IEnumerable<int>);
            NFPQUALITY max_quality = (NFPQUALITY)param[1];

            cmd_translate_origin_to_zero(unique);

            int n = unique.Count;

            Dictionary<int, IntRect> bounds = new Dictionary<int, IntRect>();
            foreach (int handle in unique)
                bounds.Add(handle, GeomUtility.GetBounds(polygon_lib[handle].GetTransformedPoly()[0]));

            int[] ordered_handles = unique.OrderByDescending(p => Math.Max(bounds[p].Height(), bounds[p].Width())).ToArray();
            double max_bound_area = bounds[ordered_handles[0]].Area();

            int start_cnt = polygon_lib.Count;

            int[] canvas_regions = AddCanvasFitPolygon(ordered_handles);

            int base_cnt = polygon_lib.Count;
            for (int i = 0; i < n * n - n; i++)
                polygon_lib.Add(new PolyRef());

            int update_breaks = 10;
            int nfp_chunk_sz = n * n / update_breaks * update_breaks == n * n ? n * n / update_breaks : n * n / update_breaks + 1;

            // the row corresponds to pattern and col to nfp for this pattern on col subj
            int[,] nfps = new int[n, n];
            for (int k = 0; k < update_breaks; k++)
            {
                int start = k * nfp_chunk_sz;
                int end = Math.Min((k + 1) * nfp_chunk_sz, n * n);

                if (start >= end)
                    break;

                Parallel.For(start, end, i => nfps[i / n, i % n] = i / n == i % n ? -1 : NFPKernel(ordered_handles[i % n], ordered_handles[i / n], max_bound_area, base_cnt + i - (i % n > i / n ? 1 : 0) - i / n, max_quality));

                double progress = Math.Min(((double)(k + 1)) / (update_breaks + 1) * 50.0, 50.0);
                background_worker.ReportProgress((int)progress);

                if (background_worker.CancellationPending)
                    break;
            }

            int place_chunk_sz = Math.Max(n / update_breaks, 1);

            bool[] placed = new bool[n];
            for (int i = 0; i < n; i++)
            {
                if (i % 10 == 0 && background_worker.CancellationPending)
                    break;

                Clipper c = new Clipper();
                c.AddPath(polygon_lib[canvas_regions[i]].poly[0], PolyType.ptSubject, true);
                for (int j = 0; j < i; j++)
                {
                    if (!placed[j])
                        continue;

                    c.AddPaths(polygon_lib[nfps[i, j]].GetTransformedPoly(), PolyType.ptClip, true);
                }
                Ngons fit_region = new Ngons();
                c.Execute(ClipType.ctDifference, fit_region, PolyFillType.pftNonZero);


                IntPoint o = polygon_lib[ordered_handles[i]].GetTransformedPoint(0, 0);
                IntRect bds = bounds[ordered_handles[i]];
                long ext_x = bds.right - o.X;
                long ext_y = bds.top - o.Y;
                IntPoint place = new IntPoint(0, 0);
                long pl_score = long.MaxValue;
                for (int k = 0; k < fit_region.Count; k++)
                    for (int l = 0; l < fit_region[k].Count; l++)
                    {
                        IntPoint cand = fit_region[k][l];
                        long cd_score = Math.Max(cand.X + ext_x, cand.Y + ext_y);
                        if (cd_score < pl_score)
                        {
                            pl_score = cd_score;
                            place = cand;
                            placed[i] = true;
                        }
                    }

                if (!placed[i])
                    continue;

                cmd_translate(ordered_handles[i], (double)(place.X - o.X), (double)(place.Y - o.Y));
                for (int k = i + 1; k < n; k++)
                    cmd_translate(nfps[k, i], (double)(place.X - o.X), (double)(place.Y - o.Y));

                if (i % place_chunk_sz == 0)
                {
                    double progress = Math.Min(60.0 + ((double)(i / place_chunk_sz)) / (update_breaks + 1) * 40.0, 100.0);
                    background_worker.ReportProgress((int)progress);
                }
            }

            // remove temporary added values
            polygon_lib.RemoveRange(start_cnt, polygon_lib.Count - start_cnt);
        }

        public void CMD_OptimalRotation(IEnumerable<int> handles)
        {
            command_buffer.Enqueue(new Command() { Call = cmd_optimal_rotation, param = new object[] { handles } });
        }

        private void cmd_optimal_rotation(int handle)
        {
            Ngon hull = polygon_lib[handle].GetTransformedPoly()[0];
            int n = hull.Count;

            double best_t = 0;
            int best = 0;
            long best_area = long.MaxValue;
            bool flip_best = false;

            for (int i = 0; i < n; i++)
            {
                double t = GeomUtility.AlignToEdgeRotation(hull, i);

                Mat3x3 rot = Mat3x3.RotateCounterClockwise(t);

                Ngon clone = hull.Clone(rot);

                IntRect bounds = GeomUtility.GetBounds(clone);
                long area = bounds.Area();
                double aspect = bounds.Aspect();

                if (area < best_area)
                {
                    best_area = area;
                    best = i;
                    best_t = t;
                    flip_best = aspect > 1.0;
                }
            }

            double flip = flip_best ? Math.PI * 0.5 : 0;
            IntPoint around = hull[best];

            cmd_translate(handle, (double)-around.X, (double)-around.Y);
            cmd_rotate(handle, best_t + flip);
            cmd_translate(handle, (double)around.X, (double)around.Y);
        }

        private void cmd_optimal_rotation(params object[] param)
        {
            HashSet<int> unique = PreprocessHandles(param[0] as IEnumerable<int>);

            foreach (int i in unique)
                cmd_optimal_rotation(i);
        }

        /// <summary>
        /// Append a set triangulated polygons to the nester and get handles for each point to the correp. polygon island
        /// </summary>
        /// <param name="points"></param>
        /// <param name="tris"></param>
        /// <returns></returns>
        public int[] AddPolygons(IntPoint[] points, int[] tris, double miter_distance = 0.0)
        {
            // from points to clusters of tris
            int[] poly_map = new int[points.Length];
            for (int i = 0; i < poly_map.Length; i++)
                poly_map[i] = -1;

            HashSet<int>[] graph = new HashSet<int>[points.Length];
            for (int i = 0; i < graph.Length; i++)
                graph[i] = new HashSet<int>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                int t1 = tris[i];
                int t2 = tris[i + 1];
                int t3 = tris[i + 2];

                graph[t1].Add(t2);
                graph[t1].Add(t3);
                graph[t2].Add(t1);
                graph[t2].Add(t3);
                graph[t3].Add(t1);
                graph[t3].Add(t2);
            }

            if (graph.Any(p => p.Count == 0))
                throw new Exception("No singular vertices should exist on mesh");

            int[] clust_ids = new int[points.Length];

            HashSet<int> unmarked = new HashSet<int>(Enumerable.Range(0, points.Length));
            int clust_cnt = 0;
            while (unmarked.Count > 0)
            {
                Queue<int> open = new Queue<int>();
                int first = unmarked.First();
                unmarked.Remove(first);
                open.Enqueue(first);
                while (open.Count > 0)
                {
                    int c = open.Dequeue();
                    clust_ids[c] = clust_cnt;
                    foreach (int n in graph[c])
                    {
                        if (unmarked.Contains(n))
                        {
                            unmarked.Remove(n);
                            open.Enqueue(n);
                        }
                    }
                }

                clust_cnt++;
            }

            Ngons[] clusters = new Ngons[clust_cnt];
            for (int i = 0; i < tris.Length; i += 3)
            {
                int clust = clust_ids[tris[i]];
                if (clusters[clust] == null)
                    clusters[clust] = new Ngons();

                IntPoint p1 = points[tris[i]];
                IntPoint p2 = points[tris[i + 1]];
                IntPoint p3 = points[tris[i + 2]];

                clusters[clust].Add(new Ngon() { p1, p2, p3 });
            }

            List<Ngons> fulls = new List<Ngons>();

            for (int i = 0; i < clust_cnt; i++)
            {
                Ngons cl = clusters[i];

                Clipper c = new Clipper();
                foreach (Ngon n in cl)
                    c.AddPath(n, PolyType.ptSubject, true);

                Ngons full = new Ngons();
                c.Execute(ClipType.ctUnion, full, PolyFillType.pftNonZero);
                full = Clipper.SimplifyPolygons(full, PolyFillType.pftNonZero);

                if (miter_distance > 0.00001)
                {
                    Ngons full_miter = new Ngons();
                    ClipperOffset co = new ClipperOffset();
                    co.AddPaths(full, JoinType.jtMiter, EndType.etClosedPolygon);
                    co.Execute(ref full_miter, miter_distance);
                    full_miter = Clipper.SimplifyPolygons(full_miter, PolyFillType.pftNonZero);
                    fulls.Add(full_miter);
                }
                else
                    fulls.Add(full);
            }

            for (int i = 0; i < clust_ids.Length; i++)
                clust_ids[i] += polygon_lib.Count;

            for (int i = 0; i < fulls.Count; i++)
                polygon_lib.Add(new PolyRef() { poly = fulls[i], trans = Mat3x3.Eye() });

            return clust_ids;
        }

        /// <summary>
        /// Append a set of triangulated polygons to the nester and get handles for points to polygons, coordinates are assumed
        /// to be in UV [0,1] space
        /// </summary>
        /// <param name="points"></param>
        /// <param name="tris"></param>
        /// <returns></returns>
        public int[] AddUVPolygons(Vector64[] points, int[] tris, double miter_distance = 0.0)
        {
            IntPoint[] new_pts = new IntPoint[points.Length];
            for (int i = 0; i < points.Length; i++)
                new_pts[i] = new IntPoint(points[i].X * unit_scale, points[i].Y * unit_scale);

            int start_index = polygon_lib.Count;

            int[] map = AddPolygons(new_pts, tris, miter_distance * unit_scale);

            return map;
        }

        public int AddMinkowskiSum(int subj_handle, int pattern_handle, NFPQUALITY quality, bool flip_pattern, int set_at = -1)
        {
            Ngons A = polygon_lib[subj_handle].GetTransformedPoly();
            Ngons B = polygon_lib[pattern_handle].GetTransformedPoly();

            Ngons C = GeomUtility.MinkowskiSum(B[0], A, quality, flip_pattern);
            PolyRef pref = new PolyRef() { poly = C, trans = Mat3x3.Eye() };

            if (set_at < 0)
                polygon_lib.Add(pref);
            else
                polygon_lib[set_at] = pref;

            return set_at < 0 ? polygon_lib.Count - 1 : set_at;
        }

        public int AddCanvasFitPolygon(IntRect canvas, int pattern_handle)
        {
            Ngon B = polygon_lib[pattern_handle].GetTransformedPoly()[0];

            Ngon C = GeomUtility.CanFitInsidePolygon(canvas, B);
            polygon_lib.Add(new PolyRef() { poly = new Ngons() { C }, trans = Mat3x3.Eye() });
            return polygon_lib.Count - 1;
        }

        public int AddCanvasFitPolygon(Rect64 canvas, int pattern_handle)
        {
            IntRect c = new IntRect((long)canvas.left, (long)canvas.top, (long)canvas.right, (long)canvas.bottom);
            return AddCanvasFitPolygon(c, pattern_handle);
        }

        public int[] AddCanvasFitPolygon(IEnumerable<int> handles)
        {
            HashSet<int> unique = PreprocessHandles(handles);

            long w = 0;
            long h = 0;

            foreach (int i in unique)
            {
                IntRect bds = GeomUtility.GetBounds(polygon_lib[i].GetTransformedPoly()[0]);
                w += bds.Width();
                h += bds.Height();
            }

            w += 1000;
            h += 1000;

            IntRect canvas = new IntRect(0, h, w, 0);

            return handles.Select(p => AddCanvasFitPolygon(canvas, p)).ToArray();
        }

        public Ngons GetTransformedPoly(int handle)
        {
            return polygon_lib[handle].GetTransformedPoly();
        }

        public void ResetTransformLib()
        {
            for (int i = 0; i < polygon_lib.Count; i++)
                polygon_lib[i].trans = Mat3x3.Eye();
        }

        public void ApplyTransformLibUVSpace(Vector64[] points, int[] handles)
        {
            for (int i = 0; i < points.Length; i++)
                points[i] = polygon_lib[handles[i]].trans * (unit_scale * points[i]);
        }

        public void RevertTransformLibUVSpace(Vector64[] points, int[] handles)
        {
            for (int i = 0; i < points.Length; i++)
                points[i] = (1.0 / unit_scale) * (polygon_lib[handles[i]].trans.Inverse() * points[i]);
        }
    }
}
