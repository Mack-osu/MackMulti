using BanchoSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Storage;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;

namespace MackMulti.Database.Databases
{
	public class ScoreDb : BaseDatabase<Score>
	{

	}
}
