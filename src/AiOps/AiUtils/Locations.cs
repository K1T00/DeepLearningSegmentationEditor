using CsvHelper.Configuration;
using System;
using System.Collections.Generic;

namespace AiOps.AiUtils
{

	public class Locations : List<Location>
	{


		public Location FindClosest(Location target)
		{
			if (this.Count == 0)
				return null;

			Location closest = null;
			var minDistance = double.MaxValue;

			foreach (var loc in this)
			{
				var distance = target.DistanceTo(loc);
				if (distance < minDistance)
				{
					minDistance = distance;
					closest = loc;
				}
			}

			return closest;
		}

	}

	public class Location
	{

		private int x;
		private int y;

		public Location()
		{
			this.x = 0;
			this.y = 0;
		}

		public Location(Location other)
		{
			this.x = other.x;
			this.y = other.y;
		}

		public int X
		{
			get => this.x;
			set => this.x = value;
		}

		public int Y
		{
			get => this.y;
			set => this.y = value;
		}

		public double DistanceTo(Location other)
		{
			var dx = this.X - other.X;
			var dy = this.Y - other.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

	}

	public class CsvRecord
	{
		public double Index { get; set; }
		public double Axis0 { get; set; }
		public double Axis1 { get; set; }
	}

	public sealed class CsvRecordMap : ClassMap<CsvRecord>
	{
		public CsvRecordMap()
		{
			Map(m => m.Index).Name("index");
			Map(m => m.Axis0).Name("axis-0");
			Map(m => m.Axis1).Name("axis-1");
		}
	}

}
