using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho.Data
{
	public class BanchoClientConfiguration
    {
        /// <summary>
        /// osu! IRC username, obtained from https://osu.ppy.sh/home/account/edit#legacy-api
        /// </summary>
        public string Username { get; init; } = Environment.GetEnvironmentVariable("IRCUSER");

        /// <summary>
        /// osu! IRC password, obtained from https://osu.ppy.sh/home/account/edit#legacy-api
        /// </summary>
        public string Password { get; init; } = Environment.GetEnvironmentVariable("IRCPASS");
    }
}
