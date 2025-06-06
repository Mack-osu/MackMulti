﻿using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class StartCommand : ICommand
	{
		public string Command => "start";

		public string[]? Aliases => [];

		public int MinimumArguments => 0;

		public bool IsGlobal => false;

		public bool AdminCommand => false;

		public string Usage => "!start <seconds>";

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
