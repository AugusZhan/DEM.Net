﻿// Reprojection.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.Imagery;
using DotSpatial.Projections;

namespace DEM.Net.Core
{
    public static class Reprojection
    {

        public const int SRID_GEODETIC = 4236;
        public const int SRID_PROJECTED_LAMBERT_93 = 2154;
        public const int SRID_PROJECTED_MERCATOR = 3857;

        public static HeightMap ReprojectGeodeticToCartesian(this HeightMap heightMap)
        {
            heightMap.Coordinates = heightMap.Coordinates.ReprojectTo(SRID_GEODETIC, SRID_PROJECTED_MERCATOR, heightMap.Count);
            heightMap.BoundingBox = heightMap.BoundingBox.ReprojectTo(SRID_GEODETIC, SRID_PROJECTED_MERCATOR);
            return heightMap;
        }

        public static HeightMap ReprojectTo(this HeightMap heightMap, int sourceEpsgCode, int destinationEpsgCode)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return heightMap;

            heightMap.Coordinates = heightMap.Coordinates.ReprojectTo(sourceEpsgCode, destinationEpsgCode, heightMap.Count);
            heightMap.BoundingBox = heightMap.BoundingBox.ReprojectTo(sourceEpsgCode, destinationEpsgCode);

            return heightMap;
        }

        public static IEnumerable<GeoPoint> ReprojectGeodeticToCartesian(this IEnumerable<GeoPoint> points, int? count = null)
        {
            return points.ReprojectTo(SRID_GEODETIC, SRID_PROJECTED_MERCATOR, count);
        }
        public static List<Gpx.GpxTrackPoint> ReprojectGeodeticToCartesian(this IEnumerable<Gpx.GpxTrackPoint> points)
        {
            return points.ReprojectTo(SRID_GEODETIC, SRID_PROJECTED_MERCATOR, null);
        }


        public static IEnumerable<GeoPoint> ReprojectTo(this IEnumerable<GeoPoint> points, int sourceEpsgCode, int destinationEpsgCode, int? pointCount = null)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return points;


            // Defines the starting coordiante system
            ProjectionInfo pSource = ProjectionInfo.FromEpsgCode(sourceEpsgCode);
            // Defines the starting coordiante system
            ProjectionInfo pTarget = ProjectionInfo.FromEpsgCode(destinationEpsgCode);

