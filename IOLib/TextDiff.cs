using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IOLib
{
    public enum DiffKind
    {
        Same,
        Changed,
        OnlyLeft,
        OnlyRight,
    }

    // One aligned row of a side-by-side diff. A side that has no line here has No = -1 and text null.
    public class DiffRow
    {
        public DiffKind Kind { get; init; }
        public int LeftNo { get; init; } = -1;
        public int RightNo { get; init; } = -1;
        public string Left { get; init; }
        public string Right { get; init; }
    }

    // Line and character diff for the file compare window: lines unique on both sides anchor the
    // alignment (patience diff), and Myers O(ND) aligns what is left between the anchors.
    public static class TextDiff
    {
        // shortcut: the Myers trace costs O(D^2) ints (4000 -> about 64 MB); past the cap a gap without unique lines (e.g. a block of identical lines) is one changed block. Upgrade to linear-space Myers if that shows up.
        private const int LineCap = 4000;
        private const int CharCap = 400;
        // Nested anchoring stops here and lets Myers align the gap, so odd inputs cannot exhaust the stack.
        private const int MaxDepth = 64;

        public static List<DiffRow> Lines(IList<string> left, IList<string> right, CancellationToken token = default)
        {
            var rows = new List<DiffRow>();
            int l = 0, r = 0, dels = 0, adds = 0;
            void Flush()
            {
                // A removed run next to an added run is shown as changed lines, pair by pair.
                int pairs = Math.Min(dels, adds), l0 = l - dels, r0 = r - adds;
                for (int i = 0; i < pairs; i++)
                    rows.Add(new DiffRow { Kind = DiffKind.Changed, LeftNo = l0 + i, RightNo = r0 + i, Left = left[l0 + i], Right = right[r0 + i] });
                for (int i = pairs; i < dels; i++)
                    rows.Add(new DiffRow { Kind = DiffKind.OnlyLeft, LeftNo = l0 + i, Left = left[l0 + i] });
                for (int i = pairs; i < adds; i++)
                    rows.Add(new DiffRow { Kind = DiffKind.OnlyRight, RightNo = r0 + i, Right = right[r0 + i] });
                dels = adds = 0;
            }
            var ops = new List<char>(left.Count + right.Count);
            Anchored(left, 0, left.Count, right, 0, right.Count, 0, ops, token);
            foreach (char op in ops)
            {
                if (op == '-') { l++; dels++; }
                else if (op == '+') { r++; adds++; }
                else
                {
                    Flush();
                    rows.Add(new DiffRow { Kind = DiffKind.Same, LeftNo = l, RightNo = r, Left = left[l], Right = right[r] });
                    l++;
                    r++;
                }
            }
            Flush();
            return rows;
        }

        // Character ranges (start, length) that differ inside two versions of one line.
        public static void Spans(string a, string b, out List<(int Start, int Length)> inA, out List<(int Start, int Length)> inB)
        {
            inA = new List<(int, int)>();
            inB = new List<(int, int)>();
            int x = 0, y = 0;
            foreach (char op in Ops(a.ToCharArray(), b.ToCharArray(), CharCap, default))
            {
                if (op == '-') Extend(inA, x++);
                else if (op == '+') Extend(inB, y++);
                else { x++; y++; }
            }
        }

        private static void Extend(List<(int Start, int Length)> spans, int at)
        {
            int last = spans.Count - 1;
            if (last >= 0 && spans[last].Start + spans[last].Length == at)
                spans[last] = (spans[last].Start, spans[last].Length + 1);
            else
                spans.Add((at, 1));
        }

        // Edit script from a to b: '=' keep, '-' only in a, '+' only in b.
        private static List<char> Ops<T>(IList<T> a, IList<T> b, int cap, CancellationToken token)
        {
            var eq = EqualityComparer<T>.Default;
            int head = 0, n = a.Count, m = b.Count;
            while (head < n && head < m && eq.Equals(a[head], b[head])) head++;
            int tail = 0;
            while (tail < n - head && tail < m - head && eq.Equals(a[n - 1 - tail], b[m - 1 - tail])) tail++;
            n -= head + tail;
            m -= head + tail;

            var ops = new List<char>(a.Count + m);
            ops.AddRange(new string('=', head));
            Middle(a, head, n, b, head, m, cap, eq, ops, token);
            ops.AddRange(new string('=', tail));
            return ops;
        }

        // Edit script for a[a0..a1) against b[b0..b1), appended to ops.
        private static void Anchored(IList<string> a, int a0, int a1, IList<string> b, int b0, int b1, int depth, List<char> ops, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            int head = 0;
            while (a0 + head < a1 && b0 + head < b1 && a[a0 + head] == b[b0 + head]) head++;
            int tail = 0;
            while (a1 - tail > a0 + head && b1 - tail > b0 + head && a[a1 - 1 - tail] == b[b1 - 1 - tail]) tail++;
            ops.AddRange(new string('=', head));
            a0 += head;
            b0 += head;
            a1 -= tail;
            b1 -= tail;

            var anchors = depth < MaxDepth && a0 < a1 && b0 < b1 ? Anchors(a, a0, a1, b, b0, b1) : new List<(int A, int B)>();
            if (anchors.Count == 0)
            {
                Middle(a, a0, a1 - a0, b, b0, b1 - b0, LineCap, EqualityComparer<string>.Default, ops, token);
            }
            else
            {
                int x = a0, y = b0;
                foreach (var (ai, bi) in anchors)
                {
                    Anchored(a, x, ai, b, y, bi, depth + 1, ops, token);
                    ops.Add('=');
                    x = ai + 1;
                    y = bi + 1;
                }
                Anchored(a, x, a1, b, y, b1, depth + 1, ops, token);
            }
            ops.AddRange(new string('=', tail));
        }

        // Lines that occur exactly once in each range, as the longest chain that is in order on both sides.
        private static List<(int A, int B)> Anchors(IList<string> a, int a0, int a1, IList<string> b, int b0, int b1)
        {
            var seen = new Dictionary<string, (int CountA, int IndexA, int CountB, int IndexB)>(StringComparer.Ordinal);
            for (int i = a0; i < a1; i++)
            {
                seen.TryGetValue(a[i], out var s);
                seen[a[i]] = (s.CountA + 1, i, 0, -1);
            }
            for (int j = b0; j < b1; j++)
                if (seen.TryGetValue(b[j], out var s))
                    seen[b[j]] = (s.CountA, s.IndexA, s.CountB + 1, j);
            var pairs = seen.Values.Where(s => s.CountA == 1 && s.CountB == 1).Select(s => (A: s.IndexA, B: s.IndexB)).OrderBy(p => p.A).ToList();

            // Longest increasing run of B (patience sorting): tails[k] ends the best chain of length k + 1.
            var tails = new List<int>();
            var prev = new int[pairs.Count];
            for (int i = 0; i < pairs.Count; i++)
            {
                int lo = 0, hi = tails.Count;
                while (lo < hi)
                {
                    int mid = (lo + hi) / 2;
                    if (pairs[tails[mid]].B < pairs[i].B) lo = mid + 1;
                    else hi = mid;
                }
                prev[i] = lo > 0 ? tails[lo - 1] : -1;
                if (lo == tails.Count) tails.Add(i);
                else tails[lo] = i;
            }
            var chain = new List<(int A, int B)>(tails.Count);
            for (int i = tails.Count > 0 ? tails[tails.Count - 1] : -1; i >= 0; i = prev[i])
                chain.Add(pairs[i]);
            chain.Reverse();
            return chain;
        }

        private static void Middle<T>(IList<T> a, int aStart, int n, IList<T> b, int bStart, int m, int cap, IEqualityComparer<T> eq, List<char> ops, CancellationToken token)
        {
            int limit = Math.Min(n + m, cap), off = limit + 1, found = -1;
            var v = new int[2 * limit + 3];
            var trace = new List<int[]>();
            for (int d = 0; d <= limit && found < 0; d++)
            {
                token.ThrowIfCancellationRequested();
                var snap = new int[2 * d + 1];
                Array.Copy(v, off - d, snap, 0, snap.Length);
                trace.Add(snap);
                for (int k = -d; k <= d; k += 2)
                {
                    int x = k == -d || (k != d && v[off + k - 1] < v[off + k + 1]) ? v[off + k + 1] : v[off + k - 1] + 1;
                    int y = x - k;
                    while (x < n && y < m && eq.Equals(a[aStart + x], b[bStart + y])) { x++; y++; }
                    v[off + k] = x;
                    if (x >= n && y >= m) { found = d; break; }
                }
            }
            if (found < 0)
            {
                ops.AddRange(new string('-', n));
                ops.AddRange(new string('+', m));
                return;
            }

            var back = new List<char>(n + m);
            int cx = n, cy = m;
            for (int d = found; d > 0; d--)
            {
                var snap = trace[d];
                int k = cx - cy;
                int prevK = k == -d || (k != d && snap[d + k - 1] < snap[d + k + 1]) ? k + 1 : k - 1;
                int prevX = snap[d + prevK], prevY = prevX - prevK;
                while (cx > prevX && cy > prevY) { back.Add('='); cx--; cy--; }
                if (cx == prevX) { back.Add('+'); cy--; }
                else { back.Add('-'); cx--; }
            }
            while (cx > 0) { back.Add('='); cx--; }
            back.Reverse();
            ops.AddRange(back);
        }
    }
}
