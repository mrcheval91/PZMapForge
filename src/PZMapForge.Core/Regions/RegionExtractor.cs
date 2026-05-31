using PZMapForge.Core.ParsedCell;

namespace PZMapForge.Core.Regions;

/// <summary>
/// BFS flood-fill region extractor for SemanticGrid using 4-neighbor connectivity.
///
/// Sort order: kind ASC, pixel_count DESC, bounds.y ASC, bounds.x ASC, discovery_id ASC.
///
/// Flat-index decomposition uses integer modulo (cx = idx % width; cy = idx / width)
/// to avoid the C# integer-division precision issues that could arise from floating-point
/// intermediate values. This matches the PowerShell fix documented in docs/REGION_EXTRACTION.md.
/// </summary>
public static class RegionExtractor
{
    // 4-neighbor offsets: up, down, left, right
    private static readonly (int dx, int dy)[] Neighbors = [( 0,-1), ( 0, 1), (-1, 0), ( 1, 0)];

    public static RegionExtractionResult Extract(
        SemanticGrid grid,
        IReadOnlyDictionary<char, string> codeToKind)
    {
        int w = grid.Width;
        int h = grid.Height;
        int total = w * h;

        var visited = new bool[total];
        // Pre-allocated BFS queue avoids repeated allocation
        var queue   = new int[total];

        var unsorted = new List<(string kind, char code, int px, int bx, int by, int bw, int bh,
                                  double cx, double cy, int tempId)>();
        int tempId = 0;

        for (int sy = 0; sy < h; sy++)
        {
            for (int sx = 0; sx < w; sx++)
            {
                int si = sy * w + sx;
                if (visited[si]) continue;

                char code = grid.GetCode(sx, sy);
                visited[si] = true;
                tempId++;

                // BFS
                int qHead = 0, qTail = 0;
                queue[qTail++] = si;

                int count = 0;
                int minX = w, maxX = -1, minY = h, maxY = -1;
                long sumX = 0, sumY = 0;

                while (qHead < qTail)
                {
                    int cur = queue[qHead++];
                    // Safe integer decomposition — no floating-point intermediate
                    int cx = cur % w;
                    int cy = cur / w;

                    count++;
                    if (cx < minX) minX = cx;
                    if (cx > maxX) maxX = cx;
                    if (cy < minY) minY = cy;
                    if (cy > maxY) maxY = cy;
                    sumX += cx;
                    sumY += cy;

                    foreach (var (dx, dy) in Neighbors)
                    {
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        int ni = ny * w + nx;
                        if (visited[ni] || grid.GetCode(nx, ny) != code) continue;
                        visited[ni] = true;
                        queue[qTail++] = ni;
                    }
                }

                double centX = Math.Round((double)sumX / count, 2);
                double centY = Math.Round((double)sumY / count, 2);

                string kind = codeToKind.TryGetValue(code, out var k) ? k : code.ToString();
                unsorted.Add((kind, code, count,
                              minX, minY, maxX - minX + 1, maxY - minY + 1,
                              centX, centY, tempId));
            }
        }

        // Deterministic sort: kind ASC, px DESC, by ASC, bx ASC, tempId ASC
        var sorted = unsorted
            .OrderBy(r  => r.kind)
            .ThenByDescending(r => r.px)
            .ThenBy(r  => r.by)
            .ThenBy(r  => r.bx)
            .ThenBy(r  => r.tempId)
            .ToList();

        // Assign sequential region_id, build final list
        var regions = new List<SemanticRegion>(sorted.Count);
        for (int i = 0; i < sorted.Count; i++)
        {
            var (kind, code, px, bx, by, bw, bh, centX, centY, _) = sorted[i];
            var region = new SemanticRegion(
                kind, code, px,
                new RegionBounds(bx, by, bw, bh),
                new RegionCentroid(centX, centY))
            { RegionId = i + 1 };
            regions.Add(region);
        }

        // Build summary_by_kind sorted by kind ASC
        var kindMap = new Dictionary<string, RegionKindSummary>(StringComparer.Ordinal);
        foreach (var r in regions)
        {
            if (!kindMap.TryGetValue(r.Kind, out var summary))
            {
                summary = new RegionKindSummary(r.Kind, r.Code);
                kindMap[r.Kind] = summary;
            }
            summary.RegionCount++;
            summary.TotalPixels += r.PixelCount;
            if (r.PixelCount > summary.LargestRegionPixels)
                summary.LargestRegionPixels = r.PixelCount;
        }

        var summaryList = kindMap.Values
            .OrderBy(s => s.Kind, StringComparer.Ordinal)
            .ToList();

        return new RegionExtractionResult(regions, summaryList);
    }
}