            if (pointCount == null)
            {
                return points.Select(pt => ReprojectPoint(pt, pSource, pTarget));
            }
            else
            {
                double[] inputPoints = null;

                return points.Select((p, index) =>
                {
                    if (inputPoints == null)
                    {
                        inputPoints = points.SelectMany(pt => new double[] { pt.Longitude, pt.Latitude }).ToArray();
                        Reproject.ReprojectPoints(inputPoints, null, pSource, pTarget, 0, pointCount.Value);
                    }
                    var pout = p.Clone();
                    pout.Longitude = inputPoints[2 * index];
                    pout.Latitude = inputPoints[2 * index + 1];

                    if (index == pointCount - 1)
                    {
                        inputPoints = null;
                    }
                    return pout;
                });


            }
        }
        public static IEnumerable<(int Key, GeoPoint Point)> ReprojectTo(this IEnumerable<(int Key, GeoPoint Point)> points, int sourceEpsgCode, int destinationEpsgCode, int pointCount)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return points;


            // Defines the starting coordiante system
            ProjectionInfo pSource = ProjectionInfo.FromEpsgCode(sourceEpsgCode);
            // Defines the starting coordiante system
            ProjectionInfo pTarget = ProjectionInfo.FromEpsgCode(destinationEpsgCode);

            return points.Select(pt => (pt.Key, ReprojectPoint(pt.Point, pSource, pTarget)));

        }
        public static List<Gpx.GpxTrackPoint> ReprojectTo(this IEnumerable<Gpx.GpxTrackPoint> points, int sourceEpsgCode, int destinationEpsgCode, int? pointCount = null)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return points.ToList();


            // Defines the starting coordiante system
            ProjectionInfo pSource = ProjectionInfo.FromEpsgCode(sourceEpsgCode);
            // Defines the starting coordiante system
            ProjectionInfo pTarget = ProjectionInfo.FromEpsgCode(destinationEpsgCode);

            if (pointCount == null)
            {
                return points.Select(pt => ReprojectPoint(pt, pSource, pTarget)).ToList();
            }
            else
            {
                double[] inputPoints = null;

                return points.Select((p, index) =>
                {
                    if (inputPoints == null)
                    {
                        inputPoints = points.SelectMany(pt => new double[] { pt.Longitude, pt.Latitude }).ToArray();
                        Reproject.ReprojectPoints(inputPoints, null, pSource, pTarget, 0, pointCount.Value);
                    }
                    p.Longitude = inputPoints[2 * index];
                    p.Latitude = inputPoints[2 * index + 1];

                    if (index == pointCount - 1)
                    {
                        inputPoints = null;
                    }
                    return p;
                })
                .ToList();


            }
        }
        public static BoundingBox ReprojectTo(this BoundingBox bbox, int sourceEpsgCode, int destinationEpsgCode)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return bbox;


            // Defines the starting coordiante system
            ProjectionInfo pSource = ProjectionInfo.FromEpsgCode(sourceEpsgCode);
            // Defines the starting coordiante system
            ProjectionInfo pTarget = ProjectionInfo.FromEpsgCode(destinationEpsgCode);

            var minmin = ReprojectPoint(new GeoPoint(bbox.yMin, bbox.xMin), pSource, pTarget);
            var minmax = ReprojectPoint(new GeoPoint(bbox.yMin, bbox.xMax), pSource, pTarget);
            var maxmax = ReprojectPoint(new GeoPoint(bbox.yMax, bbox.xMax), pSource, pTarget);
            var maxmin = ReprojectPoint(new GeoPoint(bbox.yMax, bbox.xMin), pSource, pTarget);

            return GeometryService.GetBoundingBox(new GeoPoint[] { minmin, minmax, maxmax, maxmin });

        }

        public static GeoPoint ReprojectTo(this GeoPoint point, int sourceEpsgCode, int destinationEpsgCode)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return point;

            // Defines the starting coordiante system
            ProjectionInfo pSource = ProjectionInfo.FromEpsgCode(sourceEpsgCode);
            // Defines the starting coordiante system
            ProjectionInfo pTarget = ProjectionInfo.FromEpsgCode(destinationEpsgCode);

            var tmp = ReprojectPoint(point, pSource, pTarget);

            point.Latitude = tmp.Latitude;
            point.Longitude = tmp.Longitude;

            return point;
        }

        private static GeoPoint ReprojectPoint(GeoPoint sourcePoint, ProjectionInfo sourceProj, ProjectionInfo destProj)
        {

            double[] coords = { sourcePoint.Longitude, sourcePoint.Latitude };
            // Calls the reproject function that will transform the input location to the output locaiton
            Reproject.ReprojectPoints(coords, new double[] { sourcePoint.Elevation.GetValueOrDefault(0) }, sourceProj, destProj, 0, 1);

            return new GeoPoint(sourcePoint.Id, coords[1], coords[0], sourcePoint.Elevation);
        }
        private static Gpx.GpxTrackPoint ReprojectPoint(Gpx.GpxTrackPoint sourcePoint, ProjectionInfo sourceProj, ProjectionInfo destProj)
        {

            double[] coords = { sourcePoint.Longitude, sourcePoint.Latitude };
            // Calls the reproject function that will transform the input location to the output locaiton
            Reproject.ReprojectPoints(coords, new double[] { sourcePoint.Elevation.GetValueOrDefault(0) }, sourceProj, destProj, 0, 1);

            sourcePoint.Longitude = coords[0];
            sourcePoint.Latitude = coords[1];
            return sourcePoint;
        }


    }


}
