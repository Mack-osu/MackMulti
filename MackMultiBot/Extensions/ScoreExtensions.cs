using MackMultiBot.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Extensions
{
	public static class ScoreExtensions
	{
		public static float GetAccuracy(this Score score)
		{
			return (score.Count300 * 300 + score.Count100 * 100 + score.Count50 * 50) /
				(float)(300 * (score.Count300 + score.Count100 + score.Count50 + score.CountMiss)) * 100;
		}
	}
}
